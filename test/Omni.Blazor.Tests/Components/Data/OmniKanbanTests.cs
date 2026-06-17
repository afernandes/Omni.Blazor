using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniKanban{TCard}"/>: column/card
/// rendering, count + WIP badges, collapse, empty state, add-card, and the
/// drag-driven move logic (reorder within a column, move across columns,
/// WIP enforcement). DnD is simulated by firing the same DOM events the
/// browser would (dragstart/dragover/drop) — the C# move path is fully
/// exercised without a real pointer. Keyboard moves share that same path.
/// </summary>
public class OmniKanbanTests : TestContextBase
{
    private sealed class Card
    {
        public int Id { get; set; }
        public string Status { get; set; } = "todo";
        public string Lane { get; set; } = "a";
        public string Title { get; set; } = "";
    }

    private static KanbanSwimlane[] TwoLanes() => new[]
    {
        new KanbanSwimlane { Id = "a", Title = "Lane A" },
        new KanbanSwimlane { Id = "b", Title = "Lane B" },
    };

    private static KanbanColumn[] TwoCols(int? doingWip = null) => new[]
    {
        new KanbanColumn { Id = "todo", Title = "To Do" },
        new KanbanColumn { Id = "doing", Title = "Doing", WipLimit = doingWip },
    };

    private IRenderedComponent<OmniKanban<Card>> Render(
        List<Card> items,
        KanbanColumn[] cols,
        Action<ComponentParameterCollectionBuilder<OmniKanban<Card>>>? extra = null)
        => RenderComponent<OmniKanban<Card>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Columns, cols);
            p.Add(c => c.ColumnSelector, c => c.Status);
            p.Add(c => c.ColumnSetter, (c, s) => c.Status = s);
            p.Add(c => c.CardId, c => c.Id);
            p.Add(c => c.CardTitle, c => c.Title);
            extra?.Invoke(p);
        });

    [Fact]
    public void Renders_board_and_columns()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols());

        Assert.NotNull(cut.Find(".omni-kanban"));
        Assert.Equal(2, cut.FindAll(".omni-kanban-col").Count);
    }

    [Fact]
    public void Places_cards_in_their_columns()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo" },
            new() { Id = 2, Status = "doing" },
            new() { Id = 3, Status = "todo" },
        };
        var cut = Render(items, TwoCols());

        var cols = cut.FindAll(".omni-kanban-col");
        Assert.Equal(2, cols[0].QuerySelectorAll(".omni-kanban-card").Length); // todo
        Assert.Equal(1, cols[1].QuerySelectorAll(".omni-kanban-card").Length); // doing
    }

    [Fact]
    public void Count_badge_shows_count_and_wip()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo" },
            new() { Id = 2, Status = "todo" },
            new() { Id = 3, Status = "doing" },
        };
        var cut = Render(items, TwoCols(doingWip: 2));

        var counts = cut.FindAll(".omni-kanban-col-count");
        Assert.Equal("2", counts[0].TextContent);    // todo, no limit
        Assert.Equal("1/2", counts[1].TextContent);  // doing, wip 2
    }

    [Fact]
    public void Wip_over_limit_adds_modifier_class()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "doing" },
            new() { Id = 2, Status = "doing" },
            new() { Id = 3, Status = "doing" },
        };
        var cut = Render(items, TwoCols(doingWip: 2));

        var doingCount = cut.FindAll(".omni-kanban-col-count")[1];
        Assert.Equal("3/2", doingCount.TextContent);
        Assert.Contains("omni-kanban-col-count-over", doingCount.ClassName);
    }

    [Fact]
    public void Collapse_button_hides_body()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols());
        Assert.Equal(2, cut.FindAll(".omni-kanban-col-body").Count);

        cut.FindAll(".omni-kanban-col-collapse")[0].Click();

        Assert.Single(cut.FindAll(".omni-kanban-col-body"));
        Assert.Single(cut.FindAll(".omni-kanban-col-collapsed"));
    }

    [Fact]
    public void Empty_column_shows_placeholder()
    {
        var cut = Render(new() { new Card { Id = 1, Status = "todo" } }, TwoCols());

        Assert.Single(cut.FindAll(".omni-kanban-empty")); // doing is empty
    }

    [Fact]
    public void AllowAddCard_button_fires_OnAddCard()
    {
        KanbanColumn? added = null;
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.AllowAddCard, true)
            .Add(c => c.OnAddCard, EventCallback.Factory.Create<KanbanColumn>(this, c => added = c)));

        cut.FindAll(".omni-kanban-add")[0].Click();

        Assert.NotNull(added);
        Assert.Equal("todo", added!.Id);
    }

    [Fact]
    public void Card_click_fires()
    {
        Card? clicked = null;
        var cut = Render(new() { new Card { Id = 7, Status = "todo" } }, TwoCols(), p => p
            .Add(c => c.CardClick, EventCallback.Factory.Create<Card>(this, c => clicked = c)));

        cut.Find(".omni-kanban-card").Click();

        Assert.NotNull(clicked);
        Assert.Equal(7, clicked!.Id);
    }

    [Fact]
    public void DragDisabled_sets_draggable_false()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.Add(c => c.DragDisabled, true));

        Assert.Equal("false", cut.Find(".omni-kanban-card").GetAttribute("draggable"));
    }

    [Fact]
    public void ColumnWidth_sets_css_var()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.Add(c => c.ColumnWidth, "320px"));

        Assert.Contains("--omni-kanban-col-w:320px", cut.Find(".omni-kanban").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find(".omni-kanban").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.Add(c => c.Style, "height: 400px"));
        Assert.Contains("height: 400px", cut.Find(".omni-kanban").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.AddUnmatched("data-testid", "kb"));
        Assert.Equal("kb", cut.Find(".omni-kanban").GetAttribute("data-testid"));
    }

    // ── Move logic (simulated drag) ──

    [Fact]
    public void Drag_reorders_within_column()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo" },
            new() { Id = 2, Status = "todo" },
            new() { Id = 3, Status = "todo" },
        };
        IEnumerable<Card>? changed = null;
        var cut = Render(items, TwoCols(), p => p
            .Add(c => c.ItemsChanged, EventCallback.Factory.Create<IEnumerable<Card>>(this, v => changed = v)));

        // Drag card #3 over card #1 (index 0), drop on the todo column body.
        cut.FindAll(".omni-kanban-card")[2].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[0].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(changed);
        Assert.Equal(new[] { 3, 1, 2 }, changed!.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Drag_reorders_within_column_moving_down()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo" },
            new() { Id = 2, Status = "todo" },
            new() { Id = 3, Status = "todo" },
        };
        IEnumerable<Card>? changed = null;
        var cut = Render(items, TwoCols(), p => p
            .Add(c => c.ItemsChanged, EventCallback.Factory.Create<IEnumerable<Card>>(this, v => changed = v)));

        // Drag card #1 (index 0) DOWN over card #3 (index 2). Drop-line is before #3.
        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-card")[2].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[0].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(changed);
        Assert.Equal(new[] { 2, 1, 3 }, changed!.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Drag_moves_card_across_columns()
    {
        var a = new Card { Id = 1, Status = "todo" };
        var items = new List<Card> { a, new() { Id = 2, Status = "doing" } };
        KanbanCardMovedEventArgs<Card>? moved = null;
        var cut = Render(items, TwoCols(), p => p
            .Add(c => c.CardMoved, EventCallback.Factory.Create<KanbanCardMovedEventArgs<Card>>(this, e => moved = e)));

        // Drag card #1 (todo) onto the doing column's empty body area.
        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal("doing", a.Status);            // ColumnSetter persisted the move
        Assert.NotNull(moved);
        Assert.Equal("todo", moved!.FromColumn);
        Assert.Equal("doing", moved.ToColumn);
    }

    [Fact]
    public void Enforce_wip_blocks_cross_column_drop_when_full()
    {
        var a = new Card { Id = 1, Status = "todo" };
        var items = new List<Card>
        {
            a,
            new() { Id = 2, Status = "doing" },
            new() { Id = 3, Status = "doing" },
        };
        KanbanCardMovedEventArgs<Card>? moved = null;
        var cut = Render(items, TwoCols(doingWip: 2), p => p
            .Add(c => c.WipLimitMode, WipLimitMode.Enforce)
            .Add(c => c.CardMoved, EventCallback.Factory.Create<KanbanCardMovedEventArgs<Card>>(this, e => moved = e)));

        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal("todo", a.Status); // blocked — stayed in todo
        Assert.Null(moved);
    }

    [Fact]
    public void Move_without_ColumnSetter_only_raises_event()
    {
        var a = new Card { Id = 1, Status = "todo" };
        var items = new List<Card> { a, new() { Id = 2, Status = "doing" } };
        KanbanCardMovedEventArgs<Card>? moved = null;
        var cut = RenderComponent<OmniKanban<Card>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Columns, TwoCols());
            p.Add(c => c.ColumnSelector, c => c.Status);
            p.Add(c => c.CardId, c => c.Id);
            // no ColumnSetter
            p.Add(c => c.CardMoved, EventCallback.Factory.Create<KanbanCardMovedEventArgs<Card>>(this, e => moved = e));
        });

        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(moved);          // event raised so consumer can persist
        Assert.Equal("todo", a.Status); // membership unchanged (no setter)
    }

    // ── Swimlanes ──

    [Fact]
    public void Renders_swimlanes_with_header_and_cells()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo", Lane = "a" },
            new() { Id = 2, Status = "todo", Lane = "b" },
        };
        var cut = RenderComponent<OmniKanban<Card>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Columns, new[] { new KanbanColumn { Id = "todo", Title = "To Do" } });
            p.Add(c => c.ColumnSelector, c => c.Status);
            p.Add(c => c.CardId, c => c.Id);
            p.Add(c => c.CardTitle, c => c.Title);
            p.Add(c => c.Swimlanes, TwoLanes());
            p.Add(c => c.SwimlaneSelector, c => c.Lane);
        });

        Assert.Equal(2, cut.FindAll(".omni-kanban-lane").Count);
        Assert.NotNull(cut.Find(".omni-kanban-headrow"));
        Assert.Equal(2, cut.FindAll(".omni-kanban-laneslot").Count); // 2 lanes × 1 col
        Assert.Equal(2, cut.FindAll(".omni-kanban-card").Count);
    }

    [Fact]
    public void Swimlane_head_shows_lane_count()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Status = "todo", Lane = "a" },
            new() { Id = 2, Status = "todo", Lane = "a" },
            new() { Id = 3, Status = "todo", Lane = "b" },
        };
        var cut = RenderComponent<OmniKanban<Card>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Columns, new[] { new KanbanColumn { Id = "todo" } });
            p.Add(c => c.ColumnSelector, c => c.Status);
            p.Add(c => c.CardId, c => c.Id);
            p.Add(c => c.Swimlanes, TwoLanes());
            p.Add(c => c.SwimlaneSelector, c => c.Lane);
        });

        var counts = cut.FindAll(".omni-kanban-lane-head .omni-kanban-col-count");
        Assert.Equal("2", counts[0].TextContent);
        Assert.Equal("1", counts[1].TextContent);
    }

    [Fact]
    public void Drag_moves_card_across_swimlanes()
    {
        var a = new Card { Id = 1, Status = "todo", Lane = "a" };
        var items = new List<Card> { a, new() { Id = 2, Status = "todo", Lane = "b" } };
        KanbanCardMovedEventArgs<Card>? moved = null;
        var cut = RenderComponent<OmniKanban<Card>>(p =>
        {
            p.Add(c => c.Items, items);
            p.Add(c => c.Columns, new[] { new KanbanColumn { Id = "todo" } });
            p.Add(c => c.ColumnSelector, c => c.Status);
            p.Add(c => c.CardId, c => c.Id);
            p.Add(c => c.Swimlanes, TwoLanes());
            p.Add(c => c.SwimlaneSelector, c => c.Lane);
            p.Add(c => c.SwimlaneSetter, (c, l) => c.Lane = l);
            p.Add(c => c.CardMoved, EventCallback.Factory.Create<KanbanCardMovedEventArgs<Card>>(this, e => moved = e));
        });

        // Drag card #1 (lane a) onto lane b's todo cell.
        cut.FindAll(".omni-kanban-card")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal("b", a.Lane);
        Assert.NotNull(moved);
        Assert.Equal("a", moved!.FromSwimlane);
        Assert.Equal("b", moved.ToSwimlane);
    }

    // ── Card aging ──

    [Theory]
    [InlineData(5, "omni-kanban-card-age-stale")]
    [InlineData(3, "omni-kanban-card-age-warn")]
    public void Card_age_renders_indicator_with_level(int days, string levelClass)
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.ShowCardAge, true)
            .Add(c => c.CardAge, c => (int?)days));

        Assert.Single(cut.FindAll($".{levelClass}"));
        Assert.True(cut.FindAll(".omni-kanban-age-dot").Count >= 1);
    }

    [Fact]
    public void Card_age_hidden_when_null()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.ShowCardAge, true)
            .Add(c => c.CardAge, c => (int?)null));

        Assert.Empty(cut.FindAll(".omni-kanban-card-age"));
    }

    // ── Card color ──

    [Fact]
    public void Card_color_adds_accent_class_and_var()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardColor, c => "#ff0000"));

        var card = cut.Find(".omni-kanban-card");
        Assert.Contains("omni-kanban-card-accented", card.ClassName);
        Assert.Contains("--omni-kanban-card-color:#ff0000", card.GetAttribute("style") ?? "");
    }

    // ── Quick filters + search ──

    [Fact]
    public void Quick_filter_hides_non_matching_cards()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Title = "Bug login" },
            new() { Id = 2, Title = "Task X" },
        };
        var filters = new[]
        {
            new KanbanQuickFilter<Card> { Label = "Bugs", Predicate = c => c.Title.Contains("Bug") },
        };
        var cut = Render(items, TwoCols(), p => p.Add(c => c.QuickFilters, filters));

        Assert.Equal(2, cut.FindAll(".omni-kanban-card").Count);
        cut.FindAll(".omni-kanban-chip")[0].Click();
        Assert.Single(cut.FindAll(".omni-kanban-card"));
    }

    [Fact]
    public void Search_filters_cards_by_title()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Title = "Bug login" },
            new() { Id = 2, Title = "Task X" },
        };
        var cut = Render(items, TwoCols(), p => p.Add(c => c.ShowSearch, true));

        cut.Find(".omni-kanban-search-input").Input("Bug");

        Assert.Single(cut.FindAll(".omni-kanban-card"));
    }

    [Fact]
    public void Quick_filter_active_state_survives_instance_recreation()
    {
        var items = new List<Card>
        {
            new() { Id = 1, Title = "Bug login" },
            new() { Id = 2, Title = "Task X" },
        };
        var cut = Render(items, TwoCols(), p => p.Add(c => c.QuickFilters,
            new[] { new KanbanQuickFilter<Card> { Label = "Bugs", Predicate = c => c.Title.Contains("Bug") } }));

        cut.FindAll(".omni-kanban-chip")[0].Click();
        Assert.Single(cut.FindAll(".omni-kanban-card"));

        // Re-render with a brand-new filter instance carrying the same Label —
        // the active state must persist (equality by Label, not reference).
        cut.SetParametersAndRender(p => p.Add(c => c.QuickFilters,
            new[] { new KanbanQuickFilter<Card> { Label = "Bugs", Predicate = c => c.Title.Contains("Bug") } }));

        Assert.Single(cut.FindAll(".omni-kanban-card"));
        Assert.Contains("omni-kanban-chip-active", cut.Find(".omni-kanban-chip").ClassName);
    }

    // ── Layout do card padrão (campos estruturados) ──

    [Fact]
    public void Default_card_has_no_meta_without_selectors()
    {
        var cut = Render(new() { new Card { Id = 1, Title = "X" } }, TwoCols());

        Assert.Contains("X", cut.Find(".omni-kanban-card-title").TextContent);
        Assert.Empty(cut.FindAll(".omni-kanban-card-meta"));
        Assert.Empty(cut.FindAll(".omni-kanban-card-prio"));
    }

    [Fact]
    public void Card_assignee_renders_avatar()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardAssignee, c => "Ana Souza"));

        Assert.NotNull(cut.Find(".omni-kanban-card-meta .omni-avatar"));
        Assert.Contains("AS", cut.Find(".omni-avatar").TextContent); // initials
    }

    [Theory]
    [InlineData(KanbanPriority.Urgent, "omni-kanban-card-prio-urgent")]
    [InlineData(KanbanPriority.Low, "omni-kanban-card-prio-low")]
    public void Card_priority_renders_indicator(KanbanPriority prio, string cls)
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardPriority, c => prio));

        Assert.Single(cut.FindAll($".{cls}"));
    }

    [Fact]
    public void Card_priority_none_renders_nothing()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardPriority, c => KanbanPriority.None));

        Assert.Empty(cut.FindAll(".omni-kanban-card-prio"));
    }

    [Fact]
    public void Card_due_date_overdue_adds_danger()
    {
        var overdue = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardDueDate, c => (DateTime?)new DateTime(2000, 1, 1)));
        Assert.Single(overdue.FindAll(".omni-kanban-card-chip-danger"));

        var future = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardDueDate, c => (DateTime?)new DateTime(2999, 1, 1)));
        Assert.Empty(future.FindAll(".omni-kanban-card-chip-danger"));
        Assert.NotEmpty(future.FindAll(".omni-kanban-card-chip")); // chip still rendered
    }

    [Fact]
    public void Card_subtasks_render_progress()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardSubtasks, c => ((int, int)?)(2, 5)));

        Assert.Contains("2/5", cut.Find(".omni-kanban-card-meta").TextContent);
    }

    [Fact]
    public void Card_estimate_renders()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardEstimate, c => "8"));

        Assert.Contains("8", cut.Find(".omni-kanban-card-meta").TextContent);
    }

    [Fact]
    public void Card_fields_render_capped_at_three()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardFields, c => new[]
            {
                new KanbanField { Label = "A", Value = "1" },
                new KanbanField { Label = "B", Value = "2" },
                new KanbanField { Label = "C", Value = "3" },
                new KanbanField { Label = "D", Value = "4" },
            }));

        Assert.Equal(3, cut.FindAll(".omni-kanban-card-field").Count);
    }

    [Fact]
    public void Card_fields_with_duplicate_labels_both_render()
    {
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardFields, c => new[]
            {
                new KanbanField { Label = "Tag", Value = "A" },
                new KanbanField { Label = "Tag", Value = "B" },
            }));

        Assert.Equal(2, cut.FindAll(".omni-kanban-card-field").Count); // index keys keep both
    }

    // ── Menu de ações ("…") ──

    [Fact]
    public void Card_menu_button_shown_only_with_CardActions()
    {
        var without = Render(new() { new Card { Id = 1 } }, TwoCols());
        Assert.Empty(without.FindAll(".omni-kanban-card-menu"));

        var with = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardActions, c => new[] { new KanbanCardAction { Id = "x", Label = "X" } }));
        Assert.NotNull(with.Find(".omni-kanban-card-menu"));
    }

    [Fact]
    public void Clicking_menu_opens_context_menu_with_items()
    {
        var ctx = Services.GetRequiredService<ContextMenuService>();
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardActions, c => new[]
            {
                new KanbanCardAction { Id = "assign", Label = "Atribuir" },
                KanbanCardAction.Separator(),
                new KanbanCardAction { Id = "delete", Label = "Excluir", Danger = true },
            }));

        cut.Find(".omni-kanban-card-menu").Click();

        Assert.True(ctx.IsOpen);
        Assert.Equal(3, ctx.Items.Count);
        Assert.Equal("Atribuir", ctx.Items[0].Text);
        Assert.True(ctx.Items[1].IsSeparator);
        Assert.True(ctx.Items[2].IsDanger);
    }

    [Fact]
    public async Task Selecting_menu_item_raises_CardAction()
    {
        var ctx = Services.GetRequiredService<ContextMenuService>();
        KanbanCardActionEventArgs<Card>? raised = null;
        var card = new Card { Id = 7 };
        var cut = Render(new List<Card> { card }, TwoCols(), p => p
            .Add(c => c.CardActions, c => new[] { new KanbanCardAction { Id = "assign", Label = "Atribuir" } })
            .Add(c => c.CardAction, EventCallback.Factory.Create<KanbanCardActionEventArgs<Card>>(this, e => raised = e)));

        cut.Find(".omni-kanban-card-menu").Click();
        await ctx.Items[0].OnClick!.Invoke(); // what OmniContextMenuHost does on selection

        Assert.NotNull(raised);
        Assert.Equal(7, raised!.Card.Id);
        Assert.Equal("assign", raised.Action.Id);
    }

    [Fact]
    public void Empty_actions_list_does_not_open_menu()
    {
        var ctx = Services.GetRequiredService<ContextMenuService>();
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.CardActions, c => Array.Empty<KanbanCardAction>()));

        cut.Find(".omni-kanban-card-menu").Click();

        Assert.False(ctx.IsOpen);
    }

    // ── Reordenar colunas ──

    [Fact]
    public void AllowColumnReorder_makes_header_draggable_with_grip()
    {
        var with = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p.Add(c => c.AllowColumnReorder, true));
        Assert.Equal("true", with.Find(".omni-kanban-col-head").GetAttribute("draggable"));
        Assert.NotEmpty(with.FindAll(".omni-kanban-col-grip"));

        var without = Render(new() { new Card { Id = 1 } }, TwoCols());
        Assert.Equal("false", without.Find(".omni-kanban-col-head").GetAttribute("draggable"));
        Assert.Empty(without.FindAll(".omni-kanban-col-grip"));
    }

    [Fact]
    public void Drag_reorders_columns()
    {
        IEnumerable<KanbanColumn>? changed = null;
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.AllowColumnReorder, true)
            .Add(c => c.ColumnsChanged, EventCallback.Factory.Create<IEnumerable<KanbanColumn>>(this, v => changed = v)));

        // Drag "todo" header onto "doing" header.
        cut.FindAll(".omni-kanban-col-head")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(changed);
        Assert.Equal(new[] { "doing", "todo" }, changed!.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Column_reorder_raises_ColumnMoved()
    {
        KanbanColumnMovedEventArgs? moved = null;
        var cut = Render(new() { new Card { Id = 1 } }, TwoCols(), p => p
            .Add(c => c.AllowColumnReorder, true)
            .Add(c => c.ColumnMoved, EventCallback.Factory.Create<KanbanColumnMovedEventArgs>(this, e => moved = e)));

        cut.FindAll(".omni-kanban-col-head")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[1].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[1].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(moved);
        Assert.Equal("todo", moved!.Column.Id);
        Assert.Equal(0, moved.OldIndex);
        Assert.Equal(1, moved.NewIndex);
    }

    [Fact]
    public void Drag_reorders_columns_right_to_left()
    {
        IEnumerable<KanbanColumn>? changed = null;
        var cols = new[]
        {
            new KanbanColumn { Id = "a" },
            new KanbanColumn { Id = "b" },
            new KanbanColumn { Id = "c" },
        };
        var cut = Render(new() { new Card { Id = 1 } }, cols, p => p
            .Add(c => c.AllowColumnReorder, true)
            .Add(c => c.ColumnsChanged, EventCallback.Factory.Create<IEnumerable<KanbanColumn>>(this, v => changed = v)));

        // Drag "c" (index 2) onto "a" (index 0) — right to left.
        cut.FindAll(".omni-kanban-col-head")[2].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[0].TriggerEvent("ondragover", new DragEventArgs());
        cut.FindAll(".omni-kanban-col-head")[0].TriggerEvent("ondrop", new DragEventArgs());

        Assert.NotNull(changed);
        Assert.Equal(new[] { "c", "a", "b" }, changed!.Select(c => c.Id).ToArray());
    }
}
