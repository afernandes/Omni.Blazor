using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>Behavioural contract for bounded parsing, typed mapping, validation and cancellation.</summary>
public sealed class OmniDataImportTests : TestContextBase
{
    public sealed class Produto
    {
        [Required(ErrorMessage = "Informe o nome.")]
        public string? Nome { get; set; }

        [Range(0.01, 100_000, ErrorMessage = "Informe um preço positivo.")]
        public decimal Preco { get; set; }

        [Range(1, 1_000, ErrorMessage = "Informe uma quantidade positiva.")]
        public int Quantidade { get; set; }

        public Guid Codigo { get; set; }
    }

    private static readonly DataImportSchema<Produto> Schema =
        DataImportSchema<Produto>.Create(import => import
            .Factory(static () => new Produto())
            .Delimiter(';')
            .Column(item => item.Nome, column => column
                .Header("Nome")
                .Aliases("Nome Completo")
                .Required("Informe o nome."))
            .Column(item => item.Preco, column => column.Header("Preço").Aliases("Preco").Required())
            .Column(item => item.Quantidade, column => column.Header("Quantidade").Required())
            .Column(item => item.Codigo, column => column.Header("Código").Aliases("Codigo").Required()));

    [Fact]
    public void Schema_is_typed_immutable_and_rejects_invalid_or_duplicate_properties()
    {
        DataImportSchemaBuilder<Produto> builder = DataImportSchema<Produto>.Builder();
        builder.Factory(static () => new Produto());
        builder.Column(item => item.Nome, column => column
            .Header("Produto")
            .Aliases("Nome", "Descrição")
            .Parse(static (ReadOnlySpan<char> value, IFormatProvider _, out string? parsed) =>
            {
                parsed = value.ToString().ToUpperInvariant();
                return true;
            }));
        DataImportSchema<Produto> schema = builder.Build();

        Assert.Equal(1, schema.Count);
        Assert.Throws<InvalidOperationException>(() => builder.HasHeader(false));
        Assert.Throws<InvalidOperationException>(() => DataImportSchema<Produto>.Create(import => import
            .Column(item => item.Nome)
            .Column(item => item.Nome)));
        Assert.Throws<ArgumentException>(() => DataImportSchema<Produto>.Create(import => import
            .Column(item => item.Nome!.Length)));
    }

    [Fact]
    public async Task Parses_quoted_multiline_values_maps_aliases_and_validates_typed_rows()
    {
        Guid firstCode = Guid.NewGuid();
        Guid secondCode = Guid.NewGuid();
        string csv = $"Nome Completo;Preco;Quantidade;Codigo\r\n\"Caneca\nEspecial\";12,50;2;{firstCode}\r\n;9,90;0;{secondCode}\r\n";
        var cut = Render<OmniDataImport<Produto>>(parameters => parameters
            .Add(component => component.Schema, Schema)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo("pt-BR")));

        await LoadAsync(cut, csv, "produtos.csv");

