using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Localization;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Reads bounded delimited text incrementally, maps it to a typed schema and
/// exposes only validated snapshots to cancellable persistence handlers.
/// </summary>
public partial class OmniDataImport<TItem> where TItem : class
{
    private const int ReaderBufferSize = 4_096;
    private const int MaximumValidationErrorsPerRow = 100;
    private readonly object _operationSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<string> _headers = [];
    private readonly List<string[]> _rawRows = [];
    private readonly List<DataImportRow<TItem>> _rows = [];
    private readonly Dictionary<string, int> _sourceIndexes = new(StringComparer.Ordinal);
    private CancellationTokenSource? _operation;
    private string? _fileName;
    private string? _failure;
    private bool _busy;
    private bool _importing;
    private int _validCount;
    private long _operationVersion;
    private int _disposeState;
    private readonly string _inputId = $"omni-import-{Guid.NewGuid():N}";

    /// <summary>Immutable target schema containing typed property mappings and parsers.</summary>
    [Parameter, EditorRequired]
    public DataImportSchema<TItem> Schema { get; set; } = default!;

    /// <summary>Optional cancellable destination for accepted typed rows.</summary>
    [Parameter]
    public DataImportHandler<TItem>? Handler { get; set; }

    /// <summary>Raised after Handler succeeds, or directly when no Handler is supplied.</summary>
    [Parameter]
    public EventCallback<DataImportCompletedEventArgs<TItem>> Imported { get; set; }

    /// <summary>Raised after an observed load, conversion or persistence failure.</summary>
    [Parameter]
    public EventCallback<Exception> Failed { get; set; }

    /// <summary>Maximum input size in bytes. Default 5 MB.</summary>
    [Parameter]
    public long MaxFileSize { get; set; } = 5L * 1024 * 1024;

    /// <summary>Maximum data rows retained in memory. Default 10,000.</summary>
    [Parameter]
    public int MaximumRows { get; set; } = 10_000;

    /// <summary>Maximum source columns per row. Default 256.</summary>
    [Parameter]
    public int MaximumColumns { get; set; } = 256;

    /// <summary>Maximum characters accepted in one cell. Default 32,768.</summary>
    [Parameter]
    public int MaximumCellLength { get; set; } = 32_768;

    /// <summary>Maximum processed rows rendered in the preview. Default 25.</summary>
    [Parameter]
    public int PreviewRowCount { get; set; } = 25;

    /// <summary>Allows valid rows to be imported while other rows are invalid.</summary>
    [Parameter]
    public bool AllowPartialImport { get; set; }

    /// <summary>Disables file selection, mapping and import.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Culture used by built-in numeric and date parsers. Defaults to current culture.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>HTML file input accept filter.</summary>
    [Parameter]
    public string Accept { get; set; } = ".csv,.tsv,text/csv,text/tab-separated-values";

    /// <summary>Overrides the file-selection text.</summary>
    [Parameter]
    public string? UploadText { get; set; }

    /// <summary>Overrides the mapping section title.</summary>
    [Parameter]
    public string? MappingTitle { get; set; }

    /// <summary>Overrides the preview section title.</summary>
    [Parameter]
    public string? PreviewTitle { get; set; }

    /// <summary>Overrides the import action text.</summary>
    [Parameter]
    public string? ImportText { get; set; }

    /// <summary>Overrides the clear action text.</summary>
    [Parameter]
    public string? ClearText { get; set; }

    /// <summary>Custom loading content.</summary>
    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Custom preview row cells. The fragment must render cells matching the
    /// component header structure.
    /// </summary>
    [Parameter]
    public RenderFragment<DataImportRow<TItem>>? RowTemplate { get; set; }

    /// <summary>Current immutable source headers.</summary>
    public IReadOnlyList<string> Headers => _headers;

    /// <summary>Current processed rows, bounded by MaximumRows.</summary>
    public IReadOnlyList<DataImportRow<TItem>> Rows => _rows;

    /// <summary>Current typed target-to-source mappings.</summary>
    public IReadOnlyList<DataImportMapping> Mappings
        => Schema.Columns.Select(column => new DataImportMapping(
            column.Property,
            GetHeader(GetSourceIndex(column.Property)),
            GetSourceIndex(column.Property))).ToArray();

    /// <summary>Number of valid processed rows.</summary>
    public int ValidCount => _validCount;

    /// <summary>Number of rejected processed rows.</summary>
    public int InvalidCount => _rows.Count - _validCount;

