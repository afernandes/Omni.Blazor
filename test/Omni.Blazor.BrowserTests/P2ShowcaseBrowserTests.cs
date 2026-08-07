using Microsoft.Playwright;

namespace Omni.Blazor.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class P2ShowcaseBrowserTests(BrowserFixture fixture)
{
    [Fact]
    public async Task New_P2_showcases_render_their_real_interactive_components()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        (string Route, string Heading, string Selector)[] pages =
        [
            ("property-grid", "Property Grid", ".omni-property-grid"),
            ("scheduler-form", "Scheduler Form", ".omni-scheduler"),
            ("kanban-form", "Kanban Form", ".omni-kanban"),
            ("gantt-form", "Gantt Form", ".omni-gantt"),
            ("workflow-designer", "Workflow Designer", ".omni-workflow-designer")
        ];

        foreach ((string route, string heading, string selector) in pages)
        {
            await page.GotoAsync($"{fixture.BaseUrl}/showcase/{route}");
            await page.GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true }).First.WaitForAsync();
            await page.Locator(selector).First.WaitForAsync();
        }
    }

    [Fact]
    public async Task Chart_showcase_renders_every_new_P2_chart_family()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/chart");

        await page.Locator(".omni-section-title", new() { HasText = "COLUMN E BAR EMPILHADOS" }).WaitForAsync();
        await page.Locator(".omni-section-title", new() { HasText = "SCATTER E BUBBLE" }).WaitForAsync();
        await page.Locator(".omni-section-title", new() { HasText = "RADAR" }).WaitForAsync();
        await page.Locator(".omni-section-title", new() { HasText = "GAUGE" }).WaitForAsync();
        Assert.True(await page.Locator(".omni-chart-gauge-value").IsVisibleAsync());
    }

    [Fact]
    public async Task SchedulerForm_reprojects_an_appointment_after_editing_its_dates()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();
        List<string> pageErrors = [];
        page.PageError += (_, error) => pageErrors.Add(error);
        DateTime expectedStart = DateTime.Today.AddHours(11).AddMinutes(30);
        DateTime expectedEnd = expectedStart.AddHours(2);
        string expectedStartText = expectedStart.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string expectedEndText = expectedEnd.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/scheduler-form");
        await page.GetByTestId("scheduler-form-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        ILocator appointment = page.Locator(".omni-scheduler").GetByText("Planejamento do produto", new() { Exact = true });
        await appointment.ClickAsync();

        ILocator editor = page.Locator(".omni-data-grid-form-editor");
        await editor.WaitForAsync();
        ILocator inputs = editor.Locator(".omni-datepicker-input");
        await inputs.Nth(0).FillAsync(expectedStartText);
        await inputs.Nth(1).FillAsync(expectedEndText);
        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();
        try
        {
            await editor.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        }
        catch (TimeoutException exception)
        {
            string editorText = await editor.InnerTextAsync();
            string startValue = await inputs.Nth(0).InputValueAsync();
            string endValue = await inputs.Nth(1).InputValueAsync();
            throw new InvalidOperationException(
                $"Scheduler editor remained open. Start='{startValue}', End='{endValue}'."
                + $"{Environment.NewLine}{editorText}{Environment.NewLine}{string.Join(Environment.NewLine, pageErrors)}",
                exception);
        }

        await appointment.ClickAsync();
        await editor.WaitForAsync();
        Assert.Equal(expectedStartText, await editor.Locator(".omni-datepicker-input").Nth(0).InputValueAsync());
        Assert.Equal(expectedEndText, await editor.Locator(".omni-datepicker-input").Nth(1).InputValueAsync());
    }

    [Fact]
    public async Task SchedulerForm_date_picker_popover_is_not_clipped_by_the_editor()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/scheduler-form");
        await page.GetByTestId("scheduler-form-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await page.Locator(".omni-scheduler")
            .GetByText("Planejamento do produto", new() { Exact = true })
            .ClickAsync();

        ILocator editor = page.Locator(".omni-data-grid-form-editor");
        await editor.WaitForAsync();
        await editor.Locator(".omni-datepicker-input").First.ClickAsync();

        ILocator popover = editor.Locator(".omni-datepicker-popover");
        await popover.WaitForAsync();
        string visibility = await popover.EvaluateAsync<string>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                const ancestors = [];
                for (let parent = element.parentElement; parent; parent = parent.parentElement) {
                    const style = getComputedStyle(parent);
                    if (/(auto|scroll|hidden|clip)/.test(`${style.overflow} ${style.overflowX} ${style.overflowY}`)) {
                        const parentRect = parent.getBoundingClientRect();
                        ancestors.push({
                            className: parent.className,
                            overflow: `${style.overflow}/${style.overflowX}/${style.overflowY}`,
                            rect: { left: parentRect.left, top: parentRect.top, right: parentRect.right, bottom: parentRect.bottom }
                        });
                    }
                }
                const points = [
                    [rect.left + 8, rect.top + 8],
                    [rect.right - 8, rect.top + 8],
                    [rect.left + 8, rect.bottom - 8],
                    [rect.right - 8, rect.bottom - 8],
                    [rect.left + rect.width / 2, rect.bottom - 8]
                ];
                const samples = points.map(([x, y]) => {
                    const hit = document.elementFromPoint(x, y);
                    return { x, y, hit: hit?.className ?? hit?.tagName ?? null, owned: !!hit && element.contains(hit) };
                });
                return JSON.stringify({
                    rect: { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom },
                    viewport: { width: innerWidth, height: innerHeight },
                    ancestors,
                    samples
                });
            }
            """);

        using System.Text.Json.JsonDocument geometry = System.Text.Json.JsonDocument.Parse(visibility);
        System.Text.Json.JsonElement root = geometry.RootElement;
        System.Text.Json.JsonElement rect = root.GetProperty("rect");
        System.Text.Json.JsonElement viewport = root.GetProperty("viewport");
        bool allSamplesBelongToPopover = root.GetProperty("samples")
            .EnumerateArray()
            .All(sample => sample.GetProperty("owned").GetBoolean());

        Assert.True(rect.GetProperty("left").GetDouble() >= 0, visibility);
        Assert.True(rect.GetProperty("top").GetDouble() >= 0, visibility);
        Assert.True(rect.GetProperty("right").GetDouble() <= viewport.GetProperty("width").GetDouble(), visibility);
        Assert.True(rect.GetProperty("bottom").GetDouble() <= viewport.GetProperty("height").GetDouble(), visibility);
        Assert.True(allSamplesBelongToPopover, visibility);
    }

    [Fact]
    public async Task GanttForm_edits_hierarchy_by_task_title_without_exposing_identifiers()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/gantt-form");
        await page.GetByTestId("gantt-form-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await page.Locator(".omni-gantt-bar")
            .Filter(new() { HasText = "Testes e documentação" })
            .ClickAsync();

        ILocator editor = page.Locator(".omni-data-grid-form-editor");
        await editor.WaitForAsync();
        Assert.Equal(0, await editor.Locator("input[name='Id']").CountAsync());
        Assert.Equal(0, await editor.GetByText("ParentId", new() { Exact = true }).CountAsync());

        ILocator parentField = editor.Locator(".omni-data-form-cell")
            .Filter(new() { HasText = "Tarefa pai" });
        ILocator parentSelect = parentField.Locator(".omni-select-trigger");
        await Assertions.Expect(parentSelect).ToContainTextAsync("Entrega P2");
        Assert.Equal("Entrega P2", (await parentSelect.InnerTextAsync()).Trim());
        await parentSelect.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Publicação do pacote", Exact = true }).ClickAsync();
        await Assertions.Expect(parentSelect).ToContainTextAsync("Publicação do pacote");

        await editor.GetByRole(AriaRole.Button, new() { Name = "Salvar", Exact = true }).ClickAsync();
        await editor.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        ILocator newParentRow = page.Locator(".omni-gantt-left-row")
            .Filter(new() { HasText = "Publicação do pacote" });
        await newParentRow.Locator("button.omni-gantt-chevron").ClickAsync();
        await page.Locator(".omni-gantt-left-row")
            .Filter(new() { HasText = "Testes e documentação" })
            .WaitForAsync();
    }

    [Fact]
    public async Task GanttForm_empty_parent_select_keeps_the_standard_input_height()
    {
        await using IBrowserContext context = await fixture.CreateContextAsync();
        IPage page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/showcase/gantt-form");
        await page.GetByTestId("gantt-form-interactive").WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await page.Locator(".omni-gantt-bar")
            .Filter(new() { HasText = "Entrega P2" })
            .ClickAsync();

        ILocator editor = page.Locator(".omni-data-grid-form-editor");
        await editor.WaitForAsync();
        ILocator parentSelect = editor.Locator(".omni-data-form-cell")
            .Filter(new() { HasText = "Tarefa pai" })
            .Locator(".omni-select-trigger");
        ILocator titleInput = editor.Locator("input[name='Titulo']");

        double selectHeight = await parentSelect.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().height");
        double inputHeight = await titleInput.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().height");

        Assert.True(selectHeight >= 38, $"Expected a standard input height, but found {selectHeight}px.");
        Assert.InRange(Math.Abs(selectHeight - inputHeight), 0, 1);
    }
}