        Assert.Equal(2, cut.Instance.Rows.Count);
        Assert.Equal(1, cut.Instance.ValidCount);
        Assert.Equal(1, cut.Instance.InvalidCount);
        Assert.Equal("Caneca\nEspecial", cut.Instance.Rows[0].Item!.Nome);
        Assert.Equal(12.50m, cut.Instance.Rows[0].Item!.Preco);
        Assert.Equal(firstCode, cut.Instance.Rows[0].Item!.Codigo);
        Assert.Contains(cut.Instance.Rows[1].Errors, error => error.Message == "Informe o nome.");
        Assert.Contains(cut.Instance.Rows[1].Errors, error => error.Message == "Informe uma quantidade positiva.");
        Assert.All(cut.Instance.Mappings, mapping => Assert.True(mapping.SourceIndex >= 0));
        Assert.Contains("Pré-visualização validada", cut.Markup);
    }

    [Fact]
    public async Task Mapping_changes_reprocess_rows_and_partial_import_uses_a_copy_safe_snapshot()
    {
        DataImportCompletedEventArgs<Produto>? completed = null;
        IReadOnlyList<Produto>? handled = null;
        DataImportHandler<Produto> handler = (items, _) =>
        {
            handled = items;
            return ValueTask.CompletedTask;
        };
        var cut = Render<OmniDataImport<Produto>>(parameters => parameters
            .Add(component => component.Schema, Schema)
            .Add(component => component.AllowPartialImport, true)
            .Add(component => component.Culture, CultureInfo.InvariantCulture)
            .Add(component => component.Handler, handler)
            .Add(component => component.Imported, args => completed = args));
        string validCode = Guid.NewGuid().ToString();
        string invalidCode = Guid.NewGuid().ToString();
        await LoadAsync(cut,
            $"Nome;Preço;Quantidade;Código\nMouse;25.5;3;{validCode}\nTeclado;inválido;2;{invalidCode}\n",
            "produtos.csv");

        Assert.Equal(1, cut.Instance.ValidCount);
        await cut.InvokeAsync(() => cut.Instance.SetMappingAsync(nameof(Produto.Preco), -1));
        Assert.Equal(0, cut.Instance.ValidCount);
        await cut.InvokeAsync(() => cut.Instance.SetMappingAsync(nameof(Produto.Preco), 1));
        await cut.InvokeAsync(() => cut.Instance.ImportAsync());

        Assert.NotNull(handled);
        Assert.Single(handled);
        Assert.NotNull(completed);
        Assert.Single(completed.Items);
        Assert.Equal(1, completed.RejectedCount);
        Assert.NotSame(cut.Instance.Rows, completed.Rows);
    }

    [Fact]
    public async Task A_new_load_cancels_the_previous_read_and_only_latest_data_wins()
    {
        BlockingStream firstStream = new();
        var cut = Render<OmniDataImport<Produto>>(parameters => parameters
            .Add(component => component.Schema, Schema)
            .Add(component => component.Culture, CultureInfo.InvariantCulture));

        Task firstLoad = cut.InvokeAsync(() => cut.Instance.LoadAsync(firstStream, "first.csv"));
        CancellationToken firstToken = await firstStream.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        await LoadAsync(cut,
            $"Nome;Preço;Quantidade;Código\nAtual;10;1;{Guid.NewGuid()}\n",
            "latest.csv");
        await firstLoad.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        Assert.True(firstToken.IsCancellationRequested);
        Assert.Single(cut.Instance.Rows);
        Assert.Equal("Atual", cut.Instance.Rows[0].Item!.Nome);
    }

    [Fact]
    public async Task Enforces_limits_cancels_handler_on_dispose_and_splats_common_surface()
    {
        TaskCompletionSource<CancellationToken> handlerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DataImportHandler<Produto> handler = async (_, cancellationToken) =>
        {
            handlerStarted.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        };
        var cut = Render<OmniDataImport<Produto>>(parameters => parameters
            .Add(component => component.Schema, Schema)
            .Add(component => component.Handler, handler)
            .Add(component => component.Culture, CultureInfo.InvariantCulture)
            .Add(component => component.MaximumRows, 1)
            .Add(component => component.Class, "import-custom")
            .Add(component => component.Style, "max-width:900px")
            .AddUnmatched("data-testid", "product-import"));
        var root = cut.Find(".omni-data-import");
        Assert.Contains("import-custom", root.ClassList);
        Assert.Equal("max-width:900px", root.GetAttribute("style"));
        Assert.Equal("product-import", root.GetAttribute("data-testid"));

        await LoadAsync(cut,
            $"Nome;Preço;Quantidade;Código\nUm;10;1;{Guid.NewGuid()}\nDois;20;2;{Guid.NewGuid()}\n",
            "too-many.csv");
        Assert.Contains("ultrapassa o limite de linhas", cut.Instance.ErrorMessage);
        Assert.Contains("ultrapassa o limite de linhas", cut.Markup);

        cut.Render(parameters => parameters
            .Add(component => component.MaximumRows, 10));
        await LoadAsync(cut,
            $"Nome;Preço;Quantidade;Código\nUm;10;1;{Guid.NewGuid()}\n",
            "valid.csv");
        Task import = cut.InvokeAsync(() => cut.Instance.ImportAsync());
        CancellationToken token = await handlerStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        cut.Instance.Dispose();
        await import.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
    }

    private static async Task LoadAsync(
        IRenderedComponent<OmniDataImport<Produto>> cut,
        string content,
        string fileName)
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(content));
        await cut.InvokeAsync(() => cut.Instance.LoadAsync(stream, fileName));
    }

    private sealed class BlockingStream : Stream
    {
        public TaskCompletionSource<CancellationToken> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
