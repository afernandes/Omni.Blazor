using System.Globalization;

namespace Omni.Blazor.Localization;

/// <summary>
/// Every user-facing string the library renders on its own (button labels, ARIA
/// names, empty states). The default facade resolves the current UI culture through
/// <see cref="IOmniLocalizer"/> on every access, so culture changes don't require a
/// new DI scope. Per-component parameters still take precedence.
///
/// <code>
/// builder.Services.AddOmniTranslations("fr-FR", translations);
/// builder.Services.AddOmniComponents();
/// </code>
///
/// <para>
/// Resolution order per string: the component's own <c>[Parameter]</c> (if the
/// consumer set one) → this instance → the built-in default. Defaults are pt-BR,
/// matching what the library has always rendered, so registering nothing changes
/// nothing.
/// </para>
/// <para>
/// Translation sources are composable. Use the built-in resources, a dictionary,
/// <c>IStringLocalizer</c>/RESX, a database-backed provider, or an optional PO provider.
/// Assigning <c>OmniOptions.Texts</c> remains available as a fixed legacy override.
/// </para>
/// </summary>
public class OmniTexts
{
    private readonly IOmniLocalizer? _localizer;
    private readonly Func<CultureInfo>? _cultureAccessor;
    private readonly Func<CultureInfo>? _formattingCultureAccessor;
    private readonly CultureInfo? _fixedTextCulture;

    /// <summary>Creates a mutable fixed-culture text set.</summary>
    public OmniTexts()
    {
    }

    private OmniTexts(CultureInfo fixedTextCulture)
        => _fixedTextCulture = fixedTextCulture;

    private OmniTexts(
        IOmniLocalizer localizer,
        Func<CultureInfo> cultureAccessor,
        Func<CultureInfo> formattingCultureAccessor)
    {
        _localizer = localizer;
        _cultureAccessor = cultureAccessor;
        _formattingCultureAccessor = formattingCultureAccessor;
    }

    internal bool IsLocalizedFacade => _localizer is not null;

    internal static OmniTexts FromLocalizer(
        IOmniLocalizer localizer,
        Func<CultureInfo>? cultureAccessor = null,
        Func<CultureInfo>? formattingCultureAccessor = null)
        => new(
            localizer,
            cultureAccessor ?? (static () => CultureInfo.CurrentUICulture),
            formattingCultureAccessor ?? (static () => CultureInfo.CurrentCulture));

    private string Resolve(string key, string fallback)
    {
        if (_localizer is null)
            return fallback;

        OmniLocalizedString result = _localizer.Localize(key, _cultureAccessor!());
        return result.ResourceNotFound ? fallback : result.Value;
    }

    internal string Plural(string key, decimal count, string fallback, params object?[] arguments)
    {
        string format = _localizer?.Plural(key, count, _cultureAccessor!())
            ?? (_fixedTextCulture is null
                ? fallback
                : OmniLocalizer.GetBuiltInPluralFormat(key, count, _fixedTextCulture) ?? fallback);
        return arguments.Length == 0
            ? format
            : string.Format(_formattingCultureAccessor?.Invoke() ?? CultureInfo.CurrentCulture, format, arguments);
    }

    /// <summary>Built-in pt-BR defaults, used when nothing is registered.</summary>
    public static OmniTexts Default { get; } = new(CultureInfo.GetCultureInfo("pt-BR"));

