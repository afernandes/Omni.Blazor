using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Models;

internal enum DataFormEditor
{
    Auto,
    Text,
    TextArea,
    Password,
    Email,
    Telephone,
    Url,
    Number,
    Date,
    DateTime,
    Time,
    CheckBox,
    Switch,
    Select,
    Collection,
    Custom
}

internal sealed record DataFormOption(object? Value, string Text);

internal interface IDataFormFieldTemplate<TModel> where TModel : class;

internal sealed record DataFormFieldTemplate<TModel, TValue>(
    RenderFragment<DataFormFieldContext<TModel, TValue>> Content)
    : IDataFormFieldTemplate<TModel>
    where TModel : class;

/// <summary>
/// Immutable metadata for one property configured by a
/// <see cref="DataFormSchema{TModel}"/>.
/// </summary>
public sealed class DataFormField<TModel> where TModel : class
{
    internal DataFormField(
        string property,
        DataFormPropertyPath propertyPath,
        string? groupId,
        string? label,
        string? placeholder,
        string? hint,
        string? hintRight,
        DataFormEditor editor,
        int? order,
        int columnSpan,
        bool hasExplicitColumnSpan,
        IReadOnlyDictionary<Breakpoint, int> responsiveSpans,
        bool visible,
        bool hasExplicitVisible,
        Func<TModel, bool>? visibleWhen,
        Func<TModel, bool>? enabledWhen,
        Func<TModel, bool>? readOnlyWhen,
        Func<TModel, bool>? requiredWhen,
        bool? required,
        bool enforceRequired,
        string? requiredError,
        bool? disabled,
        bool? readOnly,
        string? @class,
        string? style,
        IReadOnlyList<DataFormOption>? options,
        IReadOnlyDictionary<string, object?> editorParameters,
        IDataFormFieldTemplate<TModel>? template,
        IReadOnlyList<IDataFormFieldValidator<TModel>> validators,
        IDataFormLookupDefinition<TModel>? lookup = null,
        IDataFormCollectionDefinition<TModel>? collection = null,
        Type? customEditorType = null)
    {
        Property = property;
        PropertyPath = propertyPath;
        GroupId = groupId;
        Label = label;
        Placeholder = placeholder;
        Hint = hint;
        HintRight = hintRight;
        Editor = editor;
        Order = order;
        ColumnSpan = columnSpan;
        HasExplicitColumnSpan = hasExplicitColumnSpan;
        ResponsiveSpans = responsiveSpans;
        Visible = visible;
        HasExplicitVisible = hasExplicitVisible;
        VisibleWhen = visibleWhen;
        EnabledWhen = enabledWhen;
        ReadOnlyWhen = readOnlyWhen;
        RequiredWhen = requiredWhen;
        Required = required;
        EnforceRequired = enforceRequired;
        RequiredError = requiredError;
        Disabled = disabled;
        ReadOnly = readOnly;
        Class = @class;
        Style = style;
        Options = options;
        EditorParameters = editorParameters;
        Template = template;
        Validators = validators;
        Lookup = lookup;
        Collection = collection;
        CustomEditorType = customEditorType;
    }

    /// <summary>Name of the public model property represented by this field.</summary>
    public string Property { get; }

    /// <summary>Effective label displayed by the field.</summary>
    public string? Label { get; }

    /// <summary>Effective placeholder displayed by the editor.</summary>
    public string? Placeholder { get; }

    /// <summary>Effective helper text displayed by the field.</summary>
    public string? Hint { get; }

    /// <summary>Effective text displayed on the right side of the label.</summary>
    public string? HintRight { get; }

    /// <summary>Effective display order.</summary>
    public int? Order { get; }

    /// <summary>Number of form columns occupied by the field.</summary>
    public int ColumnSpan { get; }

    /// <summary>Responsive field spans keyed by the standard Omni breakpoints.</summary>
    public IReadOnlyDictionary<Breakpoint, int> ResponsiveSpans { get; }

    /// <summary>Whether the field is statically visible.</summary>
    public bool Visible { get; }

