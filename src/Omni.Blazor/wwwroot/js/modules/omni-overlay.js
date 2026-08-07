// Omni.Blazor overlay and floating UI services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

ns.viewportHeight = function () {
  return window.innerHeight || document.documentElement.clientHeight || 0;
};

  // Lightweight click-outside dispatcher.
  // The Blazor component registers a DotNet object reference and a CSS selector.
  // Dispatch outside notifications only after the complete click. Closing on
  // mousedown can synchronously re-render WebAssembly components and remove the
  // clicked control before its click handler runs (for example a form Save button).
  const outsideRegistry = new Map();
  document.addEventListener('click', function (e) {
    const pending = [];
    outsideRegistry.forEach(function (entry, key) {
      const target = entry.selector ? document.querySelector(entry.selector) : entry.el;
      if (target && !target.contains(e.target)) {
        pending.push({ key: key, entry: entry });
      }
    });

    if (pending.length === 0) return;
    setTimeout(function () {
      pending.forEach(function (item) {
        if (outsideRegistry.get(item.key) !== item.entry) return;
        item.entry.dotnet.invokeMethodAsync(item.entry.method || 'OnClickOutside').catch(() => {});
      });
    }, 0);
  }, true);

  ns.registerClickOutside = function (key, selector, dotnet, method) {
    outsideRegistry.set(key, { selector, dotnet, method });
  };
  ns.unregisterClickOutside = function (key) {
    outsideRegistry.delete(key);
  };

  // ESC key dispatcher
  const escRegistry = new Map();
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      escRegistry.forEach(function (entry) {
        entry.dotnet.invokeMethodAsync(entry.method || 'OnEsc').catch(() => {});
      });
    }
  });
  ns.registerEsc = function (key, dotnet, method) { escRegistry.set(key, { dotnet, method }); };
  ns.unregisterEsc = function (key) { escRegistry.delete(key); };

  // Floating popovers backed by the browser top layer. The element remains in
  // its original DOM position (important because Blazor owns it), while the
  // Popover API lets it escape overflow:hidden/auto ancestors such as dialog
  // and drawer bodies. One shared registry owns resize/scroll/ResizeObserver
  // lifetimes for every date and date-range picker in the current document.
  const floatingPopoverRegistry = new Map();
  let floatingPopoverListenersAttached = false;
  let floatingPopoverFrame = 0;

  function floatingPopoverIsOpen(element) {
    try { return element.matches(':popover-open'); }
    catch { return false; }
  }

  function positionFloatingPopover(entry) {
    const anchor = entry.anchor;
    const floating = entry.floating;
    if (!anchor?.isConnected || !floating?.isConnected) return;

    const margin = 8;
    const gap = Math.max(0, Number(entry.gap) || 0);
    const viewportWidth = document.documentElement.clientWidth;
    const viewportHeight = document.documentElement.clientHeight;
    const maxWidth = Math.max(0, viewportWidth - margin * 2);
    const maxHeight = Math.max(0, viewportHeight - margin * 2);

    floating.style.maxWidth = maxWidth + 'px';
    floating.style.maxHeight = maxHeight + 'px';
    floating.style.overflow = 'auto';
    floating.style.inset = 'auto';
    floating.style.margin = '0';
    floating.style.left = '0px';
    floating.style.top = '0px';

    const anchorRect = anchor.getBoundingClientRect();
    const floatingRect = floating.getBoundingClientRect();
    const direction = getComputedStyle(anchor).direction;

    let left = direction === 'rtl'
      ? anchorRect.right - floatingRect.width
      : anchorRect.left;
    left = Math.max(margin, Math.min(left, viewportWidth - floatingRect.width - margin));

    const below = anchorRect.bottom + gap;
    const above = anchorRect.top - gap - floatingRect.height;
    let top;
    if (below + floatingRect.height <= viewportHeight - margin) {
      top = below;
    } else if (above >= margin) {
      top = above;
    } else {
      // Neither side fits completely. The max-height above guarantees the
      // surface itself remains visible; clamp it to the viewport and let only
      // the popover body scroll instead of clipping it at an ancestor.
      top = Math.max(margin, Math.min(below, viewportHeight - floatingRect.height - margin));
    }

    floating.style.left = Math.round(left) + 'px';
    floating.style.top = Math.round(top) + 'px';
  }

  function reflowFloatingPopovers() {
    floatingPopoverFrame = 0;
    floatingPopoverRegistry.forEach(positionFloatingPopover);
  }

  function scheduleFloatingPopoverReflow() {
    if (floatingPopoverFrame) return;
    floatingPopoverFrame = requestAnimationFrame(reflowFloatingPopovers);
  }

  function attachFloatingPopoverListeners() {
    if (floatingPopoverListenersAttached) return;
    window.addEventListener('resize', scheduleFloatingPopoverReflow, { passive: true });
    window.addEventListener('scroll', scheduleFloatingPopoverReflow, { passive: true, capture: true });
    floatingPopoverListenersAttached = true;
  }

  function detachFloatingPopoverListeners() {
    if (!floatingPopoverListenersAttached || floatingPopoverRegistry.size !== 0) return;
    window.removeEventListener('resize', scheduleFloatingPopoverReflow);
    window.removeEventListener('scroll', scheduleFloatingPopoverReflow, { capture: true });
    floatingPopoverListenersAttached = false;
    if (floatingPopoverFrame) cancelAnimationFrame(floatingPopoverFrame);
    floatingPopoverFrame = 0;
  }

  ns.floatingPopoverOpen = function (anchor, floating, gap, autofocusSelector) {
    if (!anchor || !floating) return false;
    ns.floatingPopoverClose(floating);

    floating.style.visibility = 'hidden';
    if (typeof floating.showPopover === 'function' && !floatingPopoverIsOpen(floating)) {
      floating.showPopover();
    }

    const entry = { anchor, floating, gap, observer: null };
    if (typeof ResizeObserver === 'function') {
      entry.observer = new ResizeObserver(scheduleFloatingPopoverReflow);
      entry.observer.observe(anchor);
      entry.observer.observe(floating);
    }
    floatingPopoverRegistry.set(floating, entry);
    attachFloatingPopoverListeners();
    positionFloatingPopover(entry);
    floating.style.visibility = 'visible';

    if (autofocusSelector) {
      const focusTarget = floating.querySelector(autofocusSelector);
      if (focusTarget) {
        try { focusTarget.focus({ preventScroll: true }); }
        catch { try { focusTarget.focus(); } catch { } }
      }
    }
    return true;
  };

  ns.floatingPopoverClose = function (floating) {
    if (!floating) return;
    const entry = floatingPopoverRegistry.get(floating);
    if (entry?.observer) entry.observer.disconnect();
    floatingPopoverRegistry.delete(floating);
    if (typeof floating.hidePopover === 'function' && floatingPopoverIsOpen(floating)) {
      try { floating.hidePopover(); } catch { }
    }
    detachFloatingPopoverListeners();
  };

  // Flyout viewport-flip helper — keeps nested submenus inside the window.
  // Delegated pointerover on a root container: when entering a .omni-menubar-sub,
  // measure its direct-child .omni-menubar-flyout and toggle `.omni-flip-inline` on
  // the wrapper if the panel would overflow the right edge. Mirrors Radzen's
  // data-flyout-flip / Metronic's data-kt-menu-flip with progressive enhancement:
  // the flyout works without it, just may clip near the right edge of the viewport.
  const flipRegistry = new Map(); // key -> { root, handler }
  function flipMeasure(sub) {
    if (!sub || !sub.classList || !sub.classList.contains('omni-menubar-sub')) return;
    const flyout = sub.querySelector(':scope > .omni-menubar-flyout');
    if (!flyout) return;
    // Temporarily clear the flip so we measure the "natural" right-opening position
    const wasFlipped = sub.classList.contains('omni-flip-inline');
    if (wasFlipped) sub.classList.remove('omni-flip-inline');
    const r = flyout.getBoundingClientRect();
    const vw = window.innerWidth;
    const margin = 8;
    const overflowsRight = r.right > vw - margin;
    if (overflowsRight) sub.classList.add('omni-flip-inline');
    else if (wasFlipped) sub.classList.remove('omni-flip-inline');
  }
  ns.registerFlyoutFlip = function (key, rootSelector) {
    const root = typeof rootSelector === 'string' ? document.querySelector(rootSelector) : rootSelector;
    if (!root) return;
    // Avoid duplicate registration
    if (flipRegistry.has(key)) ns.unregisterFlyoutFlip(key);
    const handler = function (e) {
      const sub = e.target && e.target.closest && e.target.closest('.omni-menubar-sub');
      if (sub && root.contains(sub)) flipMeasure(sub);
    };
    root.addEventListener('pointerover', handler, true);
    // Also re-measure on focus-within (keyboard nav)
    const focusHandler = function (e) {
      const sub = e.target && e.target.closest && e.target.closest('.omni-menubar-sub');
      if (sub && root.contains(sub)) flipMeasure(sub);
    };
    root.addEventListener('focusin', focusHandler, true);
    flipRegistry.set(key, { root, handler, focusHandler });
  };
  ns.unregisterFlyoutFlip = function (key) {
    const entry = flipRegistry.get(key);
    if (!entry) return;
    try { entry.root.removeEventListener('pointerover', entry.handler, true); } catch {}
    try { entry.root.removeEventListener('focusin', entry.focusHandler, true); } catch {}
    flipRegistry.delete(key);
  };

  // ─── Panel-menu collapsed-mode flyout ───
  // When a OmniPanelMenu is rendered inside a collapsed (icon-only) OmniSidebar,
  // items with children expose `data-pm-trigger="1"` and ship a sibling
  // `.omni-panel-menu-flyout` element (initially hidden via CSS). This helper:
  //   • Measures the trigger via getBoundingClientRect() and positions the
  //     flyout as `position:fixed` so it escapes the sidebar's overflow:auto.
  //   • Toggles `.omni-flyout-open` on the trigger wrapper to drive the CSS
  //     reveal (visibility + opacity transition).
  //   • Honours a ~220 ms close grace via setTimeout — re-entering the trigger
  //     or its flyout cancels the close (handles the diagonal-traversal gap
  //     mature menu libs solve with their CancellationToken/timer pattern).
  //   • Snaps instantly to a different trigger when the user moves between
  //     siblings (replicates MudBlazor's "transient" behavior).
  //   • Closes on Escape / scroll of the host / window resize.
  const pmRegistry = new Map(); // key -> { root, handlers, timer, open }
  function pmPosition(trigger, flyout) {
    const r = trigger.getBoundingClientRect();
    const margin = 8;
    flyout.style.visibility = 'hidden';     // measure unbiased
    flyout.style.display = 'block';
    const fw = flyout.offsetWidth || 280;
    const fh = flyout.offsetHeight || 240;
    flyout.style.display = '';
    flyout.style.visibility = '';
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    let left = r.right + margin;
    if (left + fw > vw - 8) left = Math.max(8, r.left - margin - fw); // flip
    let top = r.top;
    if (top + fh > vh - 8) top = Math.max(8, vh - fh - 8);
    flyout.style.left = left + 'px';
    flyout.style.top = top + 'px';
  }
  function pmOpen(entry, trigger) {
    const flyout = trigger.querySelector(':scope > .omni-panel-menu-flyout');
    if (!flyout) return;
    if (entry.open && entry.open !== trigger) {
      entry.open.classList.remove('omni-flyout-open');
    }
    clearTimeout(entry.timer);
    entry.timer = null;
    pmPosition(trigger, flyout);
    trigger.classList.add('omni-flyout-open');
    entry.open = trigger;
  }
  function pmScheduleClose(entry) {
    clearTimeout(entry.timer);
    entry.timer = setTimeout(() => {
      if (entry.open) entry.open.classList.remove('omni-flyout-open');
      entry.open = null;
      entry.timer = null;
    }, 220);
  }
  function pmCancelClose(entry) {
    if (entry.timer) { clearTimeout(entry.timer); entry.timer = null; }
  }
  function pmCloseNow(entry) {
    clearTimeout(entry.timer);
    if (entry.open) entry.open.classList.remove('omni-flyout-open');
    entry.open = null;
    entry.timer = null;
  }
  ns.registerPanelMenuFlyout = function (key, rootSelector) {
    const root = typeof rootSelector === 'string' ? document.querySelector(rootSelector) : rootSelector;
    if (!root) return;
    if (pmRegistry.has(key)) ns.unregisterPanelMenuFlyout(key);
    const entry = { root, open: null, timer: null };

    const onOver = function (e) {
      const trigger = e.target.closest && e.target.closest('.omni-panel-menu-item-wrap[data-pm-trigger="1"]');
      if (trigger && root.contains(trigger)) {
        pmOpen(entry, trigger);
        return;
      }
      // Pointer moved over a flyout — cancel any pending close.
      const flyout = e.target.closest && e.target.closest('.omni-panel-menu-flyout');
      if (flyout && root.contains(flyout)) {
        pmCancelClose(entry);
      }
    };
    const onOut = function (e) {
      // Only react when leaving the currently-open trigger subtree (trigger + its flyout)
      if (!entry.open) return;
      const to = e.relatedTarget;
      const within = to && (entry.open.contains(to) ||
                            (entry.open.querySelector(':scope > .omni-panel-menu-flyout') &&
                             entry.open.querySelector(':scope > .omni-panel-menu-flyout').contains(to)));
      if (!within) pmScheduleClose(entry);
    };
    const onKey = function (e) { if (e.key === 'Escape') pmCloseNow(entry); };
    const onScroll = function () { pmCloseNow(entry); };
    const onResize = function () {
      if (!entry.open) return;
      const flyout = entry.open.querySelector(':scope > .omni-panel-menu-flyout');
      if (flyout) pmPosition(entry.open, flyout);
    };

    root.addEventListener('pointerover', onOver, true);
    root.addEventListener('pointerout', onOut, true);
    document.addEventListener('keydown', onKey);
    root.addEventListener('scroll', onScroll, true);
    window.addEventListener('resize', onResize);

    entry.handlers = { onOver, onOut, onKey, onScroll, onResize };
    pmRegistry.set(key, entry);
  };
  ns.unregisterPanelMenuFlyout = function (key) {
    const entry = pmRegistry.get(key);
    if (!entry) return;
    pmCloseNow(entry);
    try { entry.root.removeEventListener('pointerover', entry.handlers.onOver, true); } catch {}
    try { entry.root.removeEventListener('pointerout', entry.handlers.onOut, true); } catch {}
    try { document.removeEventListener('keydown', entry.handlers.onKey); } catch {}
    try { entry.root.removeEventListener('scroll', entry.handlers.onScroll, true); } catch {}
    try { window.removeEventListener('resize', entry.handlers.onResize); } catch {}
    pmRegistry.delete(key);
  };


  // Smart-position a floating element near (x, y).
  // Measures the element after render and flips/clamps so it stays inside the
  // viewport. Mirrors Radzen's Radzen.openPopup smart-position algorithm:
  //   - if right edge overflows, slide left so it fits
  //   - if bottom edge overflows AND there's room above the click, flip upward
  //   - else clamp to viewport with a small margin
  ns.positionContextMenu = function (selector, x, y) {
    const el = document.querySelector(selector);
    if (!el) return;
    el.style.visibility = 'hidden';
    el.style.display = 'block';
    el.style.left = '0px';
    el.style.top = '0px';
    const rect = el.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const margin = 4;

    let left = x;
    if (left + rect.width > vw - margin) left = Math.max(margin, vw - rect.width - margin);

    let top = y;
    if (top + rect.height > vh - margin) {
      if (y - rect.height > margin) top = y - rect.height;
      else top = Math.max(margin, vh - rect.height - margin);
    }

    el.style.left = left + 'px';
    el.style.top = top + 'px';
    el.style.visibility = 'visible';
  };

  // Context-menu lifecycle owned by one portal host. Unlike the generic
  // outside/Escape registries this also handles trigger anchoring, roving focus,
  // initial focus and focus restoration as one deterministic unit.
  const contextMenuRegistry = new Map();

  function contextMenuItems(root) {
    return Array.from(root.querySelectorAll('[role="menuitem"]:not([disabled])'));
  }

  function contextMenuFocus(root, edgeOrDelta) {
    const items = contextMenuItems(root);
    if (!items.length) return;
    const current = items.indexOf(document.activeElement);
    let next;
    if (edgeOrDelta === 'first') next = 0;
    else if (edgeOrDelta === 'last') next = items.length - 1;
    else next = current < 0
      ? (edgeOrDelta > 0 ? 0 : items.length - 1)
      : (current + edgeOrDelta + items.length) % items.length;
    try { items[next].focus({ preventScroll: true }); } catch { items[next].focus(); }
  }

  function contextMenuOpener(entry) {
    if (entry.opener && entry.opener.isConnected) return entry.opener;
    if (!entry.key) return null;
    try { return document.querySelector('[aria-controls="' + CSS.escape(entry.key) + '"]'); }
    catch { return null; }
  }

  ns.contextMenuClose = function (key, restoreFocus) {
    const entry = contextMenuRegistry.get(key);
    if (!entry) return;
    document.removeEventListener('mousedown', entry.onPointerDown, true);
    entry.root.removeEventListener('keydown', entry.onKeyDown, true);
    if (entry.focusTimers) entry.focusTimers.forEach(clearTimeout);
    contextMenuRegistry.delete(key);
    const opener = contextMenuOpener(entry);
    if (opener) opener.setAttribute('aria-expanded', 'false');
    if (restoreFocus && opener && typeof opener.focus === 'function') {
      try { opener.focus({ preventScroll: true }); } catch { opener.focus(); }
    }
  };

  ns.contextMenuOpen = function (key, root, x, y, anchored, alignEnd, dotnet) {
    if (!key || !root) return;
    ns.contextMenuClose(key, false);

    const active = document.activeElement;
    const opener = anchored && active && active !== document.body && !root.contains(active) ? active : null;
    if (opener) opener.setAttribute('aria-expanded', 'true');
    root.style.visibility = 'hidden';
    root.style.display = 'block';
    root.style.left = '0px';
    root.style.top = '0px';
    const menuRect = root.getBoundingClientRect();
    const margin = 4;
    const gap = 4;
    let left = x;
    let top = y;

    if (opener) {
      const triggerRect = opener.getBoundingClientRect();
      left = alignEnd ? triggerRect.right - menuRect.width : triggerRect.left;
      top = triggerRect.bottom + gap;
      if (top + menuRect.height > window.innerHeight - margin && triggerRect.top - gap - menuRect.height >= margin) {
        top = triggerRect.top - gap - menuRect.height;
      }
    } else if (top + menuRect.height > window.innerHeight - margin && y - menuRect.height >= margin) {
      top = y - menuRect.height;
    }

    left = Math.min(Math.max(margin, left), Math.max(margin, window.innerWidth - menuRect.width - margin));
    top = Math.min(Math.max(margin, top), Math.max(margin, window.innerHeight - menuRect.height - margin));
    root.style.left = left + 'px';
    root.style.top = top + 'px';
    root.style.visibility = 'visible';

    const invoke = function (method) {
      if (dotnet) dotnet.invokeMethodAsync(method).catch(() => {});
    };
    const entry = { key, root, opener, onPointerDown: null, onKeyDown: null, focusTimers: [] };
    const onPointerDown = function (event) {
      const currentOpener = contextMenuOpener(entry);
      if (!root.contains(event.target) && (!currentOpener || !currentOpener.contains(event.target))) invoke('OnClickOutside');
    };
    const onKeyDown = function (event) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        event.stopPropagation();
        contextMenuFocus(root, event.key === 'ArrowDown' ? 1 : -1);
      } else if (event.key === 'Home' || event.key === 'End') {
        event.preventDefault();
        event.stopPropagation();
        contextMenuFocus(root, event.key === 'Home' ? 'first' : 'last');
      } else if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        invoke('OnEsc');
      } else if (event.key === 'Tab') {
        invoke('OnTab');
      }
    };

    document.addEventListener('mousedown', onPointerDown, true);
    root.addEventListener('keydown', onKeyDown, true);
    entry.onPointerDown = onPointerDown;
    entry.onKeyDown = onKeyDown;
    contextMenuRegistry.set(key, entry);
    const focusInitial = function () {
      if (contextMenuRegistry.get(key) === entry
          && root.isConnected
          && !root.contains(document.activeElement)) contextMenuFocus(root, 'first');
    };
    focusInitial();
    entry.focusTimers.push(setTimeout(focusInitial, 0));
    entry.focusTimers.push(setTimeout(focusInitial, 75));
    entry.focusTimers.push(setTimeout(focusInitial, 250));
  };

  // Registry de popovers abertos pra reposicionar em resize/scroll.
  // (subscribeViewport só dispara em mudança de breakpoint, que é
  // granular demais — popovers precisam reagir a QUALQUER resize.)
  const popoverRegistry = new Map(); // wrap element → true
  let popoverResizeTimer = null;
  let popoverListenerAttached = false;

  function popoverReflowAll() {
    popoverRegistry.forEach((_, wrap) => {
      if (document.body.contains(wrap)) ns.popoverAutoFlip(wrap);
    });
  }

  function popoverOnResize() {
    if (popoverResizeTimer) clearTimeout(popoverResizeTimer);
    popoverResizeTimer = setTimeout(popoverReflowAll, 60);
  }

  ns.popoverRegister = function (wrap) {
    if (!wrap) return;
    popoverRegistry.set(wrap, true);
    if (!popoverListenerAttached) {
      window.addEventListener('resize', popoverOnResize, { passive: true });
      window.addEventListener('scroll', popoverOnResize, { passive: true, capture: true });
      popoverListenerAttached = true;
    }
  };

  ns.popoverUnregister = function (wrap) {
    if (!wrap) return;
    popoverRegistry.delete(wrap);
    if (popoverRegistry.size === 0 && popoverListenerAttached) {
      window.removeEventListener('resize', popoverOnResize);
      window.removeEventListener('scroll', popoverOnResize, { capture: true });
      popoverListenerAttached = false;
      if (popoverResizeTimer) { clearTimeout(popoverResizeTimer); popoverResizeTimer = null; }
    }
  };

  // Encontra o "clipping rect" do popover — o retângulo onde ele PODE
  // aparecer sem ficar atrás de outros elementos. Sem teleport, o
  // popover é position:absolute e fica limitado ao container do
  // trigger. Walk up procurando o 1º ancestral com overflow!=visible
  // (sub-sidebars, modais com scroll, drawers). Fallback: viewport.
  // Usado pelo popoverAutoFlip ao invés do clientWidth/clientHeight.
  function popoverClippingRect(el) {
    let p = el.parentElement;
    while (p && p !== document.body && p !== document.documentElement) {
      const s = getComputedStyle(p);
      if (s.overflow !== 'visible' || s.overflowX !== 'visible' || s.overflowY !== 'visible') {
        return p.getBoundingClientRect();
      }
      p = p.parentElement;
    }
    return {
      left: 0, top: 0,
      right: document.documentElement.clientWidth,
      bottom: document.documentElement.clientHeight,
      width: document.documentElement.clientWidth,
      height: document.documentElement.clientHeight
    };
  }

  // Auto-flip + clamp para popovers já abertos. Estratégia (inspirada
  // em MudBlazor + Radzen, mas via toggle de classes — sem teleport):
  //   1. Mede o popover na posição/alinhamento pedidos pelo dev.
  //   2. Se transborda horizontalmente, inverte align-end (right↔left).
  //   3. Se transborda verticalmente, swap bottom↔top (ou left↔right).
  //   4. Re-mede. Se AINDA transborda (popover maior que viewport ou
  //      trigger encostado em 2 bordas), faz translate final como
  //      rede de segurança.
  //
  // Os limites NÃO são a viewport — são o 1º ancestral com
  // overflow!=visible (sub-sidebars, drawers). Isso evita popover ser
  // escondido por outros elementos da página.
  //
  // Chamado de OmniPopover.OnAfterRenderAsync após o popover entrar
  // no DOM. Idempotente: a primeira chamada cacheia a intenção
  // original em data-* attrs, chamadas seguintes restauram antes de
  // re-decidir (essencial para reposicionar em resize).
  ns.popoverAutoFlip = function (wrap) {
    if (!wrap) return;
    const pop = wrap.querySelector(':scope > .omni-popover');
    if (!pop) return;

    // Reset transform de chamada anterior (se houve)
    pop.style.transform = '';

    // Cacheia a intenção do dev na 1ª chamada
    if (pop.dataset.tvsAlignEndInit === undefined) {
      pop.dataset.tvsAlignEndInit = pop.classList.contains('omni-popover-align-end') ? 'true' : 'false';
    }
    if (!pop.dataset.tvsPosInit) {
      pop.dataset.tvsPosInit = pop.classList.contains('omni-popover-top') ? 'top'
        : pop.classList.contains('omni-popover-left') ? 'left'
        : pop.classList.contains('omni-popover-right') ? 'right'
        : 'bottom';
    }
    const initialAlignEnd = pop.dataset.tvsAlignEndInit === 'true';
    const initialPos = pop.dataset.tvsPosInit;

    // Restaura intenção pra medir do baseline correto
    pop.classList.toggle('omni-popover-align-end', initialAlignEnd);
    ['omni-popover-bottom','omni-popover-top','omni-popover-left','omni-popover-right']
      .forEach(c => pop.classList.remove(c));
    pop.classList.add('omni-popover-' + initialPos);

    const PAD = 8;
    const bounds = popoverClippingRect(pop);

    // ── Horizontal flip (bottom/top → flip align-end) ──────────────
    if (initialPos === 'bottom' || initialPos === 'top') {
      const rect = pop.getBoundingClientRect();
      if (initialAlignEnd && rect.left < bounds.left + PAD) {
        pop.classList.remove('omni-popover-align-end');
      } else if (!initialAlignEnd && rect.right > bounds.right - PAD) {
        pop.classList.add('omni-popover-align-end');
      }
    } else {
      // left/right positions → align-end controla vertical
      const rect = pop.getBoundingClientRect();
      if (initialAlignEnd && rect.top < bounds.top + PAD) {
        pop.classList.remove('omni-popover-align-end');
      } else if (!initialAlignEnd && rect.bottom > bounds.bottom - PAD) {
        pop.classList.add('omni-popover-align-end');
      }
    }

    // ── Vertical flip (bottom↔top, left↔right) ────────────────────
    let rect = pop.getBoundingClientRect();
    if (initialPos === 'bottom' && rect.bottom > bounds.bottom - PAD) {
      pop.classList.remove('omni-popover-bottom');
      pop.classList.add('omni-popover-top');
    } else if (initialPos === 'top' && rect.top < bounds.top + PAD) {
      pop.classList.remove('omni-popover-top');
      pop.classList.add('omni-popover-bottom');
    } else if (initialPos === 'right' && rect.right > bounds.right - PAD) {
      pop.classList.remove('omni-popover-right');
      pop.classList.add('omni-popover-left');
    } else if (initialPos === 'left' && rect.left < bounds.left + PAD) {
      pop.classList.remove('omni-popover-left');
      pop.classList.add('omni-popover-right');
    }

    // ── Clamp final via translate (rede de segurança) ──────────────
    rect = pop.getBoundingClientRect();
    let tx = 0, ty = 0;
    if (rect.left < bounds.left + PAD) tx = (bounds.left + PAD) - rect.left;
    else if (rect.right > bounds.right - PAD) tx = (bounds.right - PAD) - rect.right;
    if (rect.top < bounds.top + PAD) ty = (bounds.top + PAD) - rect.top;
    else if (rect.bottom > bounds.bottom - PAD) ty = (bounds.bottom - PAD) - rect.bottom;
    if (tx || ty) pop.style.transform = `translate(${tx}px, ${ty}px)`;
  };

  // ——— OmniTour — spotlight (recorte via box-shadow) + posicao do coachmark ———
  // Mede o alvo, escreve as CSS vars do recorte (--omni-tour-x/y/w/h) no .omni-tour-cutout,
  // e posiciona o .omni-tour-coachmark (position:fixed) no lado pedido (ou o de maior folga),
  // com clamp a viewport. `scroll`=true traz o alvo a tela antes de medir. tourRegister liga
  // listeners debounced de resize/scroll que re-medem (sem re-scrollar) seguindo o alvo.
  let _tourArgs = null;
  let _tourTimer = null;
  let _tourListening = false;

  function _tourReflow() { if (_tourArgs) ns.tourPosition(_tourArgs[0], _tourArgs[1], _tourArgs[2], false); }
  function _tourOnScrollResize() {
    if (_tourTimer) clearTimeout(_tourTimer);
    _tourTimer = setTimeout(_tourReflow, 40);
  }

  ns.tourPosition = function (target, position, pad, scroll) {
    const coach = document.querySelector('.omni-tour-coachmark');
    const cutout = document.querySelector('.omni-tour-cutout');
    const el = typeof target === 'string' ? (target ? document.querySelector(target) : null) : target;
    _tourArgs = [target, position, pad];
    pad = (pad == null) ? 6 : pad;
    const vw = window.innerWidth, vh = window.innerHeight, M = 8, GAP = 12;

    if (!el) {
      // Sem alvo: escurece tudo (recorte 0 no centro) e centraliza o coachmark.
      if (cutout) {
        cutout.style.setProperty('--omni-tour-x', (vw / 2) + 'px');
        cutout.style.setProperty('--omni-tour-y', (vh / 2) + 'px');
        cutout.style.setProperty('--omni-tour-w', '0px');
        cutout.style.setProperty('--omni-tour-h', '0px');
      }
      if (coach) {
        const cr = coach.getBoundingClientRect();
        coach.style.left = Math.max(M, vw / 2 - cr.width / 2) + 'px';
        coach.style.top = Math.max(M, vh / 2 - cr.height / 2) + 'px';
        coach.style.visibility = 'visible';
        coach.setAttribute('data-omni-tour-side', 'center');
      }
      return 'center';
    }

    if (scroll) { try { el.scrollIntoView({ block: 'center', inline: 'nearest' }); } catch (e) { /* ignore */ } }
    const r = el.getBoundingClientRect();

    if (cutout) {
      cutout.style.setProperty('--omni-tour-x', (r.left - pad) + 'px');
      cutout.style.setProperty('--omni-tour-y', (r.top - pad) + 'px');
      cutout.style.setProperty('--omni-tour-w', (r.width + pad * 2) + 'px');
      cutout.style.setProperty('--omni-tour-h', (r.height + pad * 2) + 'px');
    }

    if (!coach) return 'bottom';
    const cr = coach.getBoundingClientRect();

    let side = position || 'auto';
    if (side === 'auto') {
      const room = { top: r.top, bottom: vh - r.bottom, left: r.left, right: vw - r.right };
      side = Object.keys(room).reduce((a, b) => (room[a] >= room[b] ? a : b));
    }

    let left, top;
    if (side === 'bottom') { top = r.bottom + GAP; left = r.left + r.width / 2 - cr.width / 2; }
    else if (side === 'top') { top = r.top - GAP - cr.height; left = r.left + r.width / 2 - cr.width / 2; }
    else if (side === 'right') { left = r.right + GAP; top = r.top + r.height / 2 - cr.height / 2; }
    else { left = r.left - GAP - cr.width; top = r.top + r.height / 2 - cr.height / 2; }

    left = Math.max(M, Math.min(left, vw - cr.width - M));
    top = Math.max(M, Math.min(top, vh - cr.height - M));
    coach.style.left = left + 'px';
    coach.style.top = top + 'px';
    coach.style.visibility = 'visible';
    coach.setAttribute('data-omni-tour-side', side);
    return side;
  };

  ns.tourRegister = function () {
    if (_tourListening) return;
    window.addEventListener('resize', _tourOnScrollResize, { passive: true });
    window.addEventListener('scroll', _tourOnScrollResize, { passive: true, capture: true });
    _tourListening = true;
  };
  ns.tourUnregister = function () {
    if (!_tourListening) return;
    window.removeEventListener('resize', _tourOnScrollResize);
    window.removeEventListener('scroll', _tourOnScrollResize, { capture: true });
    _tourListening = false;
    _tourArgs = null;
    if (_tourTimer) { clearTimeout(_tourTimer); _tourTimer = null; }
  };

  // ——— Focus trap + Escape handler ————————————————————————————————————
  // Used by overlay components (OmniDrawer Temporary/Responsive-mobile, modals).
  // setupOverlay returns a token (id) you must pass back to teardownOverlay
  // when the overlay closes. Stack: multiple overlays compose — the innermost
  // owns focus, all parents have their focus state preserved for restoration.
  const overlayStack = new Map(); // id -> { el, prevFocus, keyHandler, focusHandler, onEsc }

  function focusableWithin(el) {
    if (!el) return [];
    const sel = 'a[href], area[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), ' +
                'select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]), ' +
                'audio[controls], video[controls], iframe, object, embed, [contenteditable]:not([contenteditable="false"])';
    return Array.from(el.querySelectorAll(sel))
      .filter(n => !n.hasAttribute('disabled') && n.offsetParent !== null);
  }

  // Picks the best initial focus target inside the overlay. Priority:
  //   1. Element with .omni-autofocus class (consumer's explicit pick)
  //   2. First text input / textarea / select that is not disabled/readonly
  //   3. Element with [data-omni-default] (typically the OK button)
  //   4. First focusable of any kind
  //   5. The overlay itself (so focus is contained even with no controls)
  function preferredAutofocus(el) {
    const explicit = el.querySelector('.omni-autofocus');
    if (explicit) return explicit;
    const inputs = Array.from(el.querySelectorAll('input, textarea, select'))
      .filter(n => !n.disabled && !n.readOnly && n.type !== 'hidden' && n.offsetParent !== null);
    if (inputs.length > 0) return inputs[0];
    const def = el.querySelector('[data-omni-default]');
    if (def) return def;
    const items = focusableWithin(el);
    return items[0] || el;
  }

  /// Setup an overlay with focus trap + Esc handler + smart autofocus.
  ///
  /// id      — unique token (caller-generated; component's stable Id is fine)
  /// elSel   — selector of the overlay element (drawer aside, dialog, etc.)
  /// onEsc   — optional { dotnet, method } to invoke when Esc is pressed AND
  ///           no [data-omni-cancel] button is present inside the overlay.
  ///
  /// MARKUP CONVENTIONS the overlay respects (all opt-in, all backward-compat):
  ///   • <input class="omni-autofocus" /> — element to receive initial focus
  ///   • <button data-omni-default> — Enter (outside textarea/select) clicks it
  ///   • <button data-omni-cancel>  — Esc clicks it (preferred over onEsc handler)
  ns.setupOverlay = function (id, elSel, onEsc) {
    if (!id || !elSel) return false;
    const el = (typeof elSel === 'string') ? document.querySelector(elSel) : elSel;
    if (!el) return false;
    // Already set up? tear down first (defensive).
    if (overlayStack.has(id)) ns.teardownOverlay(id);

    const prevFocus = document.activeElement;

    // Smart autofocus.
    const initial = preferredAutofocus(el);
    try { initial.focus({ preventScroll: true }); } catch {}
    // If we focused an <input>, position caret at end (better UX than select-all).
    try {
      if (initial && (initial.tagName === 'INPUT' || initial.tagName === 'TEXTAREA') && typeof initial.value === 'string') {
        const len = initial.value.length;
        initial.setSelectionRange(len, len);
      }
    } catch { /* readonly select range types throw */ }

    function trapKey(e) {
      if (e.key !== 'Tab') return;
      const items = focusableWithin(el);
      if (items.length === 0) { e.preventDefault(); return; }
      const first = items[0];
      const last  = items[items.length - 1];
      if (e.shiftKey) {
        if (document.activeElement === first || !el.contains(document.activeElement)) {
          e.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    }

    // Enter inside the overlay → click [data-omni-default] button.
    // Skipped inside <textarea> (newline) or <select> (browser dropdown nav).
    // Skipped if the focused element is itself a <button> (browser will click it
    // natively — we don't want to fire twice and clobber custom buttons).
    function enterKey(e) {
      if (e.key !== 'Enter' && e.keyCode !== 13) return;
      const target = e.target;
      const tag = target && target.tagName;
      if (tag === 'TEXTAREA' || tag === 'SELECT' || tag === 'BUTTON') return;
      // contenteditable is also user-typing context.
      if (target && target.isContentEditable) return;
      // Skip when modifier keys are held (Ctrl+Enter, Shift+Enter etc. have
      // their own conventions — e.g. Ctrl+Enter to submit form).
      if (e.altKey || e.ctrlKey || e.metaKey) return;
      // Bow out if there's a [data-omni-enter-as-tab] ancestor between the
      // target and the overlay — the global handler at the bottom of this
      // file will advance focus instead. Without this, capture-phase
      // stopPropagation() would block the global Enter-as-Tab handler.
      if (target && target.closest &&
          target.closest('[data-omni-enter-as-tab]:not([data-omni-enter-as-tab="false"])')) return;
      const btn = el.querySelector('[data-omni-default]:not([disabled])');
      if (!btn) return;
      e.preventDefault();
      e.stopPropagation();
      try { btn.click(); } catch {}
    }

    // Esc → prefer clicking [data-omni-cancel] if present (richer semantic
    // than just "close"); otherwise invoke the onEsc callback. Without either,
    // Esc bubbles up so other listeners can handle it.
    function escKey(e) {
      if (e.key !== 'Escape' && e.key !== 'Esc') return;
      const cancelBtn = el.querySelector('[data-omni-cancel]:not([disabled])');
      if (cancelBtn) {
        e.preventDefault();
        e.stopPropagation();
        try { cancelBtn.click(); } catch {}
        return;
      }
      if (!onEsc || !onEsc.dotnet) return;
      e.preventDefault();
      e.stopPropagation();
      try { onEsc.dotnet.invokeMethodAsync(onEsc.method || 'OnEscape'); }
      catch { /* circuit gone */ }
    }

    // Stack-aware: quando há múltiplos overlays empilhados (ex.: Dialog em cima
    // de Drawer, confirm em cima de Dialog), TODOS receberiam o keydown na ordem
    // que foram registrados no document — e cada um chamaria seu OnEscape, fechando
    // tudo de uma vez. Solução: só o TOPMOST processa keys. overlayStack é Map e
    // mantém ordem de inserção — último adicionado é o topo. Quando o topmost
    // fecha (teardownOverlay remove), o próximo Esc é processado pelo que ficou.
    function isTopmost() {
      const keys = Array.from(overlayStack.keys());
      return keys.length > 0 && keys[keys.length - 1] === id;
    }

    // Combine into one keydown handler on document.
    function keyHandler(e) {
      if (!isTopmost()) return;
      trapKey(e); enterKey(e); escKey(e);
    }
    document.addEventListener('keydown', keyHandler, true);

    // If focus tries to escape (e.g. via programmatic focus elsewhere),
    // pull it back into the overlay. Topmost-only — overlays underneath
    // ficam "frozen" focus-wise, focus trap é exclusivo do topo.
    function focusHandler(e) {
      if (!isTopmost()) return;
      if (!el.contains(e.target)) {
        const items = focusableWithin(el);
        const target = items[0] || el;
        try { target.focus({ preventScroll: true }); } catch {}
      }
    }
    document.addEventListener('focusin', focusHandler, true);

    overlayStack.set(id, { el, prevFocus, keyHandler, focusHandler });
    return true;
  };

  /// Teardown the overlay set up via setupOverlay. Restores focus to the
  /// element that had it before the overlay opened (typically the hamburger).
  ns.teardownOverlay = function (id) {
    const ctx = overlayStack.get(id);
    if (!ctx) return false;
    overlayStack.delete(id);
    document.removeEventListener('keydown', ctx.keyHandler, true);
    document.removeEventListener('focusin', ctx.focusHandler, true);
    // Restore focus to the previously-focused element (if it still exists in DOM).
    if (ctx.prevFocus && document.body.contains(ctx.prevFocus)) {
      try { ctx.prevFocus.focus({ preventScroll: true }); } catch {}
    }
    return true;
  };


  // ——— Enter-as-Tab convention ————————————————————————————————————————————
  // Global keydown listener (attached once at script load). Looks for the
  // marker `[data-omni-enter-as-tab="true"]` on the focused element or any
  // ancestor; when present, Enter acts like Tab (advances focus to the next
  // focusable element inside the marker's container). On the LAST field,
  // if there's a `[data-omni-default]` button, it gets clicked (submit on
  // last-field Enter — classic ERP/PDV pattern).
  //
  // Skipped automatically:
  //   • Shift/Ctrl/Meta/Alt + Enter (modifier preserves Enter's other roles)
  //   • Inside <textarea>          (Enter inserts a newline)
  //   • Inside contenteditable     (Enter inserts a line break)
  //   • On buttons/links/options   (Enter triggers click/select)
  //
  // Usage (consumer markup):
  //   <div data-omni-enter-as-tab="true">  <!-- container: form / dialog / etc. -->
  //     <input ... />                      <!-- Enter → next field -->
  //     <input ... />                      <!-- Enter → next field -->
  //     <button data-omni-default>Save</button> <!-- Enter on last field → click -->
  //   </div>
  //
  // Works EVERYWHERE — inside dialogs, drawers, and regular page forms.
  // Coexists with [data-omni-default] on overlay Enter handler: the marker
  // takes priority (advances), and only on the last field does default fire.
  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Enter') return;
    if (e.shiftKey || e.ctrlKey || e.metaKey || e.altKey) return;

    const target = e.target;
    if (!target || target.nodeType !== 1) return;
    const tag = target.tagName;

    // Don't intercept where Enter has a stronger native meaning
    if (tag === 'TEXTAREA') return;
    if (tag === 'BUTTON' || tag === 'A') return;
    if (target.isContentEditable) return;
    // <select> with a popup open should still receive Enter for selection.
    // (We only intercept inputs and similar form fields.)

    // Walk up looking for the marker
    const container = target.closest('[data-omni-enter-as-tab]');
    if (!container) return;
    const val = container.getAttribute('data-omni-enter-as-tab');
    if (val === 'false') return;

    const focusables = focusableWithin(container);
    const idx = focusables.indexOf(target);
    if (idx === -1) return;

    e.preventDefault();

    // Walk forward through the focusables looking for what should happen on
    // Enter. Rules (in order):
    //   • Skip [data-omni-cancel] buttons — they're for Esc/click, NOT for
    //     the Enter chain (otherwise Enter on the last input would land on
    //     "Cancel" which sits before the default in DOM order).
    //   • If we hit [data-omni-default] → click it (submit / "Enter on last
    //     field submits").
    //   • Otherwise → focus the next regular field.
    for (let i = idx + 1; i < focusables.length; i++) {
      const cand = focusables[i];
      if (!cand) continue;
      if (cand.matches && cand.matches('[data-omni-cancel]')) continue;
      if (cand.matches && cand.matches('[data-omni-default]:not([disabled])')) {
        cand.click();
        return;
      }
      if (typeof cand.focus === 'function') {
        cand.focus();
        if (cand.tagName === 'INPUT' && typeof cand.select === 'function') {
          try { cand.select(); } catch { /* not all input types support select */ }
        }
        return;
      }
    }

    // No next non-cancel focusable. Fallback: click [data-omni-default]
    // anywhere in the container if present (covers cases where the default
    // is outside the normal tab order).
    const def = container.querySelector('[data-omni-default]:not([disabled])');
    if (def) def.click();
  });

  // ——— Focus-trap convention ——————————————————————————————————————————————
  // Global keydown listener — when Tab/Shift+Tab is pressed and the focused
  // element has an ancestor with [data-omni-focus-trap="true"], the cycle is
  // contained inside that container (Tab on last → first, Shift+Tab on first
  // → last). Same Tab/cycling behavior dialogs get via `setupOverlay`, now
  // available declaratively for inline forms / page sections.
  //
  // Coexists with the dialog overlay trap: when a dialog is open the overlay's
  // own trap is more restrictive (it cycles inside the dialog regardless of
  // this marker). Outside overlays, this marker takes over.
  //
  // Usage:
  //   <div data-omni-focus-trap="true">
  //     <input ... />            <!-- Shift+Tab here cycles to last -->
  //     <input ... />
  //     <button>OK</button>      <!-- Tab here cycles back to first -->
  //   </div>
  //
  // Coexists with [data-omni-enter-as-tab]: the same container can have both,
  // giving "Enter advances field + Tab cycles in" — a classic PDV experience.
  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Tab') return;

    const target = e.target;
    if (!target || target.nodeType !== 1) return;

    // Skip if dialog/drawer overlay trap is already handling this — its
    // capture-phase handler runs first; if it called preventDefault the
    // event would be marked. We still cooperate: check if there's any
    // overlay in the stack; if so, defer to it.
    if (overlayStack && overlayStack.size > 0) return;

    const container = target.closest('[data-omni-focus-trap]');
    if (!container) return;
    const val = container.getAttribute('data-omni-focus-trap');
    if (val === 'false') return;

    const focusables = focusableWithin(container);
    if (focusables.length === 0) return;

    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  });

  // ─── OmniBottomSheet — live pointer drag ─────────────────────────────────
  // Manipula drag handle do bottom sheet — pointer events nativos, atualiza
  // CSS variable --omni-bs-drag (px) sem round-trip Blazor (60fps). Quando
  // soltar, chama dotnet com delta final pra C# decidir snap/dismiss.
  //
  // POR QUE NÃO OmniSwipeArea: ele dispara só no swipe-END (delta final).
  // Pra bottom sheet a UX exige translateY acompanhando o dedo EM TEMPO REAL.
  //
  // POR QUE CSS VAR EM VEZ DE STATE: setar element.style.transform a 60fps
  // é instantâneo; trocar state C# + render pra cada frame de drag mataria
  // perf em mobile.
  ns.bottomSheetAttachDrag = function (element, dotnet, opts) {
    if (!element || !dotnet) return;
    ns.bottomSheetDetachDrag(element);

    const onSnapMethod = (opts && opts.onSnap) || 'OnDragEnd';
    let startY = 0;
    let lastY = 0;
    let dragging = false;
    let pointerId = -1;

    const onDown = (e) => {
      // Só pointer primário; ignora múltiplos toques simultâneos.
      if (e.isPrimary === false) return;
      startY = e.clientY;
      lastY = 0;
      dragging = true;
      pointerId = e.pointerId;
      try { element.setPointerCapture(pointerId); } catch { }
      // Desabilita transition durante drag pra ficar grudado no dedo.
      element.style.setProperty('--omni-bs-dragging', '1');
      e.preventDefault();
    };

    const onMove = (e) => {
      if (!dragging || e.pointerId !== pointerId) return;
      lastY = e.clientY - startY;
      // Resistência subir além do snap máximo (efeito borracha clássico iOS).
      // Pra baixo (positivo) deixa livre — drag-to-dismiss precisa ser ágil.
      const offset = lastY < 0 ? -Math.pow(-lastY, 0.7) : lastY;
      element.style.setProperty('--omni-bs-drag', offset + 'px');
      e.preventDefault();
    };

    const onUp = (e) => {
      if (!dragging || e.pointerId !== pointerId) return;
      dragging = false;
      try { element.releasePointerCapture(pointerId); } catch { }
      element.style.removeProperty('--omni-bs-dragging');
      element.style.removeProperty('--omni-bs-drag');
      // Notifica C# com delta em PIXELS — C# decide snap/dismiss baseado
      // em SnapPoints e threshold.
      try { dotnet.invokeMethodAsync(onSnapMethod, lastY); } catch { }
    };

    element.addEventListener('pointerdown', onDown);
    element.addEventListener('pointermove', onMove);
    element.addEventListener('pointerup', onUp);
    element.addEventListener('pointercancel', onUp);
    element.__tvsBSDrag = { onDown, onMove, onUp };
  };

  ns.bottomSheetDetachDrag = function (element) {
    if (!element || !element.__tvsBSDrag) return;
    const h = element.__tvsBSDrag;
    element.removeEventListener('pointerdown', h.onDown);
    element.removeEventListener('pointermove', h.onMove);
    element.removeEventListener('pointerup', h.onUp);
    element.removeEventListener('pointercancel', h.onUp);
    delete element.__tvsBSDrag;
  };

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
