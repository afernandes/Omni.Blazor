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
    /// <summary>Add a filter condition (data filter). Default "Adicionar condição".</summary>
    public string AddCondition { get; set; } = "Adicionar condição";
    /// <summary>Add a filter group (data filter). Default "Adicionar grupo".</summary>
    public string AddGroup { get; set; } = "Adicionar grupo";
    /// <summary>Apply the typed SQL back to the filter (data filter). Default "Aplicar ao filtro".</summary>
    public string ApplyToFilter { get; set; } = "Aplicar ao filtro";
    /// <summary>Remove. Default "Remover".</summary>
    public string Remove { get; set; } = "Remover";
    /// <summary>Send. Default "Enviar".</summary>
    public string Send { get; set; } = "Enviar";
    /// <summary>Copy. Default "Copiar".</summary>
    public string Copy { get; set; } = "Copiar";
    /// <summary>Row/card actions menu. Default "Ações".</summary>
    public string Actions { get; set; } = "Ações";
    /// <summary>Save a form or record. Default "Salvar".</summary>
    public string Save { get; set; } = "Salvar";
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
        AddCondition = "Add condition",
        AddGroup = "Add group",
        ApplyToFilter = "Apply to filter",
        Remove = "Remove",
        Send = "Send",
        Copy = "Copy",
        Actions = "Actions",
        Save = "Save",
        Required = "Required field.",
        DataFormValidationSummary = "Fix the errors below:",
        MoveUp = "Move up",
        MoveDown = "Move down",
        DataFormMinimumItems = "Add at least {0} item(s).",
        DataFormMaximumItems = "Keep at most {0} item(s).",
        Yes = "Yes",
        No = "No",
        NotProvided = "Not provided",
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
        MessagePlaceholder = "Type a message...",
        NoMessages = "No messages yet. Start the conversation!",
    };
}