    // ── Generic actions ──────────────────────────────────────────────────
    /// <summary>Close (dialogs, alerts, banners). Default "Fechar".</summary>
    private string _valueForClose = "Fechar";
    public string Close { get => Resolve(nameof(Close), _valueForClose); set => _valueForClose = value; }
    /// <summary>Clear an input or a filter. Default "Limpar".</summary>
    private string _valueForClear = "Limpar";
    public string Clear { get => Resolve(nameof(Clear), _valueForClear); set => _valueForClear = value; }
    /// <summary>Clear everything. Default "Limpar tudo".</summary>
    private string _valueForClearAll = "Limpar tudo";
    public string ClearAll { get => Resolve(nameof(ClearAll), _valueForClearAll); set => _valueForClearAll = value; }
    /// <summary>Cancel. Default "Cancelar".</summary>
    private string _valueForCancel = "Cancelar";
    public string Cancel { get => Resolve(nameof(Cancel), _valueForCancel); set => _valueForCancel = value; }
    /// <summary>Confirm. Default "Confirmar".</summary>
    private string _valueForConfirm = "Confirmar";
    public string Confirm { get => Resolve(nameof(Confirm), _valueForConfirm); set => _valueForConfirm = value; }
    /// <summary>Apply. Default "Aplicar".</summary>
    private string _valueForApply = "Aplicar";
    public string Apply { get => Resolve(nameof(Apply), _valueForApply); set => _valueForApply = value; }
    /// <summary>Add. Default "Adicionar".</summary>
    private string _valueForAdd = "Adicionar";
    public string Add { get => Resolve(nameof(Add), _valueForAdd); set => _valueForAdd = value; }
    /// <summary>Edit. Default "Editar".</summary>
    private string _valueForEdit = "Editar";
    public string Edit { get => Resolve(nameof(Edit), _valueForEdit); set => _valueForEdit = value; }
    /// <summary>Export data. Default "Exportar".</summary>
    private string _valueForExport = "Exportar";
    public string Export { get => Resolve(nameof(Export), _valueForExport); set => _valueForExport = value; }
    /// <summary>Resize a data column.</summary>
    private string _valueForResizeColumn = "Redimensionar coluna";
    public string ResizeColumn { get => Resolve(nameof(ResizeColumn), _valueForResizeColumn); set => _valueForResizeColumn = value; }
    /// <summary>Drag a data column to create a group.</summary>
    private string _valueForDragColumnToGroup = "Arraste para agrupar";
    public string DragColumnToGroup { get => Resolve(nameof(DragColumnToGroup), _valueForDragColumnToGroup); set => _valueForDragColumnToGroup = value; }
    /// <summary>Task estimate metadata.</summary>
    private string _valueForEstimate = "Estimativa";
    public string Estimate { get => Resolve(nameof(Estimate), _valueForEstimate); set => _valueForEstimate = value; }
    /// <summary>Task subtasks metadata.</summary>
    private string _valueForSubtasks = "Subtarefas";
    public string Subtasks { get => Resolve(nameof(Subtasks), _valueForSubtasks); set => _valueForSubtasks = value; }
    /// <summary>Task due-date metadata.</summary>
    private string _valueForDueDate = "Prazo";
    public string DueDate { get => Resolve(nameof(DueDate), _valueForDueDate); set => _valueForDueDate = value; }
    /// <summary>Minute time unit.</summary>
    private string _valueForMinute = "Minuto";
    public string Minute { get => Resolve(nameof(Minute), _valueForMinute); set => _valueForMinute = value; }
    /// <summary>Second time unit.</summary>
    private string _valueForSecond = "Segundo";
    public string Second { get => Resolve(nameof(Second), _valueForSecond); set => _valueForSecond = value; }
    /// <summary>Accessible accent-color name format.</summary>
    private string _valueForAccentColorNamed = "Cor accent {0}";
    public string AccentColorNamed { get => Resolve(nameof(AccentColorNamed), _valueForAccentColorNamed); set => _valueForAccentColorNamed = value; }
    /// <summary>Generic warning dialog title.</summary>
    private string _valueForWarning = "Aviso";
    public string Warning { get => Resolve(nameof(Warning), _valueForWarning); set => _valueForWarning = value; }
    /// <summary>Generic alert acknowledgement.</summary>
    private string _valueForUnderstood = "Entendi";
    public string Understood { get => Resolve(nameof(Understood), _valueForUnderstood); set => _valueForUnderstood = value; }
    /// <summary>Preset shortcuts heading.</summary>
    private string _valueForShortcuts = "Atalhos";
    public string Shortcuts { get => Resolve(nameof(Shortcuts), _valueForShortcuts); set => _valueForShortcuts = value; }
    /// <summary>Keyboard navigation hint.</summary>
    private string _valueForNavigate = "navegar";
    public string Navigate { get => Resolve(nameof(Navigate), _valueForNavigate); set => _valueForNavigate = value; }
    /// <summary>Generic selection action.</summary>
    private string _valueForSelect = "selecionar";
    public string Select { get => Resolve(nameof(Select), _valueForSelect); set => _valueForSelect = value; }
    /// <summary>Empty Kanban column.</summary>
    private string _valueForNoCards = "Sem cards";
    public string NoCards { get => Resolve(nameof(NoCards), _valueForNoCards); set => _valueForNoCards = value; }
    // Shift and Enter are not here on purpose: they read the same in pt-BR and English,
    // and a locale that does differ (German "Umschalt") is served by the per-key
    // VirtualKeyboardKey.AriaLabel, which belongs to the layout rather than to a global set.
    /// <summary>Space bar of the virtual keyboard. Default "Espaço".</summary>
    private string _valueForKeyboardSpace = "Espaço";
    public string KeyboardSpace { get => Resolve(nameof(KeyboardSpace), _valueForKeyboardSpace); set => _valueForKeyboardSpace = value; }
    /// <summary>Accessible name of the virtual keyboard's Backspace key. Default "Apagar".</summary>
    private string _valueForKeyboardBackspace = "Apagar";
    public string KeyboardBackspace { get => Resolve(nameof(KeyboardBackspace), _valueForKeyboardBackspace); set => _valueForKeyboardBackspace = value; }
    /// <summary>Accessible name of the virtual keyboard's symbol-set key. Default "Símbolos".</summary>
    private string _valueForKeyboardSymbols = "Símbolos";
    public string KeyboardSymbols { get => Resolve(nameof(KeyboardSymbols), _valueForKeyboardSymbols); set => _valueForKeyboardSymbols = value; }
    /// <summary>Accessible name of the virtual keyboard itself. Default "Teclado virtual".</summary>
    private string _valueForKeyboardLabel = "Teclado virtual";
    public string KeyboardLabel { get => Resolve(nameof(KeyboardLabel), _valueForKeyboardLabel); set => _valueForKeyboardLabel = value; }
    /// <summary>Add a filter condition (data filter). Default "Adicionar condição".</summary>
    private string _valueForAddCondition = "Adicionar condição";
    public string AddCondition { get => Resolve(nameof(AddCondition), _valueForAddCondition); set => _valueForAddCondition = value; }
    /// <summary>Add a filter group (data filter). Default "Adicionar grupo".</summary>
    private string _valueForAddGroup = "Adicionar grupo";
    public string AddGroup { get => Resolve(nameof(AddGroup), _valueForAddGroup); set => _valueForAddGroup = value; }
    /// <summary>Apply the typed SQL back to the filter (data filter). Default "Aplicar ao filtro".</summary>
    private string _valueForApplyToFilter = "Aplicar ao filtro";
    public string ApplyToFilter { get => Resolve(nameof(ApplyToFilter), _valueForApplyToFilter); set => _valueForApplyToFilter = value; }
    /// <summary>Lower numeric range value. Default "Mínimo".</summary>
    private string _valueForDataFilterMinimum = "Mínimo";
    public string DataFilterMinimum { get => Resolve(nameof(DataFilterMinimum), _valueForDataFilterMinimum); set => _valueForDataFilterMinimum = value; }
    /// <summary>Upper numeric range value. Default "Máximo".</summary>
    private string _valueForDataFilterMaximum = "Máximo";
    public string DataFilterMaximum { get => Resolve(nameof(DataFilterMaximum), _valueForDataFilterMaximum); set => _valueForDataFilterMaximum = value; }
    /// <summary>Range start date. Default "Data inicial".</summary>
    private string _valueForDataFilterStartDate = "Data inicial";
    public string DataFilterStartDate { get => Resolve(nameof(DataFilterStartDate), _valueForDataFilterStartDate); set => _valueForDataFilterStartDate = value; }
    /// <summary>Range end date. Default "Data final".</summary>
    private string _valueForDataFilterEndDate = "Data final";
    public string DataFilterEndDate { get => Resolve(nameof(DataFilterEndDate), _valueForDataFilterEndDate); set => _valueForDataFilterEndDate = value; }
    /// <summary>Generic range start value. Default "Valor inicial".</summary>
    private string _valueForDataFilterStartValue = "Valor inicial";
    public string DataFilterStartValue { get => Resolve(nameof(DataFilterStartValue), _valueForDataFilterStartValue); set => _valueForDataFilterStartValue = value; }
    /// <summary>Generic range end value. Default "Valor final".</summary>
    private string _valueForDataFilterEndValue = "Valor final";
    public string DataFilterEndValue { get => Resolve(nameof(DataFilterEndValue), _valueForDataFilterEndValue); set => _valueForDataFilterEndValue = value; }
    /// <summary>AND group operator.</summary>
    private string _valueForAnd = "E";
    public string And { get => Resolve(nameof(And), _valueForAnd); set => _valueForAnd = value; }
    /// <summary>OR group operator.</summary>
    private string _valueForOr = "OU";
    public string Or { get => Resolve(nameof(Or), _valueForOr); set => _valueForOr = value; }
    /// <summary>Filter value placeholder.</summary>
    private string _valueForFilterValue = "valor";
    public string FilterValue { get => Resolve(nameof(FilterValue), _valueForFilterValue); set => _valueForFilterValue = value; }
    /// <summary>Visual query-builder mode.</summary>
    private string _valueForVisualMode = "Visual";
    public string VisualMode { get => Resolve(nameof(VisualMode), _valueForVisualMode); set => _valueForVisualMode = value; }
    /// <summary>Valid SQL status.</summary>
    private string _valueForSqlValid = "SQL válido";
    public string SqlValid { get => Resolve(nameof(SqlValid), _valueForSqlValid); set => _valueForSqlValid = value; }
    /// <summary>Generated SQL preview label.</summary>
    private string _valueForGeneratedSql = "SQL gerado";
    public string GeneratedSql { get => Resolve(nameof(GeneratedSql), _valueForGeneratedSql); set => _valueForGeneratedSql = value; }
    /// <summary>Available filter fields label.</summary>
    private string _valueForFields = "Campos";
    public string Fields { get => Resolve(nameof(Fields), _valueForFields); set => _valueForFields = value; }
    /// <summary>Single filter field placeholder.</summary>
    private string _valueForField = "Campo";
    public string Field { get => Resolve(nameof(Field), _valueForField); set => _valueForField = value; }
    /// <summary>Editable SQL filter example.</summary>
    private string _valueForSqlFilterPlaceholder = "ex.: Nome LIKE '%texto%' AND Idade >= 18";
    public string SqlFilterPlaceholder { get => Resolve(nameof(SqlFilterPlaceholder), _valueForSqlFilterPlaceholder); set => _valueForSqlFilterPlaceholder = value; }
    /// <summary>SQL filter editor accessible label.</summary>
    private string _valueForSqlFilter = "Filtro em SQL";
    public string SqlFilter { get => Resolve(nameof(SqlFilter), _valueForSqlFilter); set => _valueForSqlFilter = value; }
    /// <summary>Empty root filter guidance.</summary>
    private string _valueForNoFilterConditions = "Nenhuma condição ainda — comece adicionando uma abaixo.";
    public string NoFilterConditions { get => Resolve(nameof(NoFilterConditions), _valueForNoFilterConditions); set => _valueForNoFilterConditions = value; }
    /// <summary>Empty filter group.</summary>
    private string _valueForEmptyGroup = "Grupo vazio";
    public string EmptyGroup { get => Resolve(nameof(EmptyGroup), _valueForEmptyGroup); set => _valueForEmptyGroup = value; }
    /// <summary>Contains filter operator.</summary>
    private string _valueForFilterContains = "Contém";
    public string FilterContains { get => Resolve(nameof(FilterContains), _valueForFilterContains); set => _valueForFilterContains = value; }
    /// <summary>Does-not-contain filter operator.</summary>
    private string _valueForFilterNotContains = "Não contém";
    public string FilterNotContains { get => Resolve(nameof(FilterNotContains), _valueForFilterNotContains); set => _valueForFilterNotContains = value; }
    /// <summary>Starts-with filter operator.</summary>
    private string _valueForFilterStartsWith = "Começa com";
    public string FilterStartsWith { get => Resolve(nameof(FilterStartsWith), _valueForFilterStartsWith); set => _valueForFilterStartsWith = value; }
    /// <summary>Ends-with filter operator.</summary>
    private string _valueForFilterEndsWith = "Termina com";
    public string FilterEndsWith { get => Resolve(nameof(FilterEndsWith), _valueForFilterEndsWith); set => _valueForFilterEndsWith = value; }
    /// <summary>Equals filter operator.</summary>
    private string _valueForFilterEquals = "Igual a";
    public string FilterEquals { get => Resolve(nameof(FilterEquals), _valueForFilterEquals); set => _valueForFilterEquals = value; }
    /// <summary>Not-equal filter operator.</summary>
    private string _valueForFilterNotEquals = "Diferente de";
    public string FilterNotEquals { get => Resolve(nameof(FilterNotEquals), _valueForFilterNotEquals); set => _valueForFilterNotEquals = value; }
    /// <summary>Greater-than filter operator.</summary>
    private string _valueForFilterGreaterThan = "Maior que";
    public string FilterGreaterThan { get => Resolve(nameof(FilterGreaterThan), _valueForFilterGreaterThan); set => _valueForFilterGreaterThan = value; }
    /// <summary>Greater-than-or-equal filter operator.</summary>
    private string _valueForFilterGreaterThanOrEqual = "Maior ou igual a";
    public string FilterGreaterThanOrEqual { get => Resolve(nameof(FilterGreaterThanOrEqual), _valueForFilterGreaterThanOrEqual); set => _valueForFilterGreaterThanOrEqual = value; }
    /// <summary>Less-than filter operator.</summary>
    private string _valueForFilterLessThan = "Menor que";
    public string FilterLessThan { get => Resolve(nameof(FilterLessThan), _valueForFilterLessThan); set => _valueForFilterLessThan = value; }
    /// <summary>Less-than-or-equal filter operator.</summary>
    private string _valueForFilterLessThanOrEqual = "Menor ou igual a";
    public string FilterLessThanOrEqual { get => Resolve(nameof(FilterLessThanOrEqual), _valueForFilterLessThanOrEqual); set => _valueForFilterLessThanOrEqual = value; }
    /// <summary>Between filter operator.</summary>
    private string _valueForFilterBetween = "Entre";
    public string FilterBetween { get => Resolve(nameof(FilterBetween), _valueForFilterBetween); set => _valueForFilterBetween = value; }
    /// <summary>Outside-range filter operator.</summary>
    private string _valueForFilterNotBetween = "Fora do intervalo";
    public string FilterNotBetween { get => Resolve(nameof(FilterNotBetween), _valueForFilterNotBetween); set => _valueForFilterNotBetween = value; }
    /// <summary>Empty-value filter operator.</summary>
    private string _valueForFilterIsEmpty = "Vazio";
    public string FilterIsEmpty { get => Resolve(nameof(FilterIsEmpty), _valueForFilterIsEmpty); set => _valueForFilterIsEmpty = value; }
    /// <summary>Non-empty-value filter operator.</summary>
    private string _valueForFilterIsNotEmpty = "Não vazio";
    public string FilterIsNotEmpty { get => Resolve(nameof(FilterIsNotEmpty), _valueForFilterIsNotEmpty); set => _valueForFilterIsNotEmpty = value; }
    /// <summary>Remove. Default "Remover".</summary>
    private string _valueForRemove = "Remover";
    public string Remove { get => Resolve(nameof(Remove), _valueForRemove); set => _valueForRemove = value; }
    /// <summary>Send. Default "Enviar".</summary>
    private string _valueForSend = "Enviar";
    public string Send { get => Resolve(nameof(Send), _valueForSend); set => _valueForSend = value; }
    /// <summary>Copy. Default "Copiar".</summary>
    private string _valueForCopy = "Copiar";
    public string Copy { get => Resolve(nameof(Copy), _valueForCopy); set => _valueForCopy = value; }
    /// <summary>Row/card actions menu. Default "Ações".</summary>
    private string _valueForActions = "Ações";
    public string Actions { get => Resolve(nameof(Actions), _valueForActions); set => _valueForActions = value; }
    /// <summary>Select all visible rows.</summary>
    private string _valueForSelectAllRows = "Selecionar todas as linhas visíveis";
    public string SelectAllRows { get => Resolve(nameof(SelectAllRows), _valueForSelectAllRows); set => _valueForSelectAllRows = value; }
    /// <summary>Select one grid row.</summary>
    private string _valueForSelectRow = "Selecionar linha";
    public string SelectRow { get => Resolve(nameof(SelectRow), _valueForSelectRow); set => _valueForSelectRow = value; }
    /// <summary>Select an option in a choice input.</summary>
    private string _valueForSelectOption = "Selecionar opção";
    public string SelectOption { get => Resolve(nameof(SelectOption), _valueForSelectOption); set => _valueForSelectOption = value; }
    /// <summary>Save a form or record. Default "Salvar".</summary>
    private string _valueForSave = "Salvar";
    public string Save { get => Resolve(nameof(Save), _valueForSave); set => _valueForSave = value; }
    /// <summary>DataGridForm operation error heading.</summary>
    private string _valueForDataGridFormOperationFailed = "Não foi possível concluir a operação.";
    public string DataGridFormOperationFailed { get => Resolve(nameof(DataGridFormOperationFailed), _valueForDataGridFormOperationFailed); set => _valueForDataGridFormOperationFailed = value; }
    /// <summary>DataGridForm provider validation failure.</summary>
    private string _valueForDataGridFormValidationFailed = "Revise os dados informados.";
    public string DataGridFormValidationFailed { get => Resolve(nameof(DataGridFormValidationFailed), _valueForDataGridFormValidationFailed); set => _valueForDataGridFormValidationFailed = value; }
    /// <summary>DataGridForm optimistic concurrency conflict.</summary>
    private string _valueForDataGridFormConflict = "Este registro foi alterado por outra operação. Recarregue os dados e tente novamente.";
    public string DataGridFormConflict { get => Resolve(nameof(DataGridFormConflict), _valueForDataGridFormConflict); set => _valueForDataGridFormConflict = value; }
    /// <summary>DataGridForm missing record failure.</summary>
    private string _valueForDataGridFormNotFound = "Este registro não existe mais.";
    public string DataGridFormNotFound { get => Resolve(nameof(DataGridFormNotFound), _valueForDataGridFormNotFound); set => _valueForDataGridFormNotFound = value; }
    /// <summary>DataGridForm authorization failure.</summary>
    private string _valueForDataGridFormForbidden = "Você não tem permissão para concluir esta operação.";
    public string DataGridFormForbidden { get => Resolve(nameof(DataGridFormForbidden), _valueForDataGridFormForbidden); set => _valueForDataGridFormForbidden = value; }
    /// <summary>DataGridForm refresh failure after a committed mutation.</summary>
    private string _valueForDataGridFormRefreshFailed = "A alteração foi salva, mas não foi possível atualizar a grade.";
    public string DataGridFormRefreshFailed { get => Resolve(nameof(DataGridFormRefreshFailed), _valueForDataGridFormRefreshFailed); set => _valueForDataGridFormRefreshFailed = value; }
    /// <summary>DataGridForm unsaved changes confirmation heading.</summary>
    private string _valueForDataGridFormUnsavedChangesTitle = "Descartar alterações?";
    public string DataGridFormUnsavedChangesTitle { get => Resolve(nameof(DataGridFormUnsavedChangesTitle), _valueForDataGridFormUnsavedChangesTitle); set => _valueForDataGridFormUnsavedChangesTitle = value; }
    /// <summary>DataGridForm unsaved changes confirmation message.</summary>
    private string _valueForDataGridFormUnsavedChangesMessage = "Existem alterações não salvas. Deseja descartá-las?";
    public string DataGridFormUnsavedChangesMessage { get => Resolve(nameof(DataGridFormUnsavedChangesMessage), _valueForDataGridFormUnsavedChangesMessage); set => _valueForDataGridFormUnsavedChangesMessage = value; }
    /// <summary>DataGridForm discard action.</summary>
    private string _valueForDataGridFormDiscardChanges = "Descartar alterações";
    public string DataGridFormDiscardChanges { get => Resolve(nameof(DataGridFormDiscardChanges), _valueForDataGridFormDiscardChanges); set => _valueForDataGridFormDiscardChanges = value; }
    /// <summary>DataGridForm continue editing action.</summary>
    private string _valueForDataGridFormContinueEditing = "Continuar editando";
    public string DataGridFormContinueEditing { get => Resolve(nameof(DataGridFormContinueEditing), _valueForDataGridFormContinueEditing); set => _valueForDataGridFormContinueEditing = value; }
    /// <summary>DataGridForm bulk action toolbar accessible label.</summary>
    private string _valueForDataGridFormBulkActions = "Ações em massa";
    public string DataGridFormBulkActions { get => Resolve(nameof(DataGridFormBulkActions), _valueForDataGridFormBulkActions); set => _valueForDataGridFormBulkActions = value; }
    /// <summary>DataGridForm row or bulk overflow-menu accessible label.</summary>
    private string _valueForDataGridFormMoreActions = "Mais ações";
    public string DataGridFormMoreActions { get => Resolve(nameof(DataGridFormMoreActions), _valueForDataGridFormMoreActions); set => _valueForDataGridFormMoreActions = value; }
    /// <summary>DataGridForm selected-item count format; placeholder zero receives the count.</summary>
    private string _valueForDataGridFormSelectedCount = "{0} selecionados";
    public string DataGridFormSelectedCount { get => Resolve(nameof(DataGridFormSelectedCount), _valueForDataGridFormSelectedCount); set => _valueForDataGridFormSelectedCount = value; }
    /// <summary>Default DataGridForm bulk action confirmation.</summary>
    private string _valueForDataGridFormBulkConfirmation = "Deseja aplicar esta ação aos registros selecionados?";
    public string DataGridFormBulkConfirmation { get => Resolve(nameof(DataGridFormBulkConfirmation), _valueForDataGridFormBulkConfirmation); set => _valueForDataGridFormBulkConfirmation = value; }
    /// <summary>EntityPicker dialog heading.</summary>
    private string _valueForEntityPickerTitle = "Selecionar registro";
    public string EntityPickerTitle { get => Resolve(nameof(EntityPickerTitle), _valueForEntityPickerTitle); set => _valueForEntityPickerTitle = value; }
    /// <summary>EntityPicker empty value placeholder.</summary>
    private string _valueForEntityPickerPlaceholder = "Selecione um registro";
    public string EntityPickerPlaceholder { get => Resolve(nameof(EntityPickerPlaceholder), _valueForEntityPickerPlaceholder); set => _valueForEntityPickerPlaceholder = value; }
    /// <summary>EntityPicker generated column heading.</summary>
    private string _valueForEntityPickerItem = "Registro";
    public string EntityPickerItem { get => Resolve(nameof(EntityPickerItem), _valueForEntityPickerItem); set => _valueForEntityPickerItem = value; }
    /// <summary>DataFormWizard step navigation accessible label.</summary>
    private string _valueForDataFormWizardNavigation = "Etapas do formulário";
    public string DataFormWizardNavigation { get => Resolve(nameof(DataFormWizardNavigation), _valueForDataFormWizardNavigation); set => _valueForDataFormWizardNavigation = value; }
    /// <summary>DataImport file-selection prompt.</summary>
    private string _valueForDataImportUpload = "Selecione um arquivo CSV ou TSV";
    public string DataImportUpload { get => Resolve(nameof(DataImportUpload), _valueForDataImportUpload); set => _valueForDataImportUpload = value; }
    /// <summary>DataImport input constraints; placeholders receive maximum size and row count.</summary>
    private string _valueForDataImportUploadHint = "Até {0} e {1:N0} linhas";
    public string DataImportUploadHint { get => Resolve(nameof(DataImportUploadHint), _valueForDataImportUploadHint); set => _valueForDataImportUploadHint = value; }
    /// <summary>DataImport active processing status.</summary>
    private string _valueForDataImportProcessing = "Processando arquivo...";
    public string DataImportProcessing { get => Resolve(nameof(DataImportProcessing), _valueForDataImportProcessing); set => _valueForDataImportProcessing = value; }
    /// <summary>DataImport mapping section heading.</summary>
    private string _valueForDataImportMapping = "Mapeamento de colunas";
    public string DataImportMapping { get => Resolve(nameof(DataImportMapping), _valueForDataImportMapping); set => _valueForDataImportMapping = value; }
    /// <summary>DataImport mapping section guidance.</summary>
    private string _valueForDataImportMappingHint = "Associe cada campo de destino a uma coluna do arquivo.";
    public string DataImportMappingHint { get => Resolve(nameof(DataImportMappingHint), _valueForDataImportMappingHint); set => _valueForDataImportMappingHint = value; }
    /// <summary>DataImport unmapped source option.</summary>
    private string _valueForDataImportIgnoreColumn = "Não importar";
    public string DataImportIgnoreColumn { get => Resolve(nameof(DataImportIgnoreColumn), _valueForDataImportIgnoreColumn); set => _valueForDataImportIgnoreColumn = value; }
    /// <summary>DataImport preview section heading.</summary>
    private string _valueForDataImportPreview = "Pré-visualização validada";
    public string DataImportPreview { get => Resolve(nameof(DataImportPreview), _valueForDataImportPreview); set => _valueForDataImportPreview = value; }
    /// <summary>DataImport source row heading.</summary>
    private string _valueForDataImportRow = "Linha";
    public string DataImportRow { get => Resolve(nameof(DataImportRow), _valueForDataImportRow); set => _valueForDataImportRow = value; }
    /// <summary>DataImport row status heading.</summary>
    private string _valueForDataImportStatus = "Situação";
    public string DataImportStatus { get => Resolve(nameof(DataImportStatus), _valueForDataImportStatus); set => _valueForDataImportStatus = value; }
    /// <summary>DataImport error-list heading.</summary>
    private string _valueForDataImportErrors = "Erros";
    public string DataImportErrors { get => Resolve(nameof(DataImportErrors), _valueForDataImportErrors); set => _valueForDataImportErrors = value; }
    /// <summary>DataImport valid row status.</summary>
    private string _valueForDataImportValid = "Válida";
    public string DataImportValid { get => Resolve(nameof(DataImportValid), _valueForDataImportValid); set => _valueForDataImportValid = value; }
    /// <summary>DataImport invalid row status.</summary>
    private string _valueForDataImportInvalid = "Inválida";
    public string DataImportInvalid { get => Resolve(nameof(DataImportInvalid), _valueForDataImportInvalid); set => _valueForDataImportInvalid = value; }
    /// <summary>DataImport preview summary; placeholders receive three already pluralized count labels.</summary>
    private string _valueForDataImportSummary = "{0}, {1}, {2}";
    public string DataImportSummary { get => Resolve(nameof(DataImportSummary), _valueForDataImportSummary); set => _valueForDataImportSummary = value; }
    /// <summary>DataImport valid-row count.</summary>
    private string _valueForDataImportValidCount = "{0:N0} linhas válidas";
    public string DataImportValidCount { get => Resolve(nameof(DataImportValidCount), _valueForDataImportValidCount); set => _valueForDataImportValidCount = value; }
    /// <summary>DataImport invalid-row count.</summary>
    private string _valueForDataImportInvalidCount = "{0:N0} linhas inválidas";
    public string DataImportInvalidCount { get => Resolve(nameof(DataImportInvalidCount), _valueForDataImportInvalidCount); set => _valueForDataImportInvalidCount = value; }
    /// <summary>DataImport total-row count.</summary>
    private string _valueForDataImportTotalCount = "{0:N0} linhas no total";
    public string DataImportTotalCount { get => Resolve(nameof(DataImportTotalCount), _valueForDataImportTotalCount); set => _valueForDataImportTotalCount = value; }
    /// <summary>DataImport preview truncation; placeholders receive shown and total counts.</summary>
    private string _valueForDataImportPreviewLimit = "Exibindo as primeiras {0:N0} de {1:N0} linhas.";
    public string DataImportPreviewLimit { get => Resolve(nameof(DataImportPreviewLimit), _valueForDataImportPreviewLimit); set => _valueForDataImportPreviewLimit = value; }
    /// <summary>DataImport primary action.</summary>
    private string _valueForDataImportImport = "Importar dados";
    public string DataImportImport { get => Resolve(nameof(DataImportImport), _valueForDataImportImport); set => _valueForDataImportImport = value; }
    /// <summary>DataImport accepted count.</summary>
    private string _valueForDataImportReady = "{0:N0} linhas prontas para importar.";
    public string DataImportReady { get => Resolve(nameof(DataImportReady), _valueForDataImportReady); set => _valueForDataImportReady = value; }
    /// <summary>DataImport blocking validation status.</summary>
    private string _valueForDataImportResolveErrors = "Corrija ou remova as linhas inválidas antes de importar.";
    public string DataImportResolveErrors { get => Resolve(nameof(DataImportResolveErrors), _valueForDataImportResolveErrors); set => _valueForDataImportResolveErrors = value; }
    /// <summary>DataImport generated column format; placeholder receives a one-based index.</summary>
    private string _valueForDataImportColumn = "Coluna {0}";
    public string DataImportColumn { get => Resolve(nameof(DataImportColumn), _valueForDataImportColumn); set => _valueForDataImportColumn = value; }
    /// <summary>DataImport maximum file-size error; placeholder receives the formatted limit.</summary>
    private string _valueForDataImportFileTooLarge = "O arquivo ultrapassa o limite de {0}.";
    public string DataImportFileTooLarge { get => Resolve(nameof(DataImportFileTooLarge), _valueForDataImportFileTooLarge); set => _valueForDataImportFileTooLarge = value; }
    /// <summary>DataImport malformed quoted-field error; placeholder receives the unexpected character.</summary>
    private string _valueForDataImportUnexpectedCharacter = "Caractere inesperado '{0}' após um campo entre aspas.";
    public string DataImportUnexpectedCharacter { get => Resolve(nameof(DataImportUnexpectedCharacter), _valueForDataImportUnexpectedCharacter); set => _valueForDataImportUnexpectedCharacter = value; }
    /// <summary>DataImport unterminated quoted-field error.</summary>
    private string _valueForDataImportUnclosedQuote = "O arquivo contém um campo entre aspas não finalizado.";
    public string DataImportUnclosedQuote { get => Resolve(nameof(DataImportUnclosedQuote), _valueForDataImportUnclosedQuote); set => _valueForDataImportUnclosedQuote = value; }
    /// <summary>DataImport empty-file error.</summary>
    private string _valueForDataImportEmptyFile = "O arquivo está vazio.";
    public string DataImportEmptyFile { get => Resolve(nameof(DataImportEmptyFile), _valueForDataImportEmptyFile); set => _valueForDataImportEmptyFile = value; }
    /// <summary>DataImport file without data rows error.</summary>
    private string _valueForDataImportNoDataRows = "O arquivo não contém linhas de dados.";
    public string DataImportNoDataRows { get => Resolve(nameof(DataImportNoDataRows), _valueForDataImportNoDataRows); set => _valueForDataImportNoDataRows = value; }
    /// <summary>DataImport row-limit error.</summary>
    private string _valueForDataImportTooManyRows = "O arquivo ultrapassa o limite de linhas configurado.";
    public string DataImportTooManyRows { get => Resolve(nameof(DataImportTooManyRows), _valueForDataImportTooManyRows); set => _valueForDataImportTooManyRows = value; }
    /// <summary>DataImport column-limit error.</summary>
    private string _valueForDataImportTooManyColumns = "O arquivo ultrapassa o limite de colunas configurado.";
    public string DataImportTooManyColumns { get => Resolve(nameof(DataImportTooManyColumns), _valueForDataImportTooManyColumns); set => _valueForDataImportTooManyColumns = value; }
    /// <summary>DataImport cell-length error.</summary>
    private string _valueForDataImportCellTooLong = "Uma célula ultrapassa o limite de caracteres configurado.";
    public string DataImportCellTooLong { get => Resolve(nameof(DataImportCellTooLong), _valueForDataImportCellTooLong); set => _valueForDataImportCellTooLong = value; }
    /// <summary>DataImport required target value; placeholder receives the target header.</summary>
    private string _valueForDataImportRequiredValue = "{0} é obrigatório.";
    public string DataImportRequiredValue { get => Resolve(nameof(DataImportRequiredValue), _valueForDataImportRequiredValue); set => _valueForDataImportRequiredValue = value; }
    /// <summary>DataImport conversion failure; placeholder receives the target header.</summary>
    private string _valueForDataImportInvalidValue = "{0} possui um valor inválido.";
    public string DataImportInvalidValue { get => Resolve(nameof(DataImportInvalidValue), _valueForDataImportInvalidValue); set => _valueForDataImportInvalidValue = value; }
    /// <summary>Default DataGridForm delete confirmation.</summary>
    private string _valueForDataGridFormDeleteConfirmation = "Deseja remover este registro?";
    public string DataGridFormDeleteConfirmation { get => Resolve(nameof(DataGridFormDeleteConfirmation), _valueForDataGridFormDeleteConfirmation); set => _valueForDataGridFormDeleteConfirmation = value; }
    /// <summary>Default required-field validation. Default "Campo obrigatório.".</summary>
    private string _valueForRequired = "Campo obrigatório.";
    public string Required { get => Resolve(nameof(Required), _valueForRequired); set => _valueForRequired = value; }
    /// <summary>Generic validation failure.</summary>
    private string _valueForInvalidValue = "Valor inválido.";
    public string InvalidValue { get => Resolve(nameof(InvalidValue), _valueForInvalidValue); set => _valueForInvalidValue = value; }
    /// <summary>DataForm validation summary heading.</summary>
    private string _valueForDataFormValidationSummary = "Corrija os erros abaixo:";
    public string DataFormValidationSummary { get => Resolve(nameof(DataFormValidationSummary), _valueForDataFormValidationSummary); set => _valueForDataFormValidationSummary = value; }
    /// <summary>Move a collection item up. Default "Mover para cima".</summary>
    private string _valueForMoveUp = "Mover para cima";
    public string MoveUp { get => Resolve(nameof(MoveUp), _valueForMoveUp); set => _valueForMoveUp = value; }
    /// <summary>Move a collection item down. Default "Mover para baixo".</summary>
    private string _valueForMoveDown = "Mover para baixo";
    public string MoveDown { get => Resolve(nameof(MoveDown), _valueForMoveDown); set => _valueForMoveDown = value; }
    /// <summary>DataForm minimum collection count format.</summary>
    private string _valueForDataFormMinimumItems = "Adicione pelo menos {0} itens.";
    public string DataFormMinimumItems { get => Resolve(nameof(DataFormMinimumItems), _valueForDataFormMinimumItems); set => _valueForDataFormMinimumItems = value; }
    /// <summary>DataForm maximum collection count format.</summary>
    private string _valueForDataFormMaximumItems = "Mantenha no máximo {0} itens.";
    public string DataFormMaximumItems { get => Resolve(nameof(DataFormMaximumItems), _valueForDataFormMaximumItems); set => _valueForDataFormMaximumItems = value; }
    /// <summary>Boolean affirmative option. Default "Sim".</summary>
    private string _valueForYes = "Sim";
    public string Yes { get => Resolve(nameof(Yes), _valueForYes); set => _valueForYes = value; }
    /// <summary>Boolean negative option. Default "Não".</summary>
    private string _valueForNo = "Não";
    public string No { get => Resolve(nameof(No), _valueForNo); set => _valueForNo = value; }
    /// <summary>Nullable option without a value. Default "Não informado".</summary>
    private string _valueForNotProvided = "Não informado";
    public string NotProvided { get => Resolve(nameof(NotProvided), _valueForNotProvided); set => _valueForNotProvided = value; }
    /// <summary>Reveal a masked password. Default "Mostrar senha".</summary>
    private string _valueForShowPassword = "Mostrar senha";
    public string ShowPassword { get => Resolve(nameof(ShowPassword), _valueForShowPassword); set => _valueForShowPassword = value; }
    /// <summary>Mask a revealed password. Default "Ocultar senha".</summary>
    private string _valueForHidePassword = "Ocultar senha";
    public string HidePassword { get => Resolve(nameof(HidePassword), _valueForHidePassword); set => _valueForHidePassword = value; }