    /// <summary>Current observed load or persistence failure message.</summary>
    public string? ErrorMessage => _failure;

    /// <summary>
    /// Loads delimited text from a caller-owned stream. The stream remains open.
    /// A newer load or disposal cancels this operation.
    /// </summary>
    public async Task LoadAsync(
        Stream stream,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ThrowIfDisposed();
        ValidateParameters();
        CancellationTokenSource operation = BeginOperation(cancellationToken, out long version);
        _busy = true;
        _failure = null;
        try
        {
            char delimiter = ResolveDelimiter(fileName);
            ParsedImport parsed = await ParseAsync(stream, delimiter, operation.Token);
            if (!IsCurrent(operation, version)) return;
            ApplyParsed(parsed, fileName);
            await ProcessRowsAsync(operation.Token);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsCurrent(operation, version)) return;
            _failure = exception.Message;
            if (Failed.HasDelegate) await Failed.InvokeAsync(exception);
        }
        finally
        {
            if (CompleteOperation(operation, version) && _disposeState == 0)
                await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Persists the current accepted typed snapshot.</summary>
    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!CanImport) return;
        CancellationTokenSource operation = BeginOperation(cancellationToken, out long version);
        _importing = true;
        _failure = null;
        try
        {
            TItem[] items = new TItem[_validCount];
            int itemIndex = 0;
            foreach (DataImportRow<TItem> row in _rows)
            {
                if (row.IsValid) items[itemIndex++] = row.Item!;
            }
            if (Handler is not null) await Handler(items, operation.Token);
            if (!IsCurrent(operation, version)) return;
            if (Imported.HasDelegate)
            {
                DataImportRow<TItem>[] rows = _rows.ToArray();
                await Imported.InvokeAsync(new DataImportCompletedEventArgs<TItem>(
                    Array.AsReadOnly(items),
                    Array.AsReadOnly(rows),
                    rows.Length - items.Length));
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsCurrent(operation, version)) return;
            _failure = exception.Message;
            if (Failed.HasDelegate) await Failed.InvokeAsync(exception);
        }
        finally
        {
            _importing = false;
            if (CompleteOperation(operation, version) && _disposeState == 0)
                await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Clears parsed data and cancels active work.</summary>
    public Task ClearAsync()
    {
        CancelOperation();
        _headers.Clear();
        _rawRows.Clear();
        _rows.Clear();
        _sourceIndexes.Clear();
        _fileName = null;
        _failure = null;
        _validCount = 0;
        _busy = false;
        _importing = false;
        return _disposeState == 0 ? InvokeAsync(StateHasChanged) : Task.CompletedTask;
    }

    /// <summary>Changes one target property mapping and reprocesses the bounded preview.</summary>
    public async Task SetMappingAsync(string property, int sourceIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ThrowIfDisposed();
        if (!Schema.Columns.Any(column => string.Equals(column.Property, property, StringComparison.Ordinal)))
            throw new ArgumentOutOfRangeException(nameof(property));
        if (sourceIndex < -1 || sourceIndex >= _headers.Count)
            throw new ArgumentOutOfRangeException(nameof(sourceIndex));
        CancellationTokenSource operation = BeginOperation(CancellationToken.None, out long version);
        _busy = true;
        try
        {
            _sourceIndexes[property] = sourceIndex;
            await ProcessRowsAsync(operation.Token);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        finally
        {
            if (CompleteOperation(operation, version) && _disposeState == 0)
                await InvokeAsync(StateHasChanged);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ValidateParameters();
    }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs eventArgs)
    {
        IBrowserFile file = eventArgs.File;
        if (file.Size > MaxFileSize)
        {
            IOException exception = new(string.Format(Texts.DataImportFileTooLarge, FormatBytes(MaxFileSize)));
            _failure = exception.Message;
            if (Failed.HasDelegate) await Failed.InvokeAsync(exception);
            return;
        }
        await using Stream stream = file.OpenReadStream(MaxFileSize, _lifetime.Token);
        await LoadAsync(stream, file.Name, _lifetime.Token);
    }

    private async Task ChangeMappingAsync(string property, object? value)
    {
        if (Disabled || !int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out int index)) return;
        await SetMappingAsync(property, index);
    }

    private async Task<ParsedImport> ParseAsync(Stream stream, char delimiter, CancellationToken cancellationToken)
    {
        if (stream.CanSeek && stream.Length - stream.Position > MaxFileSize)
            throw new IOException(string.Format(Texts.DataImportFileTooLarge, FormatBytes(MaxFileSize)));

        await using LimitedReadStream limited = new(
            stream,
            MaxFileSize,
            string.Format(Texts.DataImportFileTooLarge, FormatBytes(MaxFileSize)),
            leaveOpen: true);
        using StreamReader reader = new(limited, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, ReaderBufferSize, leaveOpen: true);
        char[] rented = ArrayPool<char>.Shared.Rent(ReaderBufferSize);
        try
        {
            List<string[]> records = [];
            List<string> fields = [];
            StringBuilder field = new();
            bool quoted = false;
            bool quotePending = false;
            bool closedQuotedField = false;
            bool sawCharacter = false;
            bool skipLf = false;

            while (true)
            {
                int read = await reader.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken);
                if (read == 0) break;
                for (int index = 0; index < read; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    char character = rented[index];
                    sawCharacter = true;
                    if (skipLf)
                    {
                        skipLf = false;
                        if (character == '\n') continue;
                    }
                    if (quoted)
                    {
                        if (quotePending)
                        {
                            if (character == '"')
                            {
                                Append(field, '"');
                                quotePending = false;
                                continue;
                            }
                            quoted = false;
                            quotePending = false;
                            closedQuotedField = true;
                        }
                        else if (character == '"')
                        {
                            quotePending = true;
                            continue;
                        }
                        else
                        {
                            Append(field, character);
                            continue;
                        }
                    }
                    if (character == '"' && field.Length == 0)
                    {
                        quoted = true;
                    }
                    else if (character == delimiter)
                    {
                        AddField(fields, field);
                        closedQuotedField = false;
                    }
                    else if (character is '\r' or '\n')
                    {
                        AddField(fields, field);
                        AddRecord(records, fields);
                        closedQuotedField = false;
                        if (character == '\r') skipLf = true;
                    }
                    else if (closedQuotedField && !char.IsWhiteSpace(character))
                    {
                        throw new FormatException(string.Format(Texts.DataImportUnexpectedCharacter, character));
                    }
                    else if (!closedQuotedField)
                    {
                        Append(field, character);
                    }
                }
            }
            if (quoted && !quotePending) throw new FormatException(Texts.DataImportUnclosedQuote);
            if (quotePending) quoted = false;
            if (field.Length != 0 || fields.Count != 0 || sawCharacter)
            {
                AddField(fields, field);
                if (fields.Any(static value => value.Length != 0)) AddRecord(records, fields);
            }
            if (records.Count == 0) throw new InvalidOperationException(Texts.DataImportEmptyFile);

            string[] headers;
            int firstDataRow;
            if (Schema.HasHeader)
            {
                headers = records[0];
                firstDataRow = 1;
            }
            else
            {
                int count = records.Max(static record => record.Length);
                headers = Enumerable.Range(1, count)
                    .Select(index => string.Format(FormattingCulture, Texts.DataImportColumn, index))
                    .ToArray();
                firstDataRow = 0;
            }
            if (headers.Length == 0) throw new InvalidOperationException(Texts.DataImportEmptyFile);
            if (headers.Length > MaximumColumns) throw new InvalidOperationException(Texts.DataImportTooManyColumns);
            string[][] rows = records.Skip(firstDataRow).ToArray();
            if (rows.Length == 0) throw new InvalidOperationException(Texts.DataImportNoDataRows);
            return new ParsedImport(headers, rows);

            void Append(StringBuilder builder, char value)
            {
                if (builder.Length >= MaximumCellLength)
                    throw new InvalidOperationException(Texts.DataImportCellTooLong);
                builder.Append(value);
            }

            void AddField(List<string> target, StringBuilder builder)
            {
                if (target.Count >= MaximumColumns)
                    throw new InvalidOperationException(Texts.DataImportTooManyColumns);
                target.Add(builder.ToString());
                builder.Clear();
            }

            void AddRecord(List<string[]> target, List<string> values)
            {
                if (target.Count >= MaximumRows + (Schema.HasHeader ? 1 : 0))
                    throw new InvalidOperationException(Texts.DataImportTooManyRows);
                target.Add(values.ToArray());
                values.Clear();
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private void ApplyParsed(ParsedImport parsed, string? fileName)
    {
        _headers.Clear();
        _headers.AddRange(parsed.Headers);
        _rawRows.Clear();
        _rawRows.AddRange(parsed.Rows);
        _fileName = fileName;
        BuildAutomaticMappings();
    }

    private void BuildAutomaticMappings()
    {
        _sourceIndexes.Clear();
        HashSet<int> claimed = [];
        for (int targetIndex = 0; targetIndex < Schema.Columns.Count; targetIndex++)
        {
            IDataImportColumn<TItem> column = Schema.Columns[targetIndex];
            int sourceIndex = Schema.HasHeader ? FindHeader(column, claimed) : targetIndex;
            if ((uint)sourceIndex >= (uint)_headers.Count) sourceIndex = -1;
            _sourceIndexes[column.Property] = sourceIndex;
            if (sourceIndex >= 0) claimed.Add(sourceIndex);
        }
    }

    private int FindHeader(IDataImportColumn<TItem> column, HashSet<int> claimed)
    {
        for (int index = 0; index < _headers.Count; index++)
        {
            if (claimed.Contains(index)) continue;
            string normalized = _headers[index].Trim();
            if (string.Equals(normalized, column.Header, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, column.Property, StringComparison.OrdinalIgnoreCase)
                || column.Aliases.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                return index;
        }
        return -1;
    }

    private async Task ProcessRowsAsync(CancellationToken cancellationToken)
    {
        _rows.Clear();
        _validCount = 0;
        CultureInfo culture = Culture ?? FormattingCulture;
        List<ValidationResult> validationResults = [];
        for (int rawIndex = 0; rawIndex < _rawRows.Count; rawIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rawIndex != 0 && (rawIndex & 127) == 0)
                await Task.Yield();
            string[] values = _rawRows[rawIndex];
            TItem item = Schema.Factory();
            List<DataImportError> errors = [];
            foreach (IDataImportColumn<TItem> column in Schema.Columns)
            {
                int sourceIndex = GetSourceIndex(column.Property);
                string text = (uint)sourceIndex < (uint)values.Length ? values[sourceIndex] : string.Empty;
                if (!column.TryAssign(
                        item,
                        text,
                        culture,
                        Texts.DataImportRequiredValue,
                        Texts.DataImportInvalidValue,
                        out string? error))
                    errors.Add(new DataImportError(ToRowNumber(rawIndex), column.Property, error!));
            }
            validationResults.Clear();
            foreach (IDataImportColumn<TItem> column in Schema.Columns)
            {
                DataAnnotationsValidation.ValidateProperty(
                    item,
                    column.PropertyInfo,
                    column.PropertyInfo.GetValue(item),
                    validationResults);
            }
            if (item is IValidatableObject validatable)
            {
                ValidationContext context = new(item, typeof(TItem).Name, serviceProvider: null, items: null);
                foreach (ValidationResult result in validatable.Validate(context))
                {
                    if (result != ValidationResult.Success) validationResults.Add(result);
                }
            }
            foreach (ValidationResult validationResult in validationResults.Take(MaximumValidationErrorsPerRow))
            {
                string? property = validationResult.MemberNames.FirstOrDefault();
                if (property is not null && errors.Any(error => string.Equals(error.Property, property, StringComparison.Ordinal)))
                    continue;
                errors.Add(new DataImportError(
                    ToRowNumber(rawIndex),
                    property,
                    validationResult.ErrorMessage ?? Texts.DataImportInvalid));
            }
            _rows.Add(new DataImportRow<TItem>(
                ToRowNumber(rawIndex),
                Array.AsReadOnly(values),
                errors.Count == 0 ? item : null,
                Array.AsReadOnly(errors.ToArray())));
            if (errors.Count == 0) _validCount++;
        }
    }

    private CancellationTokenSource BeginOperation(CancellationToken external, out long version)
    {
        CancellationTokenSource operation = external.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, external)
            : CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationTokenSource? previous;
        lock (_operationSync)
        {
            previous = _operation;
            _operation = operation;
            version = ++_operationVersion;
        }
        CancelAndDispose(previous);
        return operation;
    }

    private bool CompleteOperation(CancellationTokenSource operation, long version)
    {
        bool current;
        lock (_operationSync)
        {
            current = ReferenceEquals(_operation, operation) && _operationVersion == version;
            if (current) _operation = null;
        }
        operation.Dispose();
        if (current) _busy = false;
        return current;
    }

    private bool IsCurrent(CancellationTokenSource operation, long version)
    {
        lock (_operationSync)
            return ReferenceEquals(_operation, operation)
                   && _operationVersion == version
                   && !operation.IsCancellationRequested;
    }

    private void CancelOperation()
    {
        CancellationTokenSource? operation;
        lock (_operationSync)
        {
            operation = _operation;
            _operation = null;
            _operationVersion++;
        }
        CancelAndDispose(operation);
    }

    private void ValidateParameters()
    {
        ArgumentNullException.ThrowIfNull(Schema);
        if (MaxFileSize < 1) throw new ArgumentOutOfRangeException(nameof(MaxFileSize));
        if (MaximumRows < 1) throw new ArgumentOutOfRangeException(nameof(MaximumRows));
        if (MaximumColumns < 1) throw new ArgumentOutOfRangeException(nameof(MaximumColumns));
        if (MaximumCellLength < 1) throw new ArgumentOutOfRangeException(nameof(MaximumCellLength));
        if (PreviewRowCount < 1) throw new ArgumentOutOfRangeException(nameof(PreviewRowCount));
    }

    private char ResolveDelimiter(string? fileName)
        => string.Equals(Path.GetExtension(fileName), ".tsv", StringComparison.OrdinalIgnoreCase)
            ? '\t'
            : Schema.Delimiter;

    private int ToRowNumber(int rawIndex) => rawIndex + (Schema.HasHeader ? 2 : 1);
    private int GetSourceIndex(string property) => _sourceIndexes.GetValueOrDefault(property, -1);
    private string GetHeader(int index) => (uint)index < (uint)_headers.Count ? _headers[index] : string.Empty;
    private static string GetValue(DataImportRow<TItem> row, int index)
        => (uint)index < (uint)row.Values.Count ? row.Values[index] : string.Empty;

    private bool CanClear => _headers.Count != 0 || _failure is not null;
    private bool CanImport => !Disabled && !_busy && !_importing && ValidCount != 0 && (AllowPartialImport || InvalidCount == 0);
    private string MappingHeadingId => $"{Id}-mapping";
    private string PreviewHeadingId => $"{Id}-preview";
    private string UploadHint => Texts.Plural(
        OmniTranslationKeys.DataImportUploadHint,
        MaximumRows,
        Texts.DataImportUploadHint,
        FormatBytes(MaxFileSize),
        MaximumRows);
    private string PreviewSummary => string.Format(
        FormattingCulture,
        Texts.DataImportSummary,
        Texts.Plural(OmniTranslationKeys.DataImportValidCount, ValidCount, Texts.DataImportValidCount, ValidCount),
        Texts.Plural(OmniTranslationKeys.DataImportInvalidCount, InvalidCount, Texts.DataImportInvalidCount, InvalidCount),
        Texts.Plural(OmniTranslationKeys.DataImportTotalCount, _rows.Count, Texts.DataImportTotalCount, _rows.Count));
    private string ImportAvailabilityText => InvalidCount != 0 && !AllowPartialImport
        ? Texts.DataImportResolveErrors
        : Texts.Plural(
            OmniTranslationKeys.DataImportReady,
            ValidCount,
            Texts.DataImportReady,
            ValidCount);
    private string RootCss => CssBuilder.Default("omni-data-import")
        .AddClass("omni-data-import-disabled", Disabled)
        .AddClass(Class)
        .Build();

    private static string FormatBytes(long bytes)
        => bytes >= 1024 * 1024
            ? $"{bytes / (1024d * 1024d):0.#} MB"
            : $"{bytes / 1024d:0.#} KB";

    private static void CancelAndDispose(CancellationTokenSource? source)
    {
        if (source is null) return;
        try { source.Cancel(); }
        catch (ObjectDisposedException) { }
        source.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposeState != 0, this);

    /// <summary>Cancels active load/import work and releases owned resources.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        _lifetime.Cancel();
        CancelOperation();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record ParsedImport(string[] Headers, string[][] Rows);

    private sealed class LimitedReadStream(
        Stream inner,
        long maximumBytes,
        string limitMessage,
        bool leaveOpen) : Stream
    {
        private long _bytesRead;
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _bytesRead; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
            => Count(inner.Read(buffer, offset, count));
        public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Count(await inner.ReadAsync(buffer, cancellationToken));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadArrayAsync(buffer, offset, count, cancellationToken);
        private async Task<int> ReadArrayAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Count(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));
        private int Count(int count)
        {
            _bytesRead += count;
            if (_bytesRead > maximumBytes) throw new IOException(limitMessage);
            return count;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing && !leaveOpen) inner.Dispose();
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            if (!leaveOpen) await inner.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
