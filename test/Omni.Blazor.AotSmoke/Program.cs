using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Localization;
using System.Globalization;

var services = new ServiceCollection();
services.AddOmniComponents();
using ServiceProvider provider = services.BuildServiceProvider();
using IServiceScope scope = provider.CreateScope();
IOmniLocalizer localizer = scope.ServiceProvider.GetRequiredService<IOmniLocalizer>();
bool localizedResourcesWork = localizer.Localize(
    OmniTranslationKeys.Close,
    CultureInfo.GetCultureInfo("en-US")).Value == "Close";

DataFormSchema<AotContact> formSchema = DataFormSchema<AotContact>.Builder()
    .AutoGenerateFields(false)
    .Field(contact => contact.Name, field => field.Label("Name"))
    .Field(contact => contact.Email, field => field.Label("Email"))
    .Build();

DataGridSchema<AotContact> gridSchema = DataGridSchema<AotContact>.Builder()
    .Column(contact => contact.Name, column => column.Title("Name"))
    .Column(contact => contact.Email, column => column.Title("Email"))
    .Build();

DataFilterSchema<AotContact> filterSchema = DataFilterSchema<AotContact>.Builder()
    .Field(contact => contact.Name, field => field.Title("Name"))
    .Field(contact => contact.Email, field => field.Title("Email"))
    .Build();

DataImportSchema<AotContact> importSchema = DataImportSchema<AotContact>.Builder()
    .Factory(static () => new AotContact())
    .Column(contact => contact.Name, column => column.Header("Name").Required())
    .Column(contact => contact.Email, column => column.Header("Email"))
    .Build();

SchedulerSchema<AotContact> schedulerSchema = SchedulerSchema<AotContact>.Builder()
    .Range(contact => contact.Start, contact => contact.End)
    .Text(contact => contact.Name)
    .Build();

GanttSchema<AotContact> ganttSchema = GanttSchema<AotContact>.Builder()
    .Hierarchy(contact => contact.Id, contact => contact.ParentId)
    .Task(contact => contact.Name, contact => contact.Start, contact => contact.End)
    .Progress(contact => contact.Progress)
    .Build();

AotContact contact = new()
{
    Id = 7,
    Name = "Ada",
    Email = "ada@example.com",
    Start = new DateTime(2026, 8, 7, 9, 0, 0, DateTimeKind.Utc),
    End = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc),
    Progress = 50
};

DataFilterQuery<AotContact> filterQuery = filterSchema.Query(query => query
    .Condition(item => item.Name, FilterOperator.StartsWith, "A"));
bool typedRuntimePathsWork = filterQuery.Apply([contact], filterSchema).Single() == contact
    && string.Equals(gridSchema.Columns[0].Property(contact)?.ToString(), "Ada", StringComparison.Ordinal)
    && string.Equals(schedulerSchema.Text?.Invoke(contact), "Ada", StringComparison.Ordinal)
    && Equals(ganttSchema.Key?.Invoke(contact), 7)
    && ganttSchema.Start(contact) == contact.Start;

if (services.Count == 0
    || formSchema.Count != 2
    || gridSchema.Columns.Count != 2
    || filterSchema.Fields.Count != 2
    || importSchema.Count != 2
    || schedulerSchema.Text is null
    || ganttSchema.Key is null
    || !typedRuntimePathsWork
    || !localizedResourcesWork)
    return 1;

Type[] rootedComponents =
[
    typeof(OmniButton),
    typeof(OmniDataForm<AotContact>),
    typeof(OmniDataFilter<AotContact>),
    typeof(OmniDataGrid<AotContact>),
    typeof(OmniDataImport<AotContact>),
    typeof(OmniGantt<AotContact>),
    typeof(OmniOverlayHosts),
    typeof(OmniScheduler<AotContact>),
];

if (rootedComponents.Any(static component => !typeof(IComponent).IsAssignableFrom(component)))
    return 2;

Console.WriteLine("Omni.Blazor Native AOT smoke test passed.");
return 0;

internal sealed class AotContact
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public double? Progress { get; set; }
}
