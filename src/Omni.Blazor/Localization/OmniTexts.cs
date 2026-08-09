namespace Omni.Blazor.Localization;

/// <summary>
/// Every user-facing string the library renders on its own (button labels, ARIA
/// names, empty states). Register one instance to translate the whole library at
/// once instead of passing a parameter per component instance:
///
/// <code>
/// builder.Services.AddOmniComponents(o => o.Texts = OmniTexts.English());
/// </code>
///
/// <para>
/// Resolution order per string: the component's own <c>[Parameter]</c> (if the
/// consumer set one) → this instance → the built-in default. Defaults are pt-BR,
/// matching what the library has always rendered, so registering nothing changes
/// nothing.
/// </para>
/// <para>
/// For per-request culture switching, register it scoped and populate it from your
/// own <c>IStringLocalizer</c>/resx — the library deliberately takes no localization
/// dependency of its own.
/// </para>
/// </summary>
public class OmniTexts
{
    /// <summary>Built-in pt-BR defaults, used when nothing is registered.</summary>
    public static OmniTexts Default { get; } = new();

    // ── Generic actions ──────────────────────────────────────────────────
    /// <summary>Close (dialogs, alerts, banners). Default "Fechar".</summary>
    public string Close { get; set; } = "Fechar";
    /// <summary>Clear an input or a filter. Default "Limpar".</summary>
    public string Clear { get; set; } = "Limpar";
    /// <summary>Clear everything. Default "Limpar tudo".</summary>
    public string ClearAll { get; set; } = "Limpar tudo";
    /// <summary>Cancel. Default "Cancelar".</summary>
    public string Cancel { get; set; } = "Cancelar";
    /// <summary>Confirm. Default "Confirmar".</summary>
    public string Confirm { get; set; } = "Confirmar";
    /// <summary>Apply. Default "Aplicar".</summary>
    public string Apply { get; set; } = "Aplicar";
    /// <summary>Add. Default "Adicionar".</summary>
    public string Add { get; set; } = "Adicionar";
    /// <summary>Edit. Default "Editar".</summary>
    public string Edit { get; set; } = "Editar";
    // Shift and Enter are not here on purpose: they read the same in pt-BR and English,
    // and a locale that does differ (German "Umschalt") is served by the per-key
    // VirtualKeyboardKey.AriaLabel, which belongs to the layout rather than to a global set.
    /// <summary>Space bar of the virtual keyboard. Default "Espaço".</summary>
    public string KeyboardSpace { get; set; } = "Espaço";
    /// <summary>Accessible name of the virtual keyboard's Backspace key. Default "Apagar".</summary>
    public string KeyboardBackspace { get; set; } = "Apagar";
    /// <summary>Accessible name of the virtual keyboard's symbol-set key. Default "Símbolos".</summary>
    public string KeyboardSymbols { get; set; } = "Símbolos";
    /// <summary>Accessible name of the virtual keyboard itself. Default "Teclado virtual".</summary>
    public string KeyboardLabel { get; set; } = "Teclado virtual";
    /// <summary>Add a filter condition (data filter). Default "Adicionar condição".</summary>
    public string AddCondition { get; set; } = "Adicionar condição";
    /// <summary>Add a filter group (data filter). Default "Adicionar grupo".</summary>
    public string AddGroup { get; set; } = "Adicionar grupo";
    /// <summary>Apply the typed SQL back to the filter (data filter). Default "Aplicar ao filtro".</summary>
    public string ApplyToFilter { get; set; } = "Aplicar ao filtro";
    /// <summary>Lower numeric range value. Default "Mínimo".</summary>
    public string DataFilterMinimum { get; set; } = "Mínimo";
    /// <summary>Upper numeric range value. Default "Máximo".</summary>
    public string DataFilterMaximum { get; set; } = "Máximo";
    /// <summary>Range start date. Default "Data inicial".</summary>
    public string DataFilterStartDate { get; set; } = "Data inicial";
    /// <summary>Range end date. Default "Data final".</summary>
    public string DataFilterEndDate { get; set; } = "Data final";
    /// <summary>Generic range start value. Default "Valor inicial".</summary>
    public string DataFilterStartValue { get; set; } = "Valor inicial";
    /// <summary>Generic range end value. Default "Valor final".</summary>
    public string DataFilterEndValue { get; set; } = "Valor final";
    /// <summary>Remove. Default "Remover".</summary>
    public string Remove { get; set; } = "Remover";
    /// <summary>Send. Default "Enviar".</summary>
    public string Send { get; set; } = "Enviar";
    /// <summary>Copy. Default "Copiar".</summary>
    public string Copy { get; set; } = "Copiar";
    /// <summary>Row/card actions menu. Default "Ações".</summary>
    public string Actions { get; set; } = "Ações";
    /// <summary>Select all visible rows.</summary>
    public string SelectAllRows { get; set; } = "Selecionar todas as linhas visíveis";
    /// <summary>Select one grid row.</summary>
    public string SelectRow { get; set; } = "Selecionar linha";
    /// <summary>Select an option in a choice input.</summary>
    public string SelectOption { get; set; } = "Selecionar opção";
    /// <summary>Save a form or record. Default "Salvar".</summary>
    public string Save { get; set; } = "Salvar";
    /// <summary>DataGridForm operation error heading.</summary>
    public string DataGridFormOperationFailed { get; set; } = "Não foi possível concluir a operação.";
    /// <summary>DataGridForm provider validation failure.</summary>
    public string DataGridFormValidationFailed { get; set; } = "Revise os dados informados.";
    /// <summary>DataGridForm optimistic concurrency conflict.</summary>
    public string DataGridFormConflict { get; set; } = "Este registro foi alterado por outra operação. Recarregue os dados e tente novamente.";
    /// <summary>DataGridForm missing record failure.</summary>
    public string DataGridFormNotFound { get; set; } = "Este registro não existe mais.";
    /// <summary>DataGridForm authorization failure.</summary>
    public string DataGridFormForbidden { get; set; } = "Você não tem permissão para concluir esta operação.";
    /// <summary>DataGridForm refresh failure after a committed mutation.</summary>
    public string DataGridFormRefreshFailed { get; set; } = "A alteração foi salva, mas não foi possível atualizar a grade.";
    /// <summary>DataGridForm unsaved changes confirmation heading.</summary>
    public string DataGridFormUnsavedChangesTitle { get; set; } = "Descartar alterações?";
    /// <summary>DataGridForm unsaved changes confirmation message.</summary>
    public string DataGridFormUnsavedChangesMessage { get; set; } = "Existem alterações não salvas. Deseja descartá-las?";
    /// <summary>DataGridForm discard action.</summary>
    public string DataGridFormDiscardChanges { get; set; } = "Descartar alterações";
    /// <summary>DataGridForm continue editing action.</summary>
    public string DataGridFormContinueEditing { get; set; } = "Continuar editando";
    /// <summary>DataGridForm bulk action toolbar accessible label.</summary>
    public string DataGridFormBulkActions { get; set; } = "Ações em massa";
    /// <summary>DataGridForm row or bulk overflow-menu accessible label.</summary>
    public string DataGridFormMoreActions { get; set; } = "Mais ações";
    /// <summary>DataGridForm selected-item count format; placeholder zero receives the count.</summary>
    public string DataGridFormSelectedCount { get; set; } = "{0} selecionado(s)";
    /// <summary>Default DataGridForm bulk action confirmation.</summary>
    public string DataGridFormBulkConfirmation { get; set; } = "Deseja aplicar esta ação aos registros selecionados?";
    /// <summary>EntityPicker dialog heading.</summary>
    public string EntityPickerTitle { get; set; } = "Selecionar registro";
    /// <summary>EntityPicker empty value placeholder.</summary>
    public string EntityPickerPlaceholder { get; set; } = "Selecione um registro";
    /// <summary>EntityPicker generated column heading.</summary>
    public string EntityPickerItem { get; set; } = "Registro";
    /// <summary>DataFormWizard step navigation accessible label.</summary>
    public string DataFormWizardNavigation { get; set; } = "Etapas do formulário";
    /// <summary>DataImport file-selection prompt.</summary>
    public string DataImportUpload { get; set; } = "Selecione um arquivo CSV ou TSV";
    /// <summary>DataImport input constraints; placeholders receive maximum size and row count.</summary>
    public string DataImportUploadHint { get; set; } = "Até {0} e {1:N0} linhas";
    /// <summary>DataImport active processing status.</summary>
    public string DataImportProcessing { get; set; } = "Processando arquivo...";
    /// <summary>DataImport mapping section heading.</summary>
    public string DataImportMapping { get; set; } = "Mapeamento de colunas";
    /// <summary>DataImport mapping section guidance.</summary>
    public string DataImportMappingHint { get; set; } = "Associe cada campo de destino a uma coluna do arquivo.";
    /// <summary>DataImport unmapped source option.</summary>
    public string DataImportIgnoreColumn { get; set; } = "Não importar";
    /// <summary>DataImport preview section heading.</summary>
    public string DataImportPreview { get; set; } = "Pré-visualização validada";
    /// <summary>DataImport source row heading.</summary>
    public string DataImportRow { get; set; } = "Linha";
    /// <summary>DataImport row status heading.</summary>
    public string DataImportStatus { get; set; } = "Situação";
    /// <summary>DataImport error-list heading.</summary>
    public string DataImportErrors { get; set; } = "Erros";
    /// <summary>DataImport valid row status.</summary>
    public string DataImportValid { get; set; } = "Válida";
    /// <summary>DataImport invalid row status.</summary>
    public string DataImportInvalid { get; set; } = "Inválida";
    /// <summary>DataImport preview summary; placeholders receive valid, invalid and total counts.</summary>
    public string DataImportSummary { get; set; } = "{0:N0} válida(s), {1:N0} inválida(s), {2:N0} no total";
    /// <summary>DataImport preview truncation; placeholders receive shown and total counts.</summary>
    public string DataImportPreviewLimit { get; set; } = "Exibindo as primeiras {0:N0} de {1:N0} linhas.";
    /// <summary>DataImport primary action.</summary>
    public string DataImportImport { get; set; } = "Importar dados";
    /// <summary>DataImport accepted count.</summary>
    public string DataImportReady { get; set; } = "{0:N0} linha(s) pronta(s) para importar.";
    /// <summary>DataImport blocking validation status.</summary>
    public string DataImportResolveErrors { get; set; } = "Corrija ou remova as linhas inválidas antes de importar.";
    /// <summary>DataImport generated column format; placeholder receives a one-based index.</summary>
    public string DataImportColumn { get; set; } = "Coluna {0}";
    /// <summary>DataImport maximum file-size error; placeholder receives the formatted limit.</summary>
    public string DataImportFileTooLarge { get; set; } = "O arquivo ultrapassa o limite de {0}.";
    /// <summary>DataImport malformed quoted-field error; placeholder receives the unexpected character.</summary>
    public string DataImportUnexpectedCharacter { get; set; } = "Caractere inesperado '{0}' após um campo entre aspas.";
    /// <summary>DataImport unterminated quoted-field error.</summary>
    public string DataImportUnclosedQuote { get; set; } = "O arquivo contém um campo entre aspas não finalizado.";
    /// <summary>DataImport empty-file error.</summary>
    public string DataImportEmptyFile { get; set; } = "O arquivo está vazio.";
    /// <summary>DataImport file without data rows error.</summary>
    public string DataImportNoDataRows { get; set; } = "O arquivo não contém linhas de dados.";
    /// <summary>DataImport row-limit error.</summary>
    public string DataImportTooManyRows { get; set; } = "O arquivo ultrapassa o limite de linhas configurado.";
    /// <summary>DataImport column-limit error.</summary>
    public string DataImportTooManyColumns { get; set; } = "O arquivo ultrapassa o limite de colunas configurado.";
    /// <summary>DataImport cell-length error.</summary>
    public string DataImportCellTooLong { get; set; } = "Uma célula ultrapassa o limite de caracteres configurado.";
    /// <summary>DataImport required target value; placeholder receives the target header.</summary>
    public string DataImportRequiredValue { get; set; } = "{0} é obrigatório.";
    /// <summary>DataImport conversion failure; placeholder receives the target header.</summary>
    public string DataImportInvalidValue { get; set; } = "{0} possui um valor inválido.";
    /// <summary>Default DataGridForm delete confirmation.</summary>
    public string DataGridFormDeleteConfirmation { get; set; } = "Deseja remover este registro?";
    /// <summary>Default required-field validation. Default "Campo obrigatório.".</summary>
    public string Required { get; set; } = "Campo obrigatório.";
    /// <summary>DataForm validation summary heading.</summary>
    public string DataFormValidationSummary { get; set; } = "Corrija os erros abaixo:";
    /// <summary>Move a collection item up. Default "Mover para cima".</summary>
    public string MoveUp { get; set; } = "Mover para cima";
    /// <summary>Move a collection item down. Default "Mover para baixo".</summary>
    public string MoveDown { get; set; } = "Mover para baixo";
    /// <summary>DataForm minimum collection count format.</summary>
    public string DataFormMinimumItems { get; set; } = "Adicione pelo menos {0} item(ns).";
    /// <summary>DataForm maximum collection count format.</summary>
    public string DataFormMaximumItems { get; set; } = "Mantenha no máximo {0} item(ns).";
    /// <summary>Boolean affirmative option. Default "Sim".</summary>
    public string Yes { get; set; } = "Sim";
    /// <summary>Boolean negative option. Default "Não".</summary>
    public string No { get; set; } = "Não";
    /// <summary>Nullable option without a value. Default "Não informado".</summary>
    public string NotProvided { get; set; } = "Não informado";
    /// <summary>Reveal a masked password. Default "Mostrar senha".</summary>
    public string ShowPassword { get; set; } = "Mostrar senha";
    /// <summary>Mask a revealed password. Default "Ocultar senha".</summary>
    public string HidePassword { get; set; } = "Ocultar senha";