    internal DataFormEditor Editor { get; }
    internal bool HasExplicitColumnSpan { get; }
    internal bool HasExplicitVisible { get; }
    internal DataFormPropertyPath PropertyPath { get; }
    internal string? GroupId { get; }
    internal Func<TModel, bool>? VisibleWhen { get; }
    internal Func<TModel, bool>? EnabledWhen { get; }
    internal Func<TModel, bool>? ReadOnlyWhen { get; }
    internal Func<TModel, bool>? RequiredWhen { get; }
    internal bool? Required { get; }
    internal bool EnforceRequired { get; }
    internal string? RequiredError { get; }
    internal bool? Disabled { get; }
    internal bool? ReadOnly { get; }
    internal string? Class { get; }
    internal string? Style { get; }
    internal IReadOnlyList<DataFormOption>? Options { get; }
    internal IReadOnlyDictionary<string, object?> EditorParameters { get; }
    internal IDataFormFieldTemplate<TModel>? Template { get; }
    internal IReadOnlyList<IDataFormFieldValidator<TModel>> Validators { get; }
    internal IDataFormLookupDefinition<TModel>? Lookup { get; }
    internal IDataFormCollectionDefinition<TModel>? Collection { get; }
    internal Type? CustomEditorType { get; }

    internal bool IsRequired(TModel model)
        => Required == true || (RequiredWhen?.Invoke(model) ?? false);

    internal bool IsDisabled(TModel model)
        => Disabled == true || (EnabledWhen is not null && !EnabledWhen(model));

    internal bool IsReadOnly(TModel model)
        => ReadOnly == true || (ReadOnlyWhen?.Invoke(model) ?? false);
}

/// <summary>
/// Strongly typed binding context supplied to a custom DataForm field template.
/// </summary>
public sealed class DataFormFieldContext<TModel, TValue> where TModel : class
{
    internal DataFormFieldContext(
        TModel model,
        DataFormField<TModel> field,
        EditContext editContext,
        Func<TValue> getValue,
        EventCallback<TValue> valueChanged,
        Expression<Func<TValue>> valueExpression)
    {
        Model = model;
        Property = field.Property;
        Label = field.Label;
        EditContext = editContext;
        _getValue = getValue;
        ValueChanged = valueChanged;
        ValueExpression = valueExpression;
        FieldIdentifier = FieldIdentifier.Create(valueExpression);
    }

    private readonly Func<TValue> _getValue;

    /// <summary>Model instance being edited.</summary>
    public TModel Model { get; }

    /// <summary>Name of the property represented by the field.</summary>
    public string Property { get; }

    /// <summary>Effective field label.</summary>
    public string? Label { get; }

    /// <summary>EditContext used by the containing DataForm.</summary>
    public EditContext EditContext { get; }

    /// <summary>Current strongly typed field value.</summary>
    public TValue Value => _getValue();

    /// <summary>Callback that writes a new value through the DataForm binding pipeline.</summary>
    public EventCallback<TValue> ValueChanged { get; }

    /// <summary>Value expression used by Blazor validation components.</summary>
    public Expression<Func<TValue>> ValueExpression { get; }

    /// <summary>Blazor field identifier derived from <see cref="ValueExpression"/>.</summary>
    public FieldIdentifier FieldIdentifier { get; }

    /// <summary>Whether the current EditContext marks this field as modified.</summary>
    public bool IsModified => EditContext.IsModified(FieldIdentifier);

    /// <summary>Current validation messages associated with this field.</summary>
    public IEnumerable<string> Errors => EditContext.GetValidationMessages(FieldIdentifier);

    /// <summary>
    /// Notifies the EditContext after a custom template changes the value without
    /// using a Blazor/Omni form component. Standard form components already do this.
    /// </summary>
    public void NotifyFieldChanged() => EditContext.NotifyFieldChanged(FieldIdentifier);
}

/// <summary>Event data emitted after a generated DataForm editor changes a property.</summary>
public sealed record DataFormFieldChangedEventArgs<TModel>(
    TModel Model,
    string Property,
    object? Value)
    where TModel : class;