    // ── Navigation ───────────────────────────────────────────────────────
    /// <summary>Next (stepper, scheduler). Default "Próximo".</summary>
    private string _valueForNext = "Próximo";
    public string Next { get => Resolve(nameof(Next), _valueForNext); set => _valueForNext = value; }
    /// <summary>Previous (scheduler). Default "Anterior".</summary>
    private string _valueForPrevious = "Anterior";
    public string Previous { get => Resolve(nameof(Previous), _valueForPrevious); set => _valueForPrevious = value; }
    /// <summary>Back (stepper, command palette). Default "Voltar".</summary>
    private string _valueForBack = "Voltar";
    public string Back { get => Resolve(nameof(Back), _valueForBack); set => _valueForBack = value; }
    /// <summary>Finish a stepper. Default "Concluir".</summary>
    private string _valueForComplete = "Concluir";
    public string Complete { get => Resolve(nameof(Complete), _valueForComplete); set => _valueForComplete = value; }
    /// <summary>Today (scheduler). Default "Hoje".</summary>
    private string _valueForToday = "Hoje";
    public string Today { get => Resolve(nameof(Today), _valueForToday); set => _valueForToday = value; }
    /// <summary>Multi-day scheduler view.</summary>
    private string _valueForMultiDay = "Multi-dias";
    public string MultiDay { get => Resolve(nameof(MultiDay), _valueForMultiDay); set => _valueForMultiDay = value; }
    /// <summary>Year timeline scheduler view.</summary>
    private string _valueForTimeline = "Linha do tempo";
    public string Timeline { get => Resolve(nameof(Timeline), _valueForTimeline); set => _valueForTimeline = value; }
    /// <summary>Year planner scheduler view.</summary>
    private string _valueForPlanner = "Planejador";
    public string Planner { get => Resolve(nameof(Planner), _valueForPlanner); set => _valueForPlanner = value; }
    /// <summary>Month-view overflow format.</summary>
    private string _valueForMoreAppointments = "+ {0} mais";
    public string MoreAppointments { get => Resolve(nameof(MoreAppointments), _valueForMoreAppointments); set => _valueForMoreAppointments = value; }
    /// <summary>Next month (calendar). Default "Próximo mês".</summary>
    private string _valueForNextMonth = "Próximo mês";
    public string NextMonth { get => Resolve(nameof(NextMonth), _valueForNextMonth); set => _valueForNextMonth = value; }
    /// <summary>Previous month (calendar). Default "Mês anterior".</summary>
    private string _valueForPreviousMonth = "Mês anterior";
    public string PreviousMonth { get => Resolve(nameof(PreviousMonth), _valueForPreviousMonth); set => _valueForPreviousMonth = value; }
    /// <summary>Next slide (carousel). Default "Próximo slide".</summary>
    private string _valueForNextSlide = "Próximo slide";
    public string NextSlide { get => Resolve(nameof(NextSlide), _valueForNextSlide); set => _valueForNextSlide = value; }
    /// <summary>Previous slide (carousel). Default "Slide anterior".</summary>
    private string _valueForPreviousSlide = "Slide anterior";
    public string PreviousSlide { get => Resolve(nameof(PreviousSlide), _valueForPreviousSlide); set => _valueForPreviousSlide = value; }
    /// <summary>Skip link target label. Default "Pular para o conteúdo".</summary>
    private string _valueForSkipToContent = "Pular para o conteúdo";
    public string SkipToContent { get => Resolve(nameof(SkipToContent), _valueForSkipToContent); set => _valueForSkipToContent = value; }
    /// <summary>Scroll-to-top button. Default "Voltar ao topo".</summary>
    private string _valueForScrollToTop = "Voltar ao topo";
    public string ScrollToTop { get => Resolve(nameof(ScrollToTop), _valueForScrollToTop); set => _valueForScrollToTop = value; }
    /// <summary>Close the navigation drawer/pane. Default "Fechar navegação".</summary>
    private string _valueForCloseNavigation = "Fechar navegação";
    public string CloseNavigation { get => Resolve(nameof(CloseNavigation), _valueForCloseNavigation); set => _valueForCloseNavigation = value; }
    /// <summary>Open a FAB menu. Default "Abrir menu".</summary>
    private string _valueForOpenMenu = "Abrir menu";
    public string OpenMenu { get => Resolve(nameof(OpenMenu), _valueForOpenMenu); set => _valueForOpenMenu = value; }
    /// <summary>Close a FAB menu. Default "Fechar menu".</summary>
    private string _valueForCloseMenu = "Fechar menu";
    public string CloseMenu { get => Resolve(nameof(CloseMenu), _valueForCloseMenu); set => _valueForCloseMenu = value; }
    /// <summary>Open navigation pane.</summary>
    private string _valueForOpenNavigation = "Abrir navegação";
    public string OpenNavigation { get => Resolve(nameof(OpenNavigation), _valueForOpenNavigation); set => _valueForOpenNavigation = value; }
    /// <summary>More options accessible label.</summary>
    private string _valueForMoreOptions = "Mais opções";
    public string MoreOptions { get => Resolve(nameof(MoreOptions), _valueForMoreOptions); set => _valueForMoreOptions = value; }
    /// <summary>Collapse the next splitter pane.</summary>
    private string _valueForCollapseNextPane = "Colapsar próximo painel";
    public string CollapseNextPane { get => Resolve(nameof(CollapseNextPane), _valueForCollapseNextPane); set => _valueForCollapseNextPane = value; }
    /// <summary>Expand the next splitter pane.</summary>
    private string _valueForExpandNextPane = "Expandir próximo painel";
    public string ExpandNextPane { get => Resolve(nameof(ExpandNextPane), _valueForExpandNextPane); set => _valueForExpandNextPane = value; }

