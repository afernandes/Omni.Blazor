using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniCommandPalette"/>: open/closed render,
/// grouping, filtering (label + keywords), selection callbacks, and the splat.
/// </summary>
public class OmniCommandPaletteTests : TestContextBase
{
    private static OmniCommand[] Sample() => new[]
    {
        new OmniCommand("Ir para Dashboard") { Category = "Navegação", Icon = "bar-chart" },
        new OmniCommand("Novo pedido") { Category = "Ações", Icon = "plus", Keywords = new[] { "criar" } },
        new OmniCommand("Configurações") { Category = "Conta", Icon = "settings" },
        new OmniCommand("Sair") { Category = "Conta", Keywords = new[] { "logout" } },
    };

    [Fact]
    public void Closed_renders_nothing()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p.Add(c => c.Commands, Sample()));
        Assert.Empty(cut.FindAll(".omni-cmdk"));
    }

    [Fact]
    public void Open_renders_panel_input_and_all_commands()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample()));
        Assert.NotNull(cut.Find(".omni-cmdk .omni-cmdk-input"));
        Assert.Equal(4, cut.FindAll(".omni-cmdk-item").Count);
    }

    [Fact]
    public void Renders_group_labels_per_category()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample()));
        var groups = cut.FindAll(".omni-cmdk-group").Select(g => g.TextContent).ToList();
        Assert.Contains("Navegação", groups);
        Assert.Contains("Ações", groups);
        Assert.Contains("Conta", groups);
    }

    [Fact]
    public void Typing_filters_by_label()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample()));
        cut.Find(".omni-cmdk-input").Input("dash");
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Single(items);
        Assert.Contains("Dashboard", items[0].TextContent);
    }

    [Fact]
    public void Typing_filters_by_keyword()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample()));
        cut.Find(".omni-cmdk-input").Input("logout");   // only "Sair" via keyword
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Single(items);
        Assert.Contains("Sair", items[0].TextContent);
    }

    [Fact]
    public void Empty_state_when_no_match()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample()));
        cut.Find(".omni-cmdk-input").Input("zzzzzz");
        Assert.Empty(cut.FindAll(".omni-cmdk-item"));
        Assert.NotNull(cut.Find(".omni-cmdk-empty"));
    }

    [Fact]
    public void Clicking_command_fires_OnCommand()
    {
        OmniCommand? chosen = null;
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample())
            .Add(c => c.OnCommand, EventCallback.Factory.Create<OmniCommand>(this, c => chosen = c)));
        cut.FindAll(".omni-cmdk-item")[0].Click();
        Assert.NotNull(chosen);
        Assert.Equal("Ir para Dashboard", chosen!.Label);
    }

    [Fact]
    public void Command_Action_runs_on_select()
    {
        var ran = false;
        var cmds = new[] { new OmniCommand("Run me") { Action = () => { ran = true; return Task.CompletedTask; } } };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-item").Click();
        Assert.True(ran);
    }

    [Fact]
    public void Appends_Class_and_splats_attributes()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Sample())
            .Add(c => c.Class, "x").AddUnmatched("data-testid", "cp1"));
        var root = cut.Find(".omni-cmdk");
        Assert.Contains("x", root.ClassName);
        Assert.Equal("cp1", root.GetAttribute("data-testid"));
    }

    // ---- Fuzzy / diacritic-insensitive search ------------------------------

    [Theory]
    [InlineData("usuario")]   // sem acento
    [InlineData("usuário")]   // com acento
    [InlineData("USUÁRIO")]   // maiúsculo + acento
    public void Matches_ignoring_diacritics_and_case(string query)
    {
        var cmds = new[] { new OmniCommand("Usuários") { Category = "Admin" } };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input(query);
        Assert.Single(cut.FindAll(".omni-cmdk-item"));
    }

    [Fact]
    public void Matches_accented_label_when_query_is_plain()
    {
        var cmds = new[] { new OmniCommand("Configurações") };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("configuracoes");
        Assert.Single(cut.FindAll(".omni-cmdk-item"));
    }

    [Fact]
    public void Acronym_query_matches_word_initials()
    {
        var cmds = new[]
        {
            new OmniCommand("Tabela de Usuários") { Category = "App" },
            new OmniCommand("Configurações") { Category = "App" },
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("tu");
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Single(items);
        Assert.Contains("Tabela de Usuários", items[0].TextContent);
    }

    [Fact]
    public void Ranks_prefix_above_midword_match()
    {
        var cmds = new[]
        {
            new OmniCommand("Reset password"),   // "set" no meio da palavra
            new OmniCommand("Settings"),          // "set" é prefixo → deve vir primeiro
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("set");
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Equal(2, items.Count);
        Assert.Contains("Settings", items[0].TextContent);
        Assert.Contains("Reset", items[1].TextContent);
    }

    [Fact]
    public void Highlights_matched_characters()
    {
        var cmds = new[] { new OmniCommand("Dashboard") };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("dash");
        var marks = cut.FindAll(".omni-cmdk-hit");
        Assert.NotEmpty(marks);
        Assert.Equal("Dash", string.Concat(marks.Select(m => m.TextContent)));
    }

    [Fact]
    public void Highlight_tracks_the_substring_that_actually_matched()
    {
        // "set" is a contiguous run inside "settings" — NOT the scattered s/e/t in "Sweet".
        var cmds = new[] { new OmniCommand("Sweet settings") };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("set");
        var marks = cut.FindAll(".omni-cmdk-hit");
        Assert.Equal("set", string.Concat(marks.Select(m => m.TextContent)));
    }

    [Fact]
    public void Label_substring_outranks_keyword_only_match()
    {
        var cmds = new[]
        {
            new OmniCommand("Imprimir recibo") { Keywords = new[] { "print" } }, // keyword-only
            new OmniCommand("Quick print"),                                       // label substring
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("print");
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Contains("Quick print", items[0].TextContent);
    }

    [Fact]
    public void Label_substring_outranks_category_only_match()
    {
        var cmds = new[]
        {
            new OmniCommand("Saldo") { Category = "Conta" },  // category-only
            new OmniCommand("Recontagem"),                    // label substring
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.Find(".omni-cmdk-input").Input("conta");
        var items = cut.FindAll(".omni-cmdk-item");
        Assert.Contains("Recontagem", items[0].TextContent);
    }

    [Fact]
    public void Duplicate_command_reference_does_not_crash_on_filter()
    {
        var shared = new OmniCommand("Shared cmd") { Category = "Nav" };
        var cmds = new[] { shared, shared };   // same reference twice
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        // First keystroke triggers a diff — previously threw "duplicate key".
        cut.Find(".omni-cmdk-input").Input("shared");
        Assert.Equal(2, cut.FindAll(".omni-cmdk-item").Count);
    }

    // ---- Sub-command pages -------------------------------------------------

    private static OmniCommand[] WithPage() => new[]
    {
        new OmniCommand("Configurações") { Children = new[] { new OmniCommand("Tema"), new OmniCommand("Perfil") } },
        new OmniCommand("Início"),
    };

    [Fact]
    public void Parent_with_children_shows_drill_in_chevron()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, WithPage()));
        Assert.NotEmpty(cut.FindAll(".omni-cmdk-item-more"));
    }

    [Fact]
    public void Selecting_parent_drills_into_subpage_without_running_it()
    {
        OmniCommand? fired = null;
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, WithPage())
            .Add(c => c.OnCommand, EventCallback.Factory.Create<OmniCommand>(this, c => fired = c)));
        cut.FindAll(".omni-cmdk-item").First(b => b.TextContent.Contains("Configurações")).Click();

        var labels = cut.FindAll(".omni-cmdk-item-label").Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("Tema", labels);
        Assert.Contains("Perfil", labels);
        Assert.DoesNotContain("Início", labels);
        Assert.NotNull(cut.Find(".omni-cmdk-back"));
        Assert.Equal("Configurações", cut.Find(".omni-cmdk-crumb").TextContent.Trim());
        Assert.Null(fired);   // drilling in is not "running" the command
    }

    [Fact]
    public void Back_button_returns_to_root()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, WithPage()));
        cut.FindAll(".omni-cmdk-item").First(b => b.TextContent.Contains("Configurações")).Click();
        cut.Find(".omni-cmdk-back").Click();

        var labels = cut.FindAll(".omni-cmdk-item-label").Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("Início", labels);
        Assert.Empty(cut.FindAll(".omni-cmdk-back"));
    }

    [Fact]
    public void Escape_in_subpage_pops_instead_of_closing()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, WithPage()));
        cut.FindAll(".omni-cmdk-item").First(b => b.TextContent.Contains("Configurações")).Click();
        Assert.NotEmpty(cut.FindAll(".omni-cmdk-back"));

        cut.Find(".omni-cmdk-input").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".omni-cmdk-back"));   // popped to root…
        Assert.NotEmpty(cut.FindAll(".omni-cmdk"));     // …but still open
    }

    // ---- Async source ------------------------------------------------------

    [Fact]
    public void Async_CommandSource_results_appear()
    {
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, new[] { new OmniCommand("Local") })
            .Add(c => c.SearchDebounce, 0)
            .Add(c => c.CommandSource, new Func<string, Task<IEnumerable<OmniCommand>>>(
                q => Task.FromResult<IEnumerable<OmniCommand>>(new[] { new OmniCommand($"Remoto {q}") }))));

        cut.Find(".omni-cmdk-input").Input("abc");
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll(".omni-cmdk-item-label").Select(x => x.TextContent), t => t.Contains("Remoto abc")));
    }

    // ---- MRU / recents -----------------------------------------------------

    [Fact]
    public async Task Recent_commands_surface_in_their_own_section()
    {
        var hist = Services.GetRequiredService<CommandHistoryService>();
        await hist.RecordAsync("default", "Comando B");

        var cmds = new[] { new OmniCommand("Comando A"), new OmniCommand("Comando B"), new OmniCommand("Comando C") };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));

        cut.WaitForAssertion(() =>
            Assert.Contains("Recentes", cut.FindAll(".omni-cmdk-group").Select(g => g.TextContent.Trim())));
    }

    [Fact]
    public async Task Recent_command_is_not_duplicated_under_its_category()
    {
        var hist = Services.GetRequiredService<CommandHistoryService>();
        await hist.RecordAsync("default", "Comando B");

        var cmds = new[]
        {
            new OmniCommand("Comando A"),
            new OmniCommand("Comando B") { Category = "Conta" },
            new OmniCommand("Comando C"),
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));

        cut.WaitForAssertion(() =>
            Assert.Contains("Recentes", cut.FindAll(".omni-cmdk-group").Select(g => g.TextContent.Trim())));
        var bCount = cut.FindAll(".omni-cmdk-item-label").Count(x => x.TextContent.Trim() == "Comando B");
        Assert.Equal(1, bCount);   // in Recentes, NOT also under "Conta"
    }

    [Fact]
    public async Task History_plus_category_named_like_recent_label_does_not_crash()
    {
        var hist = Services.GetRequiredService<CommandHistoryService>();
        await hist.RecordAsync("default", "X");   // X becomes recent

        var cmds = new[]
        {
            new OmniCommand("X") { Category = "Recentes" },   // recent → MRU group (label "Recentes")
            new OmniCommand("Y") { Category = "Recentes" },   // not recent → category group (also "Recentes")
        };
        // Two groups display "Recentes"; positional @key keeps the render diff valid (no crash).
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        cut.WaitForAssertion(() =>
            Assert.Contains("X", cut.FindAll(".omni-cmdk-item-label").Select(x => x.TextContent.Trim())));
        Assert.Contains("Y", cut.FindAll(".omni-cmdk-item-label").Select(x => x.TextContent.Trim()));
    }

    [Fact]
    public void Category_named_like_recent_label_renders_one_heading()
    {
        var cmds = new[]
        {
            new OmniCommand("X") { Category = "Recentes" },
            new OmniCommand("Y") { Category = "Recentes" },
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));
        var headings = cut.FindAll(".omni-cmdk-group").Count(g => g.TextContent.Trim() == "Recentes");
        Assert.Equal(1, headings);   // not fragmented into one-item groups
    }

    [Fact]
    public void Spinner_clears_when_query_is_cleared_mid_search()
    {
        var gate = new TaskCompletionSource<IEnumerable<OmniCommand>>();
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Array.Empty<OmniCommand>())
            .Add(c => c.SearchDebounce, 0)
            .Add(c => c.CommandSource, new Func<string, Task<IEnumerable<OmniCommand>>>(_ => gate.Task)));

        cut.Find(".omni-cmdk-input").Input("ab");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".omni-cmdk-spin")));   // searching…
        cut.Find(".omni-cmdk-input").Input("");                                        // clear before it resolves
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".omni-cmdk-spin")));      // spinner not stuck
    }

    [Fact]
    public void Spinner_not_stuck_when_cleared_before_debounce_elapses()
    {
        var gate = new TaskCompletionSource<IEnumerable<OmniCommand>>();
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, Array.Empty<OmniCommand>())
            .Add(c => c.SearchDebounce, 60)
            .Add(c => c.CommandSource, new Func<string, Task<IEnumerable<OmniCommand>>>(_ => gate.Task)));

        cut.Find(".omni-cmdk-input").Input("ab");   // search A enters the 60ms debounce
        cut.Find(".omni-cmdk-input").Input("");      // cleared before A's debounce elapses
        gate.SetResult(new[] { new OmniCommand("Remoto") });
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".omni-cmdk-spin")));
    }

    [Fact]
    public async Task Recents_dedupe_by_label_for_duplicate_references()
    {
        var hist = Services.GetRequiredService<CommandHistoryService>();
        await hist.RecordAsync("default", "Dup");

        var shared = new OmniCommand("Dup") { Category = "Cat" };
        var cmds = new[] { shared, shared, new OmniCommand("Other") { Category = "Cat" } };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));

        cut.WaitForAssertion(() =>
            Assert.Contains("Recentes", cut.FindAll(".omni-cmdk-group").Select(g => g.TextContent.Trim())));
        Assert.Equal(1, cut.FindAll(".omni-cmdk-item-label").Count(x => x.TextContent.Trim() == "Dup"));
    }

    [Fact]
    public async Task Recents_dedupe_two_distinct_commands_sharing_a_label()
    {
        var hist = Services.GetRequiredService<CommandHistoryService>();
        await hist.RecordAsync("default", "Same");

        var cmds = new[]
        {
            new OmniCommand("Same") { Category = "C1" },
            new OmniCommand("Same") { Category = "C2" },
            new OmniCommand("Unique") { Category = "C3" },
        };
        var cut = RenderComponent<OmniCommandPalette>(p => p
            .Add(c => c.Open, true).Add(c => c.Commands, cmds));

        cut.WaitForAssertion(() =>
            Assert.Contains("Recentes", cut.FindAll(".omni-cmdk-group").Select(g => g.TextContent.Trim())));
        Assert.Equal(1, cut.FindAll(".omni-cmdk-item-label").Count(x => x.TextContent.Trim() == "Same"));
    }
}