    // ── Navigation ───────────────────────────────────────────────────────
    /// <summary>Next (stepper, scheduler). Default "Próximo".</summary>
    public string Next { get; set; } = "Próximo";
    /// <summary>Previous (scheduler). Default "Anterior".</summary>
    public string Previous { get; set; } = "Anterior";
    /// <summary>Back (stepper, command palette). Default "Voltar".</summary>
    public string Back { get; set; } = "Voltar";
    /// <summary>Finish a stepper. Default "Concluir".</summary>
    public string Complete { get; set; } = "Concluir";
    /// <summary>Today (scheduler). Default "Hoje".</summary>
    public string Today { get; set; } = "Hoje";
    /// <summary>Next month (calendar). Default "Próximo mês".</summary>
    public string NextMonth { get; set; } = "Próximo mês";
    /// <summary>Previous month (calendar). Default "Mês anterior".</summary>
    public string PreviousMonth { get; set; } = "Mês anterior";
    /// <summary>Next slide (carousel). Default "Próximo slide".</summary>
    public string NextSlide { get; set; } = "Próximo slide";
    /// <summary>Previous slide (carousel). Default "Slide anterior".</summary>
    public string PreviousSlide { get; set; } = "Slide anterior";
    /// <summary>Skip link target label. Default "Pular para o conteúdo".</summary>
    public string SkipToContent { get; set; } = "Pular para o conteúdo";
    /// <summary>Scroll-to-top button. Default "Voltar ao topo".</summary>
    public string ScrollToTop { get; set; } = "Voltar ao topo";
    /// <summary>Close the navigation drawer/pane. Default "Fechar navegação".</summary>
    public string CloseNavigation { get; set; } = "Fechar navegação";
    /// <summary>Open a FAB menu. Default "Abrir menu".</summary>
    public string OpenMenu { get; set; } = "Abrir menu";
    /// <summary>Close a FAB menu. Default "Fechar menu".</summary>
    public string CloseMenu { get; set; } = "Fechar menu";