    // ── Search / data ────────────────────────────────────────────────────
    /// <summary>Search box placeholder. Default "Buscar...".</summary>
    private string _valueForSearchPlaceholder = "Buscar...";
    public string SearchPlaceholder { get => Resolve(nameof(SearchPlaceholder), _valueForSearchPlaceholder); set => _valueForSearchPlaceholder = value; }
    /// <summary>Command palette placeholder. Default "Buscar comando...".</summary>
    private string _valueForCommandPlaceholder = "Buscar comando...";
    public string CommandPlaceholder { get => Resolve(nameof(CommandPlaceholder), _valueForCommandPlaceholder); set => _valueForCommandPlaceholder = value; }
    /// <summary>Recent command section.</summary>
    private string _valueForRecent = "Recentes";
    public string Recent { get => Resolve(nameof(Recent), _valueForRecent); set => _valueForRecent = value; }
    /// <summary>Global search accessible label.</summary>
    private string _valueForGlobalSearch = "Busca global";
    public string GlobalSearch { get => Resolve(nameof(GlobalSearch), _valueForGlobalSearch); set => _valueForGlobalSearch = value; }
    /// <summary>Global search placeholder.</summary>
    private string _valueForGlobalSearchPlaceholder = "Buscar em todo o sistema...";
    public string GlobalSearchPlaceholder { get => Resolve(nameof(GlobalSearchPlaceholder), _valueForGlobalSearchPlaceholder); set => _valueForGlobalSearchPlaceholder = value; }
    /// <summary>Global search initial guidance.</summary>
    private string _valueForTypeToSearch = "Digite para buscar";
    public string TypeToSearch { get => Resolve(nameof(TypeToSearch), _valueForTypeToSearch); set => _valueForTypeToSearch = value; }
    /// <summary>Global search failure.</summary>
    private string _valueForSearchFailed = "Não foi possível concluir a busca.";
    public string SearchFailed { get => Resolve(nameof(SearchFailed), _valueForSearchFailed); set => _valueForSearchFailed = value; }
    /// <summary>Clear the global search query.</summary>
    private string _valueForClearSearch = "Limpar busca";
    public string ClearSearch { get => Resolve(nameof(ClearSearch), _valueForClearSearch); set => _valueForClearSearch = value; }
    /// <summary>Unsaved-navigation prompt title.</summary>
    private string _valueForExitWithoutSavingTitle = "Sair sem salvar?";
    public string ExitWithoutSavingTitle { get => Resolve(nameof(ExitWithoutSavingTitle), _valueForExitWithoutSavingTitle); set => _valueForExitWithoutSavingTitle = value; }
    /// <summary>Unsaved-navigation prompt message.</summary>
    private string _valueForExitWithoutSavingMessage = "Você tem alterações não salvas. Deseja sair mesmo assim?";
    public string ExitWithoutSavingMessage { get => Resolve(nameof(ExitWithoutSavingMessage), _valueForExitWithoutSavingMessage); set => _valueForExitWithoutSavingMessage = value; }
    /// <summary>Confirm navigation without saving.</summary>
    private string _valueForExitWithoutSaving = "Sair sem salvar";
    public string ExitWithoutSaving { get => Resolve(nameof(ExitWithoutSaving), _valueForExitWithoutSaving); set => _valueForExitWithoutSaving = value; }
    /// <summary>Continue editing after a navigation prompt.</summary>
    private string _valueForContinueEditing = "Continuar editando";
    public string ContinueEditing { get => Resolve(nameof(ContinueEditing), _valueForContinueEditing); set => _valueForContinueEditing = value; }
    /// <summary>Appearance-mode control label.</summary>
    private string _valueForAppearanceMode = "Modo de aparência";
    public string AppearanceMode { get => Resolve(nameof(AppearanceMode), _valueForAppearanceMode); set => _valueForAppearanceMode = value; }
    /// <summary>Light appearance mode.</summary>
    private string _valueForLight = "Claro";
    public string Light { get => Resolve(nameof(Light), _valueForLight); set => _valueForLight = value; }
    /// <summary>Dark appearance mode.</summary>
    private string _valueForDark = "Escuro";
    public string Dark { get => Resolve(nameof(Dark), _valueForDark); set => _valueForDark = value; }
    /// <summary>System appearance mode.</summary>
    private string _valueForSystem = "Sistema";
    public string System { get => Resolve(nameof(System), _valueForSystem); set => _valueForSystem = value; }
    /// <summary>Theme-picker title.</summary>
    private string _valueForTheme = "Tema";
    public string Theme { get => Resolve(nameof(Theme), _valueForTheme); set => _valueForTheme = value; }
    private string _valueForLanguage = "Idioma";
    public string Language { get => Resolve(nameof(Language), _valueForLanguage); set => _valueForLanguage = value; }
    /// <summary>Default density label.</summary>
    private string _valueForDefaultDensity = "Padrão";
    public string DefaultDensity { get => Resolve(nameof(DefaultDensity), _valueForDefaultDensity); set => _valueForDefaultDensity = value; }
    /// <summary>Spacious density label.</summary>
    private string _valueForSpaciousDensity = "Espaçoso";
    public string SpaciousDensity { get => Resolve(nameof(SpaciousDensity), _valueForSpaciousDensity); set => _valueForSpaciousDensity = value; }
    /// <summary>Accent-color picker label.</summary>
    private string _valueForAccentColor = "Cor accent";
    public string AccentColor { get => Resolve(nameof(AccentColor), _valueForAccentColor); set => _valueForAccentColor = value; }
    /// <summary>Appearance-mode section label.</summary>
    private string _valueForMode = "Modo";
    public string Mode { get => Resolve(nameof(Mode), _valueForMode); set => _valueForMode = value; }
    /// <summary>Layout density section label.</summary>
    private string _valueForDensity = "Densidade";
    public string Density { get => Resolve(nameof(Density), _valueForDensity); set => _valueForDensity = value; }
    /// <summary>Compact density label.</summary>
    private string _valueForCompactDensity = "Compacto";
    public string CompactDensity { get => Resolve(nameof(CompactDensity), _valueForCompactDensity); set => _valueForCompactDensity = value; }
    /// <summary>Filter box placeholder. Default "Filtrar…".</summary>
    private string _valueForFilterPlaceholder = "Filtrar…";
    public string FilterPlaceholder { get => Resolve(nameof(FilterPlaceholder), _valueForFilterPlaceholder); set => _valueForFilterPlaceholder = value; }
    /// <summary>Loading indicator. Default "Buscando…".</summary>
    private string _valueForSearching = "Buscando…";
    public string Searching { get => Resolve(nameof(Searching), _valueForSearching); set => _valueForSearching = value; }
    /// <summary>Empty result list. Default "Nenhum resultado.".</summary>
    private string _valueForNoResults = "Nenhum resultado.";
    public string NoResults { get => Resolve(nameof(NoResults), _valueForNoResults); set => _valueForNoResults = value; }
    /// <summary>Empty data grid. Default "Nenhum registro encontrado.".</summary>
    private string _valueForNoRecords = "Nenhum registro encontrado.";
    public string NoRecords { get => Resolve(nameof(NoRecords), _valueForNoRecords); set => _valueForNoRecords = value; }
    /// <summary>File manager accessible label.</summary>
    private string _valueForFileManager = "Gerenciador de arquivos";
    public string FileManager { get => Resolve(nameof(FileManager), _valueForFileManager); set => _valueForFileManager = value; }
    /// <summary>File manager breadcrumb label.</summary>
    private string _valueForLocation = "Localização";
    public string Location { get => Resolve(nameof(Location), _valueForLocation); set => _valueForLocation = value; }
    /// <summary>Create a folder.</summary>
    private string _valueForNewFolder = "Nova pasta";
    public string NewFolder { get => Resolve(nameof(NewFolder), _valueForNewFolder); set => _valueForNewFolder = value; }
    /// <summary>Folder-name placeholder.</summary>
    private string _valueForFolderName = "Nome da pasta";
    public string FolderName { get => Resolve(nameof(FolderName), _valueForFolderName); set => _valueForFolderName = value; }
    /// <summary>Rename a file-system entry.</summary>
    private string _valueForRename = "Renomear";
    public string Rename { get => Resolve(nameof(Rename), _valueForRename); set => _valueForRename = value; }
    /// <summary>New-name placeholder.</summary>
    private string _valueForNewName = "Novo nome";
    public string NewName { get => Resolve(nameof(NewName), _valueForNewName); set => _valueForNewName = value; }
    /// <summary>Delete a file-system entry.</summary>
    private string _valueForDelete = "Excluir";
    public string Delete { get => Resolve(nameof(Delete), _valueForDelete); set => _valueForDelete = value; }
    /// <summary>File-system delete confirmation.</summary>
    private string _valueForDeleteNamedItem = "Excluir “{0}”?";
    public string DeleteNamedItem { get => Resolve(nameof(DeleteNamedItem), _valueForDeleteNamedItem); set => _valueForDeleteNamedItem = value; }
    /// <summary>Upload files.</summary>
    private string _valueForUpload = "Enviar";
    public string Upload { get => Resolve(nameof(Upload), _valueForUpload); set => _valueForUpload = value; }
    /// <summary>Download a file.</summary>
    private string _valueForDownload = "Baixar";
    public string Download { get => Resolve(nameof(Download), _valueForDownload); set => _valueForDownload = value; }
    /// <summary>Refresh data.</summary>
    private string _valueForRefresh = "Atualizar";
    public string Refresh { get => Resolve(nameof(Refresh), _valueForRefresh); set => _valueForRefresh = value; }
    /// <summary>Switch to list view.</summary>
    private string _valueForListView = "Exibição em lista";
    public string ListView { get => Resolve(nameof(ListView), _valueForListView); set => _valueForListView = value; }
    /// <summary>Switch to grid view.</summary>
    private string _valueForGridView = "Exibição em grade";
    public string GridView { get => Resolve(nameof(GridView), _valueForGridView); set => _valueForGridView = value; }
    /// <summary>Search inside the current folder.</summary>
    private string _valueForSearchFolder = "Buscar nesta pasta";
    public string SearchFolder { get => Resolve(nameof(SearchFolder), _valueForSearchFolder); set => _valueForSearchFolder = value; }
    /// <summary>File-manager loading state.</summary>
    private string _valueForLoadingFiles = "Carregando arquivos...";
    public string LoadingFiles { get => Resolve(nameof(LoadingFiles), _valueForLoadingFiles); set => _valueForLoadingFiles = value; }
    /// <summary>Empty folder state.</summary>
    private string _valueForEmptyFolder = "Esta pasta está vazia.";
    public string EmptyFolder { get => Resolve(nameof(EmptyFolder), _valueForEmptyFolder); set => _valueForEmptyFolder = value; }
    /// <summary>Generic file operation failure.</summary>
    private string _valueForFileOperationFailed = "Não foi possível concluir a operação.";
    public string FileOperationFailed { get => Resolve(nameof(FileOperationFailed), _valueForFileOperationFailed); set => _valueForFileOperationFailed = value; }
    /// <summary>Visible and total item count format.</summary>
    private string _valueForItemsCount = "{0} de {1} itens";
    public string ItemsCount { get => Resolve(nameof(ItemsCount), _valueForItemsCount); set => _valueForItemsCount = value; }
    /// <summary>Item limit format.</summary>
    private string _valueForItemLimit = "Limite de {0} itens";
    public string ItemLimit { get => Resolve(nameof(ItemLimit), _valueForItemLimit); set => _valueForItemLimit = value; }
    /// <summary>Generic loading state.</summary>
    private string _valueForLoading = "Carregando...";
    public string Loading { get => Resolve(nameof(Loading), _valueForLoading); set => _valueForLoading = value; }
    private string _valueForLoadingStatus = "Carregando";
    public string LoadingStatus { get => Resolve(nameof(LoadingStatus), _valueForLoadingStatus); set => _valueForLoadingStatus = value; }
    /// <summary>Remote option loading failure.</summary>
    private string _valueForLoadOptionsError = "Não foi possível carregar as opções.";
    public string LoadOptionsError { get => Resolve(nameof(LoadOptionsError), _valueForLoadOptionsError); set => _valueForLoadOptionsError = value; }
    /// <summary>Retry a failed operation.</summary>
    private string _valueForRetry = "Tentar novamente";
    public string Retry { get => Resolve(nameof(Retry), _valueForRetry); set => _valueForRetry = value; }
    /// <summary>Load the next page of options.</summary>
    private string _valueForLoadMore = "Carregar mais";
    public string LoadMore { get => Resolve(nameof(LoadMore), _valueForLoadMore); set => _valueForLoadMore = value; }
    /// <summary>Empty choice list.</summary>
    private string _valueForNoOptions = "Sem opções";
    public string NoOptions { get => Resolve(nameof(NoOptions), _valueForNoOptions); set => _valueForNoOptions = value; }
    /// <summary>Fallback text for a selected value not loaded yet.</summary>
    private string _valueForSelectedOption = "Opção selecionada";
    public string SelectedOption { get => Resolve(nameof(SelectedOption), _valueForSelectedOption); set => _valueForSelectedOption = value; }
    /// <summary>Date range input placeholder.</summary>
    private string _valueForSelectDateRange = "Selecionar período";
    public string SelectDateRange { get => Resolve(nameof(SelectDateRange), _valueForSelectDateRange); set => _valueForSelectDateRange = value; }
    /// <summary>Date range pending selection guidance.</summary>
    private string _valueForSelectRangeEnd = "Início: {0} · selecione o fim";
    public string SelectRangeEnd { get => Resolve(nameof(SelectRangeEnd), _valueForSelectRangeEnd); set => _valueForSelectRangeEnd = value; }
    /// <summary>Open a calendar popup.</summary>
    private string _valueForOpenCalendar = "Abrir calendário";
    public string OpenCalendar { get => Resolve(nameof(OpenCalendar), _valueForOpenCalendar); set => _valueForOpenCalendar = value; }
    /// <summary>Color transparency control.</summary>
    private string _valueForTransparency = "Transparência";
    public string Transparency { get => Resolve(nameof(Transparency), _valueForTransparency); set => _valueForTransparency = value; }
    /// <summary>Security-code cell label.</summary>
    private string _valueForDigit = "Dígito";
    public string Digit { get => Resolve(nameof(Digit), _valueForDigit); set => _valueForDigit = value; }
    /// <summary>Signature drawing area label.</summary>
    private string _valueForSignatureArea = "Área para assinatura";
    public string SignatureArea { get => Resolve(nameof(SignatureArea), _valueForSignatureArea); set => _valueForSignatureArea = value; }
    /// <summary>Undo the last signature stroke.</summary>
    private string _valueForUndo = "Desfazer";
    public string Undo { get => Resolve(nameof(Undo), _valueForUndo); set => _valueForUndo = value; }
    /// <summary>Empty signature status.</summary>
    private string _valueForAwaitingSignature = "Aguardando assinatura";
    public string AwaitingSignature { get => Resolve(nameof(AwaitingSignature), _valueForAwaitingSignature); set => _valueForAwaitingSignature = value; }
    /// <summary>Captured signature status.</summary>
    private string _valueForSignatureCaptured = "Assinatura capturada";
    public string SignatureCaptured { get => Resolve(nameof(SignatureCaptured), _valueForSignatureCaptured); set => _valueForSignatureCaptured = value; }
    /// <summary>File upload drop-zone guidance.</summary>
    private string _valueForChooseFiles = "Arraste arquivos aqui ou clique para selecionar";
    public string ChooseFiles { get => Resolve(nameof(ChooseFiles), _valueForChooseFiles); set => _valueForChooseFiles = value; }
    /// <summary>File size constraint format.</summary>
    private string _valueForUpToSize = "até {0}";
    public string UpToSize { get => Resolve(nameof(UpToSize), _valueForUpToSize); set => _valueForUpToSize = value; }
    /// <summary>Password strength label.</summary>
    private string _valueForPasswordStrength = "Força da senha";
    public string PasswordStrength { get => Resolve(nameof(PasswordStrength), _valueForPasswordStrength); set => _valueForPasswordStrength = value; }
    /// <summary>Password uppercase requirement.</summary>
    private string _valueForPasswordUppercase = "Uma letra maiúscula";
    public string PasswordUppercase { get => Resolve(nameof(PasswordUppercase), _valueForPasswordUppercase); set => _valueForPasswordUppercase = value; }
    /// <summary>Password lowercase requirement.</summary>
    private string _valueForPasswordLowercase = "Uma letra minúscula";
    public string PasswordLowercase { get => Resolve(nameof(PasswordLowercase), _valueForPasswordLowercase); set => _valueForPasswordLowercase = value; }
    /// <summary>Password digit requirement.</summary>
    private string _valueForPasswordDigit = "Um número";
    public string PasswordDigit { get => Resolve(nameof(PasswordDigit), _valueForPasswordDigit); set => _valueForPasswordDigit = value; }
    /// <summary>Password symbol requirement.</summary>
    private string _valueForPasswordSymbol = "Um símbolo";
    public string PasswordSymbol { get => Resolve(nameof(PasswordSymbol), _valueForPasswordSymbol); set => _valueForPasswordSymbol = value; }
    /// <summary>Weak password score.</summary>
    private string _valueForPasswordWeak = "Fraca";
    public string PasswordWeak { get => Resolve(nameof(PasswordWeak), _valueForPasswordWeak); set => _valueForPasswordWeak = value; }
    /// <summary>Fair password score.</summary>
    private string _valueForPasswordFair = "Razoável";
    public string PasswordFair { get => Resolve(nameof(PasswordFair), _valueForPasswordFair); set => _valueForPasswordFair = value; }
    /// <summary>Good password score.</summary>
    private string _valueForPasswordGood = "Boa";
    public string PasswordGood { get => Resolve(nameof(PasswordGood), _valueForPasswordGood); set => _valueForPasswordGood = value; }
    /// <summary>Strong password score.</summary>
    private string _valueForPasswordStrong = "Forte";
    public string PasswordStrong { get => Resolve(nameof(PasswordStrong), _valueForPasswordStrong); set => _valueForPasswordStrong = value; }
    /// <summary>Empty transfer list.</summary>
    private string _valueForEmptyList = "Lista vazia";
    public string EmptyList { get => Resolve(nameof(EmptyList), _valueForEmptyList); set => _valueForEmptyList = value; }
    /// <summary>Transfer-list source label.</summary>
    private string _valueForSource = "Origem";
    public string Source { get => Resolve(nameof(Source), _valueForSource); set => _valueForSource = value; }
    /// <summary>Transfer-list destination label.</summary>
    private string _valueForDestination = "Destino";
    public string Destination { get => Resolve(nameof(Destination), _valueForDestination); set => _valueForDestination = value; }
    /// <summary>Move selected items to the destination.</summary>
    private string _valueForMoveSelectedToDestination = "Mover selecionados para o destino";
    public string MoveSelectedToDestination { get => Resolve(nameof(MoveSelectedToDestination), _valueForMoveSelectedToDestination); set => _valueForMoveSelectedToDestination = value; }
    /// <summary>Move selected items to the source.</summary>
    private string _valueForMoveSelectedToSource = "Mover selecionados para a origem";
    public string MoveSelectedToSource { get => Resolve(nameof(MoveSelectedToSource), _valueForMoveSelectedToSource); set => _valueForMoveSelectedToSource = value; }
    /// <summary>Move all items to the destination.</summary>
    private string _valueForMoveAllToDestination = "Mover todos para o destino";
    public string MoveAllToDestination { get => Resolve(nameof(MoveAllToDestination), _valueForMoveAllToDestination); set => _valueForMoveAllToDestination = value; }
    /// <summary>Move all items to the source.</summary>
    private string _valueForMoveAllToSource = "Mover todos para a origem";
    public string MoveAllToSource { get => Resolve(nameof(MoveAllToSource), _valueForMoveAllToSource); set => _valueForMoveAllToSource = value; }
    /// <summary>Rating accessible label format.</summary>
    private string _valueForRating = "Avaliação {0} de {1}";
    public string Rating { get => Resolve(nameof(Rating), _valueForRating); set => _valueForRating = value; }
    /// <summary>Generic value label.</summary>
    private string _valueForValue = "Valor";
    public string Value { get => Resolve(nameof(Value), _valueForValue); set => _valueForValue = value; }
    /// <summary>Last seven days date-range preset.</summary>
    private string _valueForLastSevenDays = "Últimos 7 dias";
    public string LastSevenDays { get => Resolve(nameof(LastSevenDays), _valueForLastSevenDays); set => _valueForLastSevenDays = value; }
    /// <summary>Last thirty days date-range preset.</summary>
    private string _valueForLastThirtyDays = "Últimos 30 dias";
    public string LastThirtyDays { get => Resolve(nameof(LastThirtyDays), _valueForLastThirtyDays); set => _valueForLastThirtyDays = value; }
    /// <summary>Current month date-range preset.</summary>
    private string _valueForThisMonth = "Este mês";
    public string ThisMonth { get => Resolve(nameof(ThisMonth), _valueForThisMonth); set => _valueForThisMonth = value; }
    /// <summary>Previous month date-range preset.</summary>
    private string _valueForLastMonth = "Mês passado";
    public string LastMonth { get => Resolve(nameof(LastMonth), _valueForLastMonth); set => _valueForLastMonth = value; }
    /// <summary>Yesterday date-range preset.</summary>
    private string _valueForYesterday = "Ontem";
    public string Yesterday { get => Resolve(nameof(Yesterday), _valueForYesterday); set => _valueForYesterday = value; }
    /// <summary>Current year date-range preset.</summary>
    private string _valueForThisYear = "Este ano";
    public string ThisYear { get => Resolve(nameof(ThisYear), _valueForThisYear); set => _valueForThisYear = value; }
    /// <summary>Prompt to select the first date of a range.</summary>
    private string _valueForSelectRangeStart = "Selecione a data inicial";
    public string SelectRangeStart { get => Resolve(nameof(SelectRangeStart), _valueForSelectRangeStart); set => _valueForSelectRangeStart = value; }
    /// <summary>Completed date-range summary.</summary>
    private string _valueForDateRangeSummary = "{0} → {1} · {2} dias";
    public string DateRangeSummary { get => Resolve(nameof(DateRangeSummary), _valueForDateRangeSummary); set => _valueForDateRangeSummary = value; }
    /// <summary>Remove a grouping chip (data grid). Default "Remover agrupamento".</summary>
    private string _valueForRemoveGrouping = "Remover agrupamento";
    public string RemoveGrouping { get => Resolve(nameof(RemoveGrouping), _valueForRemoveGrouping); set => _valueForRemoveGrouping = value; }
    /// <summary>Visible-column menu heading.</summary>
    private string _valueForVisibleColumns = "Colunas visíveis";
    public string VisibleColumns { get => Resolve(nameof(VisibleColumns), _valueForVisibleColumns); set => _valueForVisibleColumns = value; }
    /// <summary>Hierarchical table accessible label.</summary>
    private string _valueForHierarchicalTable = "Tabela hierárquica";
    public string HierarchicalTable { get => Resolve(nameof(HierarchicalTable), _valueForHierarchicalTable); set => _valueForHierarchicalTable = value; }
    /// <summary>Expand a tree row.</summary>
    private string _valueForExpand = "Expandir";
    public string Expand { get => Resolve(nameof(Expand), _valueForExpand); set => _valueForExpand = value; }
    /// <summary>Collapse a tree row.</summary>
    private string _valueForCollapse = "Recolher";
    public string Collapse { get => Resolve(nameof(Collapse), _valueForCollapse); set => _valueForCollapse = value; }
    /// <summary>Tree child loading failure.</summary>
    private string _valueForHierarchyLoadError = "Não foi possível carregar os itens.";
    public string HierarchyLoadError { get => Resolve(nameof(HierarchyLoadError), _valueForHierarchyLoadError); set => _valueForHierarchyLoadError = value; }
    /// <summary>Tree row limit message format.</summary>
    private string _valueForHierarchyLimitReached = "Exibindo no máximo {0} linhas.";
    public string HierarchyLimitReached { get => Resolve(nameof(HierarchyLimitReached), _valueForHierarchyLimitReached); set => _valueForHierarchyLimitReached = value; }
    /// <summary>Data-grid grouping drop-zone guidance.</summary>
    private string _valueForGroupPanel = "Arraste o cabeçalho de uma coluna aqui para agrupar";
    public string GroupPanel { get => Resolve(nameof(GroupPanel), _valueForGroupPanel); set => _valueForGroupPanel = value; }
    /// <summary>Data-grid group limit message format.</summary>
    private string _valueForGroupLimitReached = "Exibindo no máximo {0} grupos.";
    public string GroupLimitReached { get => Resolve(nameof(GroupLimitReached), _valueForGroupLimitReached); set => _valueForGroupLimitReached = value; }
    /// <summary>Fit a diagram or timeline to the viewport.</summary>
    private string _valueForFitToView = "Ajustar à tela";
    public string FitToView { get => Resolve(nameof(FitToView), _valueForFitToView); set => _valueForFitToView = value; }
    /// <summary>Run automatic diagram layout.</summary>
    private string _valueForAutoLayout = "Auto-layout (organizar)";
    public string AutoLayout { get => Resolve(nameof(AutoLayout), _valueForAutoLayout); set => _valueForAutoLayout = value; }
    /// <summary>Increase canvas zoom.</summary>
    private string _valueForZoomIn = "Aumentar zoom";
    public string ZoomIn { get => Resolve(nameof(ZoomIn), _valueForZoomIn); set => _valueForZoomIn = value; }
    /// <summary>Decrease canvas zoom.</summary>
    private string _valueForZoomOut = "Diminuir zoom";
    public string ZoomOut { get => Resolve(nameof(ZoomOut), _valueForZoomOut); set => _valueForZoomOut = value; }

    // ── Date grouping (data grid) ────────────────────────────────────────
    // Nomes das unidades: compõem o chip de agrupamento ("Data (Ano › Mês › Dia)").
    /// <summary>Year interval name. Default "Ano".</summary>
    private string _valueForYear = "Ano";
    public string Year { get => Resolve(nameof(Year), _valueForYear); set => _valueForYear = value; }
    /// <summary>Quarter interval name. Default "Trimestre".</summary>
    private string _valueForQuarter = "Trimestre";
    public string Quarter { get => Resolve(nameof(Quarter), _valueForQuarter); set => _valueForQuarter = value; }
    /// <summary>Month interval name. Default "Mês".</summary>
    private string _valueForMonth = "Mês";
    public string Month { get => Resolve(nameof(Month), _valueForMonth); set => _valueForMonth = value; }
    /// <summary>Week interval name. Default "Semana".</summary>
    private string _valueForWeek = "Semana";
    public string Week { get => Resolve(nameof(Week), _valueForWeek); set => _valueForWeek = value; }
    /// <summary>Day interval name. Default "Dia".</summary>
    private string _valueForDay = "Dia";
    public string Day { get => Resolve(nameof(Day), _valueForDay); set => _valueForDay = value; }
    /// <summary>Hour interval name. Default "Hora".</summary>
    private string _valueForHour = "Hora";
    public string Hour { get => Resolve(nameof(Hour), _valueForHour); set => _valueForHour = value; }
    /// <summary>Pager unit when the grid is grouped (it pages groups, not rows). Default "grupos".</summary>
    private string _valueForGroups = "grupos";
    public string Groups { get => Resolve(nameof(Groups), _valueForGroups); set => _valueForGroups = value; }
    /// <summary>Aggregate total label.</summary>
    private string _valueForTotal = "Total";
    public string Total { get => Resolve(nameof(Total), _valueForTotal); set => _valueForTotal = value; }
    /// <summary>Grand-total label.</summary>
    private string _valueForGrandTotal = "Total geral";
    public string GrandTotal { get => Resolve(nameof(GrandTotal), _valueForGrandTotal); set => _valueForGrandTotal = value; }
    /// <summary>Quarter group label; <c>{0}</c> is the quarter number. Default "T{0}".</summary>
    private string _valueForQuarterAbbreviation = "T{0}";
    public string QuarterAbbreviation { get => Resolve(nameof(QuarterAbbreviation), _valueForQuarterAbbreviation); set => _valueForQuarterAbbreviation = value; }
    /// <summary>Week group label; <c>{0}</c> is the first day of the week. Default "Semana de {0}".</summary>
    private string _valueForWeekOf = "Semana de {0}";
    public string WeekOf { get => Resolve(nameof(WeekOf), _valueForWeekOf); set => _valueForWeekOf = value; }