    // ── Search / data ────────────────────────────────────────────────────
    /// <summary>Search box placeholder. Default "Buscar...".</summary>
    public string SearchPlaceholder { get; set; } = "Buscar...";
    /// <summary>Command palette placeholder. Default "Buscar comando...".</summary>
    public string CommandPlaceholder { get; set; } = "Buscar comando...";
    /// <summary>Filter box placeholder. Default "Filtrar…".</summary>
    public string FilterPlaceholder { get; set; } = "Filtrar…";
    /// <summary>Loading indicator. Default "Buscando…".</summary>
    public string Searching { get; set; } = "Buscando…";
    /// <summary>Empty result list. Default "Nenhum resultado.".</summary>
    public string NoResults { get; set; } = "Nenhum resultado.";
    /// <summary>Empty data grid. Default "Nenhum registro encontrado.".</summary>
    public string NoRecords { get; set; } = "Nenhum registro encontrado.";
    /// <summary>Remove a grouping chip (data grid). Default "Remover agrupamento".</summary>
    public string RemoveGrouping { get; set; } = "Remover agrupamento";

    // ── Date grouping (data grid) ────────────────────────────────────────
    // Nomes das unidades: compõem o chip de agrupamento ("Data (Ano › Mês › Dia)").
    /// <summary>Year interval name. Default "Ano".</summary>
    public string Year { get; set; } = "Ano";
    /// <summary>Quarter interval name. Default "Trimestre".</summary>
    public string Quarter { get; set; } = "Trimestre";
    /// <summary>Month interval name. Default "Mês".</summary>
    public string Month { get; set; } = "Mês";
    /// <summary>Week interval name. Default "Semana".</summary>
    public string Week { get; set; } = "Semana";
    /// <summary>Day interval name. Default "Dia".</summary>
    public string Day { get; set; } = "Dia";
    /// <summary>Hour interval name. Default "Hora".</summary>
    public string Hour { get; set; } = "Hora";
    /// <summary>Pager unit when the grid is grouped (it pages groups, not rows). Default "grupos".</summary>
    public string Groups { get; set; } = "grupos";
    /// <summary>Quarter group label; <c>{0}</c> is the quarter number. Default "T{0}".</summary>
    public string QuarterAbbreviation { get; set; } = "T{0}";
    /// <summary>Week group label; <c>{0}</c> is the first day of the week. Default "Semana de {0}".</summary>
    public string WeekOf { get; set; } = "Semana de {0}";

    // ── Chat ─────────────────────────────────────────────────────────────
    /// <summary>Chat composer placeholder. Default "Digite uma mensagem...".</summary>
    public string MessagePlaceholder { get; set; } = "Digite uma mensagem...";
    /// <summary>Empty chat. Default "Nenhuma mensagem ainda. Comece a conversa!".</summary>
    public string NoMessages { get; set; } = "Nenhuma mensagem ainda. Comece a conversa!";

    /// <summary>
    /// An English set. Use as-is, or as a starting point:
    /// <c>o.Texts = OmniTexts.English()</c>.
    /// </summary>
    public static OmniTexts English() => new()
    {
        Close = "Close",
        Clear = "Clear",
        ClearAll = "Clear all",
        Cancel = "Cancel",
        Confirm = "Confirm",
        Apply = "Apply",
        Add = "Add",
        Edit = "Edit",
        AddCondition = "Add condition",
        AddGroup = "Add group",
        ApplyToFilter = "Apply to filter",
        DataFilterMinimum = "Minimum",
        DataFilterMaximum = "Maximum",
        DataFilterStartDate = "Start date",
        DataFilterEndDate = "End date",
        DataFilterStartValue = "Start value",
        DataFilterEndValue = "End value",
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
        DataImportSummary = "{0:N0} valid, {1:N0} invalid, {2:N0} total",
        DataImportPreviewLimit = "Showing the first {0:N0} of {1:N0} rows.",
        DataImportImport = "Import data",
        DataImportReady = "{0:N0} row(s) ready to import.",
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
        DataFormValidationSummary = "Fix the errors below:",
        MoveUp = "Move up",
        MoveDown = "Move down",
        DataFormMinimumItems = "Add at least {0} item(s).",
        DataFormMaximumItems = "Keep at most {0} item(s).",
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
        NextMonth = "Next month",
        PreviousMonth = "Previous month",
        NextSlide = "Next slide",
        PreviousSlide = "Previous slide",
        SkipToContent = "Skip to content",
        ScrollToTop = "Back to top",
        CloseNavigation = "Close navigation",
        OpenMenu = "Open menu",
        CloseMenu = "Close menu",
        SearchPlaceholder = "Search...",
        CommandPlaceholder = "Search command...",
        FilterPlaceholder = "Filter…",
        Searching = "Searching…",
        NoResults = "No results.",
        NoRecords = "No records found.",
        RemoveGrouping = "Remove grouping",
        Year = "Year",
        Quarter = "Quarter",
        Month = "Month",
        Week = "Week",
        Day = "Day",
        Hour = "Hour",
        Groups = "groups",
        QuarterAbbreviation = "Q{0}",
        WeekOf = "Week of {0}",
        MessagePlaceholder = "Type a message...",
        NoMessages = "No messages yet. Start the conversation!",
        KeyboardSpace = "Space",
        KeyboardBackspace = "Backspace",
        KeyboardSymbols = "Symbols",
        KeyboardLabel = "Virtual keyboard",
    };
}