    // ── Chat ─────────────────────────────────────────────────────────────
    /// <summary>Chat composer placeholder. Default "Digite uma mensagem...".</summary>
    private string _valueForMessagePlaceholder = "Digite uma mensagem...";
    public string MessagePlaceholder { get => Resolve(nameof(MessagePlaceholder), _valueForMessagePlaceholder); set => _valueForMessagePlaceholder = value; }
    /// <summary>Empty chat. Default "Nenhuma mensagem ainda. Comece a conversa!".</summary>
    private string _valueForNoMessages = "Nenhuma mensagem ainda. Comece a conversa!";
    public string NoMessages { get => Resolve(nameof(NoMessages), _valueForNoMessages); set => _valueForNoMessages = value; }

    // ── Remaining component UI and accessibility ─────────────────────────
    private string _valueForMessage = "Mensagem";
    public string Message { get => Resolve(nameof(Message), _valueForMessage); set => _valueForMessage = value; }
    private string _valueForMessages = "Mensagens";
    public string Messages { get => Resolve(nameof(Messages), _valueForMessages); set => _valueForMessages = value; }
    private string _valueForConversation = "Conversa";
    public string Conversation { get => Resolve(nameof(Conversation), _valueForConversation); set => _valueForConversation = value; }
    private string _valueForSomeone = "Alguém";
    public string Someone { get => Resolve(nameof(Someone), _valueForSomeone); set => _valueForSomeone = value; }
    private string _valueForTypingOne = "{0} está digitando…";
    public string TypingOne { get => Resolve(nameof(TypingOne), _valueForTypingOne); set => _valueForTypingOne = value; }
    private string _valueForTypingTwo = "{0} e {1} estão digitando…";
    public string TypingTwo { get => Resolve(nameof(TypingTwo), _valueForTypingTwo); set => _valueForTypingTwo = value; }
    private string _valueForTypingMany = "{0} e mais {1} estão digitando…";
    public string TypingMany { get => Resolve(nameof(TypingMany), _valueForTypingMany); set => _valueForTypingMany = value; }
    private string _valueForReasoning = "Raciocínio";
    public string Reasoning { get => Resolve(nameof(Reasoning), _valueForReasoning); set => _valueForReasoning = value; }
    private string _valueForSuggestions = "Sugestões";
    public string Suggestions { get => Resolve(nameof(Suggestions), _valueForSuggestions); set => _valueForSuggestions = value; }
    private string _valueForGoToSlide = "Ir para o slide {0}";
    public string GoToSlide { get => Resolve(nameof(GoToSlide), _valueForGoToSlide); set => _valueForGoToSlide = value; }
    private string _valueForSeries = "Série {0}";
    public string Series { get => Resolve(nameof(Series), _valueForSeries); set => _valueForSeries = value; }
    private string _valueForRequiredIndicator = "obrigatório";
    public string RequiredIndicator { get => Resolve(nameof(RequiredIndicator), _valueForRequiredIndicator); set => _valueForRequiredIndicator = value; }
    private string _valueForNoCommandsFor = "Nenhum comando para “{0}”.";
    public string NoCommandsFor { get => Resolve(nameof(NoCommandsFor), _valueForNoCommandsFor); set => _valueForNoCommandsFor = value; }
    private string _valueForCommandPalette = "Paleta de comandos";
    public string CommandPalette { get => Resolve(nameof(CommandPalette), _valueForCommandPalette); set => _valueForCommandPalette = value; }
    private string _valueForPageRange = "{0}–{1} de {2}";
    public string PageRange { get => Resolve(nameof(PageRange), _valueForPageRange); set => _valueForPageRange = value; }
    private string _valueForDetails = "Detalhes";
    public string Details { get => Resolve(nameof(Details), _valueForDetails); set => _valueForDetails = value; }
    private string _valueForAddCard = "Adicionar card";
    public string AddCard { get => Resolve(nameof(AddCard), _valueForAddCard); set => _valueForAddCard = value; }
    private string _valueForSearchCards = "Buscar cards";
    public string SearchCards { get => Resolve(nameof(SearchCards), _valueForSearchCards); set => _valueForSearchCards = value; }
    private string _valueForCardActions = "Ações do card";
    public string CardActions { get => Resolve(nameof(CardActions), _valueForCardActions); set => _valueForCardActions = value; }
    private string _valueForNothingHere = "Nada por aqui.";
    public string NothingHere { get => Resolve(nameof(NothingHere), _valueForNothingHere); set => _valueForNothingHere = value; }
    private string _valueForIncrease = "Aumentar";
    public string Increase { get => Resolve(nameof(Increase), _valueForIncrease); set => _valueForIncrease = value; }
    private string _valueForDecrease = "Diminuir";
    public string Decrease { get => Resolve(nameof(Decrease), _valueForDecrease); set => _valueForDecrease = value; }
    private string _valueForIncreaseQuantity = "Aumentar quantidade";
    public string IncreaseQuantity { get => Resolve(nameof(IncreaseQuantity), _valueForIncreaseQuantity); set => _valueForIncreaseQuantity = value; }
    private string _valueForDecreaseQuantity = "Diminuir quantidade";
    public string DecreaseQuantity { get => Resolve(nameof(DecreaseQuantity), _valueForDecreaseQuantity); set => _valueForDecreaseQuantity = value; }
    private string _valueForRemoveNamedItem = "Remover {0}";
    public string RemoveNamedItem { get => Resolve(nameof(RemoveNamedItem), _valueForRemoveNamedItem); set => _valueForRemoveNamedItem = value; }
    private string _valueForHue = "Matiz";
    public string Hue { get => Resolve(nameof(Hue), _valueForHue); set => _valueForHue = value; }
    private string _valueForHexValue = "Valor hexadecimal";
    public string HexValue { get => Resolve(nameof(HexValue), _valueForHexValue); set => _valueForHexValue = value; }
    private string _valueForSteps = "Etapas";
    public string Steps { get => Resolve(nameof(Steps), _valueForSteps); set => _valueForSteps = value; }
    private string _valueForStep = "Etapa";
    public string Step { get => Resolve(nameof(Step), _valueForStep); set => _valueForStep = value; }
    private string _valueForGoToStep = "Ir para etapa {0}: {1}";
    public string GoToStep { get => Resolve(nameof(GoToStep), _valueForGoToStep); set => _valueForGoToStep = value; }
    private string _valueForSkipTour = "Pular tour";
    public string SkipTour { get => Resolve(nameof(SkipTour), _valueForSkipTour); set => _valueForSkipTour = value; }
    private string _valueForChart = "Gráfico";
    public string Chart { get => Resolve(nameof(Chart), _valueForChart); set => _valueForChart = value; }
    private string _valueForSparkline = "Minigráfico";
    public string Sparkline { get => Resolve(nameof(Sparkline), _valueForSparkline); set => _valueForSparkline = value; }
    private string _valueForSpeechConnecting = "Conectando…";
    public string SpeechConnecting { get => Resolve(nameof(SpeechConnecting), _valueForSpeechConnecting); set => _valueForSpeechConnecting = value; }
    private string _valueForSpeechStopping = "Parando…";
    public string SpeechStopping { get => Resolve(nameof(SpeechStopping), _valueForSpeechStopping); set => _valueForSpeechStopping = value; }
    private string _valueForSpeechStart = "Pressione para falar";
    public string SpeechStart { get => Resolve(nameof(SpeechStart), _valueForSpeechStart); set => _valueForSpeechStart = value; }
    private string _valueForSpeechStop = "Pressione para parar";
    public string SpeechStop { get => Resolve(nameof(SpeechStop), _valueForSpeechStop); set => _valueForSpeechStop = value; }
    private string _valueForPleaseWait = "Aguarde…";
    public string PleaseWait { get => Resolve(nameof(PleaseWait), _valueForPleaseWait); set => _valueForPleaseWait = value; }
    private string _valueForSpeechUnsupported = "Reconhecimento de voz não disponível neste navegador";
    public string SpeechUnsupported { get => Resolve(nameof(SpeechUnsupported), _valueForSpeechUnsupported); set => _valueForSpeechUnsupported = value; }
    private string _valueForErrorWithMessage = "Erro: {0}";
    public string ErrorWithMessage { get => Resolve(nameof(ErrorWithMessage), _valueForErrorWithMessage); set => _valueForErrorWithMessage = value; }
    private string _valueForMicrophonePermissionDenied = "permissão de microfone negada";
    public string MicrophonePermissionDenied { get => Resolve(nameof(MicrophonePermissionDenied), _valueForMicrophonePermissionDenied); set => _valueForMicrophonePermissionDenied = value; }
    private string _valueForNoSpeechDetected = "nenhuma fala detectada";
    public string NoSpeechDetected { get => Resolve(nameof(NoSpeechDetected), _valueForNoSpeechDetected); set => _valueForNoSpeechDetected = value; }
    private string _valueForMicrophoneUnavailable = "microfone indisponível";
    public string MicrophoneUnavailable { get => Resolve(nameof(MicrophoneUnavailable), _valueForMicrophoneUnavailable); set => _valueForMicrophoneUnavailable = value; }
    private string _valueForNetworkError = "erro de rede";
    public string NetworkError { get => Resolve(nameof(NetworkError), _valueForNetworkError); set => _valueForNetworkError = value; }
    private string _valueForAborted = "interrompido";
    public string Aborted { get => Resolve(nameof(Aborted), _valueForAborted); set => _valueForAborted = value; }
    private string _valueForLanguageUnsupported = "idioma não suportado";
    public string LanguageUnsupported { get => Resolve(nameof(LanguageUnsupported), _valueForLanguageUnsupported); set => _valueForLanguageUnsupported = value; }
    private string _valueForServiceBlocked = "serviço bloqueado";
    public string ServiceBlocked { get => Resolve(nameof(ServiceBlocked), _valueForServiceBlocked); set => _valueForServiceBlocked = value; }
    private string _valueForBrowserTimeout = "navegador não respondeu";
    public string BrowserTimeout { get => Resolve(nameof(BrowserTimeout), _valueForBrowserTimeout); set => _valueForBrowserTimeout = value; }
    private string _valueForSpeechConnectionTimeout = "não conectou ao serviço de fala — verifique sua rede";
    public string SpeechConnectionTimeout { get => Resolve(nameof(SpeechConnectionTimeout), _valueForSpeechConnectionTimeout); set => _valueForSpeechConnectionTimeout = value; }
    private string _valueForInvalidState = "estado inválido — tente novamente";
    public string InvalidState { get => Resolve(nameof(InvalidState), _valueForInvalidState); set => _valueForInvalidState = value; }
    private string _valueForRecognizerCreationFailed = "falha ao criar reconhecedor";
    public string RecognizerCreationFailed { get => Resolve(nameof(RecognizerCreationFailed), _valueForRecognizerCreationFailed); set => _valueForRecognizerCreationFailed = value; }
    private string _valueForSuperseded = "substituído por outro botão";
    public string Superseded { get => Resolve(nameof(Superseded), _valueForSuperseded); set => _valueForSuperseded = value; }
    private string _valueForUndoAction = "Desfazer";
    public string UndoAction { get => Resolve(nameof(UndoAction), _valueForUndoAction); set => _valueForUndoAction = value; }
    private string _valueForRedoAction = "Refazer";
    public string RedoAction { get => Resolve(nameof(RedoAction), _valueForRedoAction); set => _valueForRedoAction = value; }
    private string _valueForBold = "Negrito";
    public string Bold { get => Resolve(nameof(Bold), _valueForBold); set => _valueForBold = value; }
    private string _valueForItalic = "Itálico";
    public string Italic { get => Resolve(nameof(Italic), _valueForItalic); set => _valueForItalic = value; }
    private string _valueForUnderline = "Sublinhado";
    public string Underline { get => Resolve(nameof(Underline), _valueForUnderline); set => _valueForUnderline = value; }
    private string _valueForStrikethrough = "Tachado";
    public string Strikethrough { get => Resolve(nameof(Strikethrough), _valueForStrikethrough); set => _valueForStrikethrough = value; }
    private string _valueForBlockStyle = "Estilo do bloco";
    public string BlockStyle { get => Resolve(nameof(BlockStyle), _valueForBlockStyle); set => _valueForBlockStyle = value; }
    private string _valueForParagraph = "Parágrafo";
    public string Paragraph { get => Resolve(nameof(Paragraph), _valueForParagraph); set => _valueForParagraph = value; }
    private string _valueForHeading = "Título {0}";
    public string Heading { get => Resolve(nameof(Heading), _valueForHeading); set => _valueForHeading = value; }
    private string _valueForQuote = "Citação";
    public string Quote { get => Resolve(nameof(Quote), _valueForQuote); set => _valueForQuote = value; }
    private string _valueForCode = "Código";
    public string Code { get => Resolve(nameof(Code), _valueForCode); set => _valueForCode = value; }
    private string _valueForAlignLeft = "Alinhar à esquerda";
    public string AlignLeft { get => Resolve(nameof(AlignLeft), _valueForAlignLeft); set => _valueForAlignLeft = value; }
    private string _valueForAlignCenter = "Centralizar";
    public string AlignCenter { get => Resolve(nameof(AlignCenter), _valueForAlignCenter); set => _valueForAlignCenter = value; }
    private string _valueForAlignRight = "Alinhar à direita";
    public string AlignRight { get => Resolve(nameof(AlignRight), _valueForAlignRight); set => _valueForAlignRight = value; }
    private string _valueForBulletedList = "Lista";
    public string BulletedList { get => Resolve(nameof(BulletedList), _valueForBulletedList); set => _valueForBulletedList = value; }
    private string _valueForNumberedList = "Lista numerada";
    public string NumberedList { get => Resolve(nameof(NumberedList), _valueForNumberedList); set => _valueForNumberedList = value; }
    private string _valueForTextColor = "Cor do texto";
    public string TextColor { get => Resolve(nameof(TextColor), _valueForTextColor); set => _valueForTextColor = value; }
    private string _valueForInsertLink = "Inserir link";
    public string InsertLink { get => Resolve(nameof(InsertLink), _valueForInsertLink); set => _valueForInsertLink = value; }
    private string _valueForRemoveFormatting = "Limpar formatação";
    public string RemoveFormatting { get => Resolve(nameof(RemoveFormatting), _valueForRemoveFormatting); set => _valueForRemoveFormatting = value; }
    private string _valueForViewSource = "Ver código";
    public string ViewSource { get => Resolve(nameof(ViewSource), _valueForViewSource); set => _valueForViewSource = value; }
    private string _valueForLinkUrlPrompt = "URL do link:";
    public string LinkUrlPrompt { get => Resolve(nameof(LinkUrlPrompt), _valueForLinkUrlPrompt); set => _valueForLinkUrlPrompt = value; }
    private string _valueForWorkflowActions = "Ações do fluxo";
    public string WorkflowActions { get => Resolve(nameof(WorkflowActions), _valueForWorkflowActions); set => _valueForWorkflowActions = value; }
    private string _valueForWorkflowPalette = "Paleta do fluxo";
    public string WorkflowPalette { get => Resolve(nameof(WorkflowPalette), _valueForWorkflowPalette); set => _valueForWorkflowPalette = value; }
    private string _valueForProperties = "Propriedades";
    public string Properties { get => Resolve(nameof(Properties), _valueForProperties); set => _valueForProperties = value; }
    private string _valueForSelectNode = "Selecione um nó";
    public string SelectNode { get => Resolve(nameof(SelectNode), _valueForSelectNode); set => _valueForSelectNode = value; }
    private string _valueForReviewWorkflow = "Revise o fluxo";
    public string ReviewWorkflow { get => Resolve(nameof(ReviewWorkflow), _valueForReviewWorkflow); set => _valueForReviewWorkflow = value; }
    private string _valueForMenu = "Menu";
    public string Menu { get => Resolve(nameof(Menu), _valueForMenu); set => _valueForMenu = value; }
    private string _valueForCollapsePreviousPane = "Colapsar painel anterior";
    public string CollapsePreviousPane { get => Resolve(nameof(CollapsePreviousPane), _valueForCollapsePreviousPane); set => _valueForCollapsePreviousPane = value; }
    private string _valueForExpandPreviousPane = "Expandir painel anterior";
    public string ExpandPreviousPane { get => Resolve(nameof(ExpandPreviousPane), _valueForExpandPreviousPane); set => _valueForExpandPreviousPane = value; }
    private string _valueForSqlExpected = "esperado {0}";
    public string SqlExpected { get => Resolve(nameof(SqlExpected), _valueForSqlExpected); set => _valueForSqlExpected = value; }
    private string _valueForSqlUnexpectedToken = "token inesperado “{0}”";
    public string SqlUnexpectedToken { get => Resolve(nameof(SqlUnexpectedToken), _valueForSqlUnexpectedToken); set => _valueForSqlUnexpectedToken = value; }
    private string _valueForSqlPrefixNotUnsupported = "NOT só é suportado na forma infixa (campo NOT LIKE '%x%', <>, IS NOT NULL)";
    public string SqlPrefixNotUnsupported { get => Resolve(nameof(SqlPrefixNotUnsupported), _valueForSqlPrefixNotUnsupported); set => _valueForSqlPrefixNotUnsupported = value; }
    private string _valueForSqlExpectedField = "esperado um campo, encontrado “{0}”";
    public string SqlExpectedField { get => Resolve(nameof(SqlExpectedField), _valueForSqlExpectedField); set => _valueForSqlExpectedField = value; }
    private string _valueForSqlUnknownColumn = "coluna “{0}” desconhecida";
    public string SqlUnknownColumn { get => Resolve(nameof(SqlUnknownColumn), _valueForSqlUnknownColumn); set => _valueForSqlUnknownColumn = value; }
    private string _valueForSqlExpectedLikeOrBetween = "esperado LIKE ou BETWEEN após NOT";
    public string SqlExpectedLikeOrBetween { get => Resolve(nameof(SqlExpectedLikeOrBetween), _valueForSqlExpectedLikeOrBetween); set => _valueForSqlExpectedLikeOrBetween = value; }
    private string _valueForSqlExpectedOperator = "esperado um operador após “{0}”";
    public string SqlExpectedOperator { get => Resolve(nameof(SqlExpectedOperator), _valueForSqlExpectedOperator); set => _valueForSqlExpectedOperator = value; }
    private string _valueForSqlExpectedValue = "esperado um valor, encontrado “{0}”";
    public string SqlExpectedValue { get => Resolve(nameof(SqlExpectedValue), _valueForSqlExpectedValue); set => _valueForSqlExpectedValue = value; }
    private string _valueForSqlExpectedNumber = "valor numérico esperado para “{0}”";
    public string SqlExpectedNumber { get => Resolve(nameof(SqlExpectedNumber), _valueForSqlExpectedNumber); set => _valueForSqlExpectedNumber = value; }
    private string _valueForSqlInvalidDate = "data inválida para “{0}” (use 'AAAA-MM-DD')";
    public string SqlInvalidDate { get => Resolve(nameof(SqlInvalidDate), _valueForSqlInvalidDate); set => _valueForSqlInvalidDate = value; }
    private string _valueForSqlExpectedBoolean = "valor booleano esperado para “{0}” (TRUE/FALSE)";
    public string SqlExpectedBoolean { get => Resolve(nameof(SqlExpectedBoolean), _valueForSqlExpectedBoolean); set => _valueForSqlExpectedBoolean = value; }
    private string _valueForSqlNotLikePatternUnsupported = "NOT LIKE só suporta '%valor%' ou um valor exato";
    public string SqlNotLikePatternUnsupported { get => Resolve(nameof(SqlNotLikePatternUnsupported), _valueForSqlNotLikePatternUnsupported); set => _valueForSqlNotLikePatternUnsupported = value; }
    private string _valueForSqlUnexpectedCharacter = "caractere inesperado “{0}”";
    public string SqlUnexpectedCharacter { get => Resolve(nameof(SqlUnexpectedCharacter), _valueForSqlUnexpectedCharacter); set => _valueForSqlUnexpectedCharacter = value; }
    private string _valueForSqlUnterminatedString = "string não terminada (faltou aspas de fechamento)";
    public string SqlUnterminatedString { get => Resolve(nameof(SqlUnterminatedString), _valueForSqlUnterminatedString); set => _valueForSqlUnterminatedString = value; }
    private string _valueForSqlUnterminatedIdentifier = "identificador não terminado";
    public string SqlUnterminatedIdentifier { get => Resolve(nameof(SqlUnterminatedIdentifier), _valueForSqlUnterminatedIdentifier); set => _valueForSqlUnterminatedIdentifier = value; }
    private string _valueForSqlInvalidNumber = "número inválido “{0}”";
    public string SqlInvalidNumber { get => Resolve(nameof(SqlInvalidNumber), _valueForSqlInvalidNumber); set => _valueForSqlInvalidNumber = value; }
    private string _valueForSqlExpectedNumberLiteral = "esperado um número";
    public string SqlExpectedNumberLiteral { get => Resolve(nameof(SqlExpectedNumberLiteral), _valueForSqlExpectedNumberLiteral); set => _valueForSqlExpectedNumberLiteral = value; }
    private string _valueForSqlExpectedClosingParenthesis = "esperado “)”";
    public string SqlExpectedClosingParenthesis { get => Resolve(nameof(SqlExpectedClosingParenthesis), _valueForSqlExpectedClosingParenthesis); set => _valueForSqlExpectedClosingParenthesis = value; }
    private string _valueForSqlExpectedNull = "esperado NULL";
    public string SqlExpectedNull { get => Resolve(nameof(SqlExpectedNull), _valueForSqlExpectedNull); set => _valueForSqlExpectedNull = value; }
    private string _valueForSqlExpectedQuotedPattern = "esperado um padrão entre aspas";
    public string SqlExpectedQuotedPattern { get => Resolve(nameof(SqlExpectedQuotedPattern), _valueForSqlExpectedQuotedPattern); set => _valueForSqlExpectedQuotedPattern = value; }
    private string _valueForSqlExpectedAndInRange = "esperado AND no intervalo";
    public string SqlExpectedAndInRange { get => Resolve(nameof(SqlExpectedAndInRange), _valueForSqlExpectedAndInRange); set => _valueForSqlExpectedAndInRange = value; }

    /// <summary>
    /// An English set. Use as-is, or as a starting point:
    /// <c>o.Texts = OmniTexts.English()</c>.
    /// </summary>
    public static OmniTexts English() => new(CultureInfo.GetCultureInfo("en"))
    {
        Close = "Close",
        Clear = "Clear",
        ClearAll = "Clear all",
        Cancel = "Cancel",
        Confirm = "Confirm",
        Apply = "Apply",
        Add = "Add",
        Edit = "Edit",
        Export = "Export",
        ResizeColumn = "Resize column",
        DragColumnToGroup = "Drag to group",
        Estimate = "Estimate",
        Subtasks = "Subtasks",
        DueDate = "Due date",
        Minute = "Minute",
        Second = "Second",
        AccentColorNamed = "Accent color {0}",
        Warning = "Warning",
        Understood = "Got it",
        Shortcuts = "Shortcuts",
        Navigate = "navigate",
        Select = "select",
        NoCards = "No cards",
        AddCondition = "Add condition",
        AddGroup = "Add group",
        ApplyToFilter = "Apply to filter",
        DataFilterMinimum = "Minimum",
        DataFilterMaximum = "Maximum",
        DataFilterStartDate = "Start date",
        DataFilterEndDate = "End date",
        DataFilterStartValue = "Start value",
        DataFilterEndValue = "End value",
        And = "AND",
        Or = "OR",
        FilterValue = "value",
        VisualMode = "Visual",
        SqlValid = "Valid SQL",
        GeneratedSql = "Generated SQL",
        Fields = "Fields",
        Field = "Field",
        SqlFilterPlaceholder = "e.g. Name LIKE '%text%' AND Age >= 18",
        SqlFilter = "SQL filter",
        NoFilterConditions = "No conditions yet — add one below to get started.",
        EmptyGroup = "Empty group",
        FilterContains = "Contains",
        FilterNotContains = "Does not contain",
        FilterStartsWith = "Starts with",
        FilterEndsWith = "Ends with",
        FilterEquals = "Equals",
        FilterNotEquals = "Does not equal",
        FilterGreaterThan = "Greater than",
        FilterGreaterThanOrEqual = "Greater than or equal to",
        FilterLessThan = "Less than",
        FilterLessThanOrEqual = "Less than or equal to",
        FilterBetween = "Between",
        FilterNotBetween = "Outside range",
        FilterIsEmpty = "Is empty",
        FilterIsNotEmpty = "Is not empty",
        Remove = "Remove",
        Send = "Send",
        Copy = "Copy",
        Actions = "Actions",
        SelectAllRows = "Select all visible rows",
        SelectRow = "Select row",
        SelectOption = "Select an option",
        Save = "Save",
        DataGridFormOperationFailed = "The operation could not be completed.",
        DataGridFormValidationFailed = "Review the submitted data.",
        DataGridFormConflict = "This record was changed by another operation. Refresh the data and try again.",
        DataGridFormNotFound = "This record no longer exists.",
        DataGridFormForbidden = "You do not have permission to complete this operation.",
        DataGridFormRefreshFailed = "The change was saved, but the grid could not be refreshed.",
        DataGridFormUnsavedChangesTitle = "Discard changes?",
        DataGridFormUnsavedChangesMessage = "There are unsaved changes. Do you want to discard them?",
        DataGridFormDiscardChanges = "Discard changes",
        DataGridFormContinueEditing = "Continue editing",
        DataGridFormBulkActions = "Bulk actions",
        DataGridFormMoreActions = "More actions",
        DataGridFormSelectedCount = "{0} selected",
        DataGridFormBulkConfirmation = "Apply this action to the selected records?",
        EntityPickerTitle = "Select record",
        EntityPickerPlaceholder = "Select a record",
        EntityPickerItem = "Record",
        DataFormWizardNavigation = "Form steps",
        DataImportUpload = "Select a CSV or TSV file",
        DataImportUploadHint = "Up to {0} and {1:N0} rows",
        DataImportProcessing = "Processing file...",
        DataImportMapping = "Column mapping",
        DataImportMappingHint = "Map each target field to a source-file column.",
        DataImportIgnoreColumn = "Do not import",
        DataImportPreview = "Validated preview",
        DataImportRow = "Row",
        DataImportStatus = "Status",
        DataImportErrors = "Errors",
        DataImportValid = "Valid",
        DataImportInvalid = "Invalid",
        DataImportSummary = "{0}, {1}, {2}",
        DataImportValidCount = "{0:N0} valid rows",
        DataImportInvalidCount = "{0:N0} invalid rows",
        DataImportTotalCount = "{0:N0} total rows",
        DataImportPreviewLimit = "Showing the first {0:N0} of {1:N0} rows.",
        DataImportImport = "Import data",
        DataImportReady = "{0:N0} rows ready to import.",
        DataImportResolveErrors = "Fix or remove invalid rows before importing.",
        DataImportColumn = "Column {0}",
        DataImportFileTooLarge = "The file exceeds the {0} limit.",
        DataImportUnexpectedCharacter = "Unexpected character '{0}' after a quoted field.",
        DataImportUnclosedQuote = "The file contains an unterminated quoted field.",
        DataImportEmptyFile = "The file is empty.",
        DataImportNoDataRows = "The file contains no data rows.",
        DataImportTooManyRows = "The file exceeds the configured row limit.",
        DataImportTooManyColumns = "The file exceeds the configured column limit.",
        DataImportCellTooLong = "A cell exceeds the configured character limit.",
        DataImportRequiredValue = "{0} is required.",
        DataImportInvalidValue = "{0} has an invalid value.",
        DataGridFormDeleteConfirmation = "Do you want to remove this record?",
        Required = "Required field.",
        InvalidValue = "Invalid value.",
        DataFormValidationSummary = "Fix the errors below:",
        MoveUp = "Move up",
        MoveDown = "Move down",
        DataFormMinimumItems = "Add at least {0} items.",
        DataFormMaximumItems = "Keep at most {0} items.",
        Yes = "Yes",
        No = "No",
        NotProvided = "Not provided",
        ShowPassword = "Show password",
        HidePassword = "Hide password",
        Next = "Next",
        Previous = "Previous",
        Back = "Back",
        Complete = "Finish",
        Today = "Today",
        MultiDay = "Multi-day",
        Timeline = "Timeline",
        Planner = "Planner",
        MoreAppointments = "+ {0} more",
        NextMonth = "Next month",
        PreviousMonth = "Previous month",
        NextSlide = "Next slide",
        PreviousSlide = "Previous slide",
        SkipToContent = "Skip to content",
        ScrollToTop = "Back to top",
        CloseNavigation = "Close navigation",
        OpenMenu = "Open menu",
        CloseMenu = "Close menu",
        OpenNavigation = "Open navigation",
        MoreOptions = "More options",
        CollapseNextPane = "Collapse next pane",
        ExpandNextPane = "Expand next pane",
        SearchPlaceholder = "Search...",
        CommandPlaceholder = "Search command...",
        Recent = "Recent",
        GlobalSearch = "Global search",
        GlobalSearchPlaceholder = "Search the entire system...",
        TypeToSearch = "Type to search",
        SearchFailed = "The search could not be completed.",
        ClearSearch = "Clear search",
        ExitWithoutSavingTitle = "Leave without saving?",
        ExitWithoutSavingMessage = "You have unsaved changes. Do you want to leave anyway?",
        ExitWithoutSaving = "Leave without saving",
        ContinueEditing = "Continue editing",
        AppearanceMode = "Appearance mode",
        Light = "Light",
        Dark = "Dark",
        System = "System",
        Theme = "Theme",
        Language = "Language",
        DefaultDensity = "Default",
        SpaciousDensity = "Spacious",
        AccentColor = "Accent color",
        Mode = "Mode",
        Density = "Density",
        CompactDensity = "Compact",
        FilterPlaceholder = "Filter…",
        Searching = "Searching…",
        NoResults = "No results.",
        NoRecords = "No records found.",
        FileManager = "File manager",
        Location = "Location",
        NewFolder = "New folder",
        FolderName = "Folder name",
        Rename = "Rename",
        NewName = "New name",
        Delete = "Delete",
        DeleteNamedItem = "Delete “{0}”?",
        Upload = "Upload",
        Download = "Download",
        Refresh = "Refresh",
        ListView = "List view",
        GridView = "Grid view",
        SearchFolder = "Search this folder",
        LoadingFiles = "Loading files...",
        EmptyFolder = "This folder is empty.",
        FileOperationFailed = "The operation could not be completed.",
        ItemsCount = "{0} of {1} items",
        ItemLimit = "Limit of {0} items",
        Loading = "Loading...",
        LoadingStatus = "Loading",
        LoadOptionsError = "The options could not be loaded.",
        Retry = "Try again",
        LoadMore = "Load more",
        NoOptions = "No options",
        SelectedOption = "Selected option",
        SelectDateRange = "Select date range",
        SelectRangeEnd = "Start: {0} · select the end",
        OpenCalendar = "Open calendar",
        Transparency = "Transparency",
        Digit = "Digit",
        SignatureArea = "Signature area",
        Undo = "Undo",
        AwaitingSignature = "Awaiting signature",
        SignatureCaptured = "Signature captured",
        ChooseFiles = "Drag files here or click to select",
        UpToSize = "up to {0}",
        PasswordStrength = "Password strength",
        PasswordUppercase = "One uppercase letter",
        PasswordLowercase = "One lowercase letter",
        PasswordDigit = "One number",
        PasswordSymbol = "One symbol",
        PasswordWeak = "Weak",
        PasswordFair = "Fair",
        PasswordGood = "Good",
        PasswordStrong = "Strong",
        EmptyList = "Empty list",
        Source = "Source",
        Destination = "Destination",
        MoveSelectedToDestination = "Move selected items to destination",
        MoveSelectedToSource = "Move selected items to source",
        MoveAllToDestination = "Move all items to destination",
        MoveAllToSource = "Move all items to source",
        Rating = "Rating {0} of {1}",
        Value = "Value",
        LastSevenDays = "Last 7 days",
        LastThirtyDays = "Last 30 days",
        ThisMonth = "This month",
        LastMonth = "Last month",
        Yesterday = "Yesterday",
        ThisYear = "This year",
        SelectRangeStart = "Select the start date",
        DateRangeSummary = "{0} → {1} · {2} days",
        RemoveGrouping = "Remove grouping",
        VisibleColumns = "Visible columns",
        HierarchicalTable = "Hierarchical table",
        Expand = "Expand",
        Collapse = "Collapse",
        HierarchyLoadError = "The items could not be loaded.",
        HierarchyLimitReached = "Showing at most {0} rows.",
        GroupPanel = "Drag a column header here to group",
        GroupLimitReached = "Showing at most {0} groups.",
        FitToView = "Fit to view",
        AutoLayout = "Auto layout",
        ZoomIn = "Zoom in",
        ZoomOut = "Zoom out",
        Year = "Year",
        Quarter = "Quarter",
        Month = "Month",
        Week = "Week",
        Day = "Day",
        Hour = "Hour",
        Groups = "groups",
        Total = "Total",
        GrandTotal = "Grand total",
        QuarterAbbreviation = "Q{0}",
        WeekOf = "Week of {0}",
        MessagePlaceholder = "Type a message...",
        NoMessages = "No messages yet. Start the conversation!",
        Message = "Message",
        Messages = "Messages",
        Conversation = "Conversation",
        Someone = "Someone",
        TypingOne = "{0} is typing…",
        TypingTwo = "{0} and {1} are typing…",
        TypingMany = "{0} and {1} more are typing…",
        Reasoning = "Reasoning",
        Suggestions = "Suggestions",
        GoToSlide = "Go to slide {0}",
        Series = "Series {0}",
        RequiredIndicator = "required",
        NoCommandsFor = "No commands for “{0}”.",
        CommandPalette = "Command palette",
        PageRange = "{0}–{1} of {2}",
        Details = "Details",
        AddCard = "Add card",
        SearchCards = "Search cards",
        CardActions = "Card actions",
        NothingHere = "Nothing here.",
        Increase = "Increase",
        Decrease = "Decrease",
        IncreaseQuantity = "Increase quantity",
        DecreaseQuantity = "Decrease quantity",
        RemoveNamedItem = "Remove {0}",
        Hue = "Hue",
        HexValue = "Hexadecimal value",
        Steps = "Steps",
        Step = "Step",
        GoToStep = "Go to step {0}: {1}",
        SkipTour = "Skip tour",
        Chart = "Chart",
        Sparkline = "Sparkline",
        SpeechConnecting = "Connecting…",
        SpeechStopping = "Stopping…",
        SpeechStart = "Press to speak",
        SpeechStop = "Press to stop",
        PleaseWait = "Please wait…",
        SpeechUnsupported = "Speech recognition is not available in this browser",
        ErrorWithMessage = "Error: {0}",
        MicrophonePermissionDenied = "microphone permission denied",
        NoSpeechDetected = "no speech detected",
        MicrophoneUnavailable = "microphone unavailable",
        NetworkError = "network error",
        Aborted = "aborted",
        LanguageUnsupported = "language not supported",
        ServiceBlocked = "service blocked",
        BrowserTimeout = "browser did not respond",
        SpeechConnectionTimeout = "could not connect to the speech service — check your network",
        InvalidState = "invalid state — try again",
        RecognizerCreationFailed = "failed to create speech recognizer",
        Superseded = "superseded by another button",
        UndoAction = "Undo",
        RedoAction = "Redo",
        Bold = "Bold",
        Italic = "Italic",
        Underline = "Underline",
        Strikethrough = "Strikethrough",
        BlockStyle = "Block style",
        Paragraph = "Paragraph",
        Heading = "Heading {0}",
        Quote = "Quote",
        Code = "Code",
        AlignLeft = "Align left",
        AlignCenter = "Align center",
        AlignRight = "Align right",
        BulletedList = "Bulleted list",
        NumberedList = "Numbered list",
        TextColor = "Text color",
        InsertLink = "Insert link",
        RemoveFormatting = "Clear formatting",
        ViewSource = "View source",
        LinkUrlPrompt = "Link URL:",
        WorkflowActions = "Workflow actions",
        WorkflowPalette = "Workflow palette",
        Properties = "Properties",
        SelectNode = "Select a node",
        ReviewWorkflow = "Review the workflow",
        Menu = "Menu",
        CollapsePreviousPane = "Collapse previous pane",
        ExpandPreviousPane = "Expand previous pane",
        SqlExpected = "expected {0}",
        SqlUnexpectedToken = "unexpected token “{0}”",
        SqlPrefixNotUnsupported = "NOT is only supported in infix form (field NOT LIKE '%x%', <>, IS NOT NULL)",
        SqlExpectedField = "expected a field, found “{0}”",
        SqlUnknownColumn = "unknown column “{0}”",
        SqlExpectedLikeOrBetween = "expected LIKE or BETWEEN after NOT",
        SqlExpectedOperator = "expected an operator after “{0}”",
        SqlExpectedValue = "expected a value, found “{0}”",
        SqlExpectedNumber = "expected a numeric value for “{0}”",
        SqlInvalidDate = "invalid date for “{0}” (use 'YYYY-MM-DD')",
        SqlExpectedBoolean = "expected a Boolean value for “{0}” (TRUE/FALSE)",
        SqlNotLikePatternUnsupported = "NOT LIKE only supports '%value%' or an exact value",
        SqlUnexpectedCharacter = "unexpected character “{0}”",
        SqlUnterminatedString = "unterminated string (missing closing quote)",
        SqlUnterminatedIdentifier = "unterminated identifier",
        SqlInvalidNumber = "invalid number “{0}”",
        SqlExpectedNumberLiteral = "expected a number",
        SqlExpectedClosingParenthesis = "expected “)”",
        SqlExpectedNull = "expected NULL",
        SqlExpectedQuotedPattern = "expected a quoted pattern",
        SqlExpectedAndInRange = "expected AND in the range",
        KeyboardSpace = "Space",
        KeyboardBackspace = "Backspace",
        KeyboardSymbols = "Symbols",
        KeyboardLabel = "Virtual keyboard",
    };
}
