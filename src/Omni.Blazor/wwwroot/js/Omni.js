// Omni.Blazor JS helpers — kept minimal and dependency-free.
(function () {
  const ns = (window.omniBlazor = window.omniBlazor || {});

  ns.setAttr = function (selector, name, value) {
    const el = (selector === 'html' || selector === ':root')
      ? document.documentElement
      : document.querySelector(selector);
    if (!el) return;
    if (value === null || value === undefined || value === '') el.removeAttribute(name);
    else el.setAttribute(name, String(value));
  };

  ns.getAttr = function (selector, name) {
    const el = (selector === 'html' || selector === ':root')
      ? document.documentElement
      : document.querySelector(selector);
    return el ? el.getAttribute(name) : null;
  };

  // ——— Scroll manager —————————————————————————————————————————————————
  // Counter-based scroll lock: multiple components can lock simultaneously
  // (think: dialog inside a drawer); the body unlocks only when the last
  // owner releases. Keyed by selector so different scroll roots are independent.
  const scrollLockCounters = new Map(); // selector -> count

  // App-shell layouts (like OmniLayout) put the real scroll inside an inner
  // pane such as .omni-body, not on the document. `selector === "auto"` (or
  // omitted) walks up from the focused / active area to find the first
  // ancestor whose contents actually overflow. Falls back to the document.
  function isScrollable(el) {
    if (!el || el === document.documentElement || el === document.body) return false;
    const s = getComputedStyle(el);
    if (!/(auto|scroll)/.test(s.overflowY + s.overflowX)) return false;
    return el.scrollHeight > el.clientHeight || el.scrollWidth > el.clientWidth;
  }

  function findScrollRoot() {
    // 1) Procura dentro dos ancestrais do elemento focado — caminho mais
    //    preciso quando vem de click em botão (FAB, etc.). Botão recém-
    //    clicado geralmente é o activeElement.
    let node = document.activeElement;
    while (node && node !== document.body) {
      if (isScrollable(node)) return node;
      node = node.parentElement;
    }

    // 2) Document scroll quando algo realmente overflows a viewport.
    const doc = document.documentElement;
    if (doc.scrollHeight > doc.clientHeight) return doc;

    // 3) Maior elemento scrollable da página — heurística universal pra
    //    layouts complexos (app shell com .omni-main fixa + .omni-showcase-body
    //    scrolling dentro, ou similar). Pega o container com maior área visível
    //    que está com overflow. Tipicamente é o "main content" pra qualquer
    //    layout, sem precisar conhecer class names específicos.
    let best = null;
    let bestArea = 0;
    const all = document.querySelectorAll('*');
    for (let i = 0; i < all.length; i++) {
      const el = all[i];
      if (!isScrollable(el)) continue;
      const area = el.clientWidth * el.clientHeight;
      if (area > bestArea) { best = el; bestArea = area; }
    }
    if (best) return best;

    // 4) Fallback: documentElement.
    return doc;
  }

  function resolveScrollTarget(selector) {
    if (selector === 'window' || selector === null || selector === undefined || selector === 'auto') {
      return findScrollRoot();
    }
    if (selector === 'html' || selector === ':root') return document.documentElement;
    return document.querySelector(selector);
  }

  // For lockScroll: when target is the document root, we want to apply overflow
  // hidden to <html>. For inner panes, we apply it to the pane itself.
  ns.lockScroll = function (selector) {
    selector = selector || 'auto';
    const el = resolveScrollTarget(selector);
    if (!el) return;
    // Cache the resolved element by selector so unlock targets the same node
    // even if the DOM shifted.
    const key = selector + '|' + (el.id || el.className || el.tagName);
    const prev = scrollLockCounters.get(key) || 0;
    scrollLockCounters.set(key, prev + 1);
    if (prev === 0) {
      el.dataset.tvsScrollOverflow = el.style.overflow || '';
      el.style.overflow = 'hidden';
      // Remember which key locked this element so unlock without a selector works.
      el.dataset.tvsScrollKey = key;
    }
  };

  ns.unlockScroll = function (selector) {
    selector = selector || 'auto';
    const el = resolveScrollTarget(selector);
    if (!el) return;
    const key = el.dataset.tvsScrollKey || (selector + '|' + (el.id || el.className || el.tagName));
    const prev = scrollLockCounters.get(key) || 0;
    if (prev <= 0) return;
    if (prev === 1) {
      scrollLockCounters.delete(key);
      el.style.overflow = el.dataset.tvsScrollOverflow || '';
      delete el.dataset.tvsScrollOverflow;
      delete el.dataset.tvsScrollKey;
    } else {
      scrollLockCounters.set(key, prev - 1);
    }
  };

  ns.scrollLockCount = function (selector) {
    selector = selector || 'auto';
    const el = resolveScrollTarget(selector);
    if (!el) return 0;
    const key = el.dataset.tvsScrollKey || (selector + '|' + (el.id || el.className || el.tagName));
    return scrollLockCounters.get(key) || 0;
  };

  function scrollTargetFor(selector) {
    if (selector === 'window' || !selector || selector === 'auto') {
      return findScrollRoot();
    }
    if (selector === 'html' || selector === ':root') return document.documentElement;
    return document.querySelector(selector);
  }

  ns.scrollTo = function (selector, opts) {
    const o = opts || {};
    const el = scrollTargetFor(selector);
    if (!el) return;
    el.scrollTo({ top: o.top || 0, left: o.left || 0, behavior: o.behavior || 'auto' });
  };

  ns.scrollIntoView = function (selector, opts) {
    const el = document.querySelector(selector);
    if (!el) return;
    el.scrollIntoView({
      behavior: (opts && opts.behavior) || 'auto',
      block: (opts && opts.block) || 'start',
      inline: (opts && opts.inline) || 'nearest'
    });
  };

  ns.scrollToTop = function (selector, behavior) {
    const el = scrollTargetFor(selector);
    if (!el) return;
    el.scrollTo({ top: 0, behavior: behavior || 'auto' });
  };

  // ─── Scroll position observer (rAF-throttled) ──────────────────────────────
  // Permite observar scroll de um container e receber callback C# a cada frame
  // com snapshot de posição (top, height, percent). Usado por
  // OmniScrollToTopButton + qualquer código user-land via ScrollManager.
  //
  // rAF coalescing: scroll dispara MUITO (60+/s). rAF garante 1 callback por
  // frame, sem afogar Blazor SignalR com chamadas. ResizeObserver re-emite
  // quando conteúdo cresce (ex: lazy load) — sem isso o "percent" ficaria
  // stale após mudança de altura.
  const scrollObservers = new Map(); // token → { target, onScroll, ro }
  let _scrollTokenSeq = 0;

  function _computeScrollInfo(el) {
    // window vs Element APIs unificadas: documentElement ou Element comum.
    const isWin = el === window;
    const node = isWin ? document.documentElement : el;
    const scrollTop = isWin ? window.scrollY : node.scrollTop;
    const scrollHeight = node.scrollHeight;
    const clientHeight = isWin ? window.innerHeight : node.clientHeight;
    const maxScroll = Math.max(0, scrollHeight - clientHeight);
    const percent = maxScroll > 0 ? Math.min(1, Math.max(0, scrollTop / maxScroll)) : 0;
    return { scrollTop, scrollHeight, clientHeight, maxScroll, percent };
  }

  ns.observeScrollPosition = function (selector, dotnet, opts) {
    if (!dotnet) return null;
    const target = scrollTargetFor(selector);
    if (!target) return null;

    const method = (opts && opts.method) || 'OnScroll';
    const callOnInit = !opts || opts.callOnInit !== false;

    let scheduled = false;
    let lastTop = -1;
    const fire = () => {
      const info = _computeScrollInfo(target);
      // Skip se mudou nada (evita render desnecessário no C#).
      if (info.scrollTop === lastTop) return;
      lastTop = info.scrollTop;
      try { dotnet.invokeMethodAsync(method, info); } catch { /* circuit gone */ }
    };

    const onScroll = () => {
      if (scheduled) return;
      scheduled = true;
      requestAnimationFrame(() => {
        scheduled = false;
        fire();
      });
    };

    // Use addEventListener com passive:true — não bloqueia scroll nativo.
    // Pra window scroll, listener vai no window mesmo.
    const eventTarget = target === window ? window : target;
    eventTarget.addEventListener('scroll', onScroll, { passive: true });

    // ResizeObserver pra detectar mudança no scrollHeight (lazy load, etc.)
    let ro = null;
    if (window.ResizeObserver && target !== window) {
      ro = new ResizeObserver(onScroll);
      ro.observe(target);
      // Observa o primeiro filho também — mudança de content height fora do container
      // (ex: items adicionados ao conteúdo) só dispara RO no FILHO, não no container.
      if (target.firstElementChild) {
        try { ro.observe(target.firstElementChild); } catch { }
      }
    }

    const token = String(++_scrollTokenSeq);
    scrollObservers.set(token, { target: eventTarget, onScroll, ro });

    if (callOnInit) {
      // Emite estado inicial pro componente saber se já está scrollado na 1ª render.
      lastTop = -1; // força fire
      requestAnimationFrame(fire);
    }
    return token;
  };

  ns.unobserveScrollPosition = function (token) {
    if (!token) return;
    const data = scrollObservers.get(token);
    if (!data) return;
    data.target.removeEventListener('scroll', data.onScroll, { passive: true });
    if (data.ro) data.ro.disconnect();
    scrollObservers.delete(token);
  };

  // ─── Sparkline size observer ──────────────────────────────────────────────
  // Mede o container do sparkline e notifica C# sempre que muda. Sem isso,
  // o SVG usaria viewBox fixo + preserveAspectRatio="none" que ESTICA o
  // conteúdo, fazendo markers (circles) virarem elipses em containers de
  // aspect ratio diferente do viewBox default (100:30).
  //
  // Com ResizeObserver, C# pode renderizar o SVG usando viewBox em PIXELS
  // reais (1 unidade SVG = 1 pixel) — círculos ficam redondos, paths
  // proporcionais ao tamanho real.
  ns.observeSparklineSize = function (element, dotnet, method) {
    if (!element || !dotnet || !window.ResizeObserver) return;
    // Cleanup defensivo se já estava observando.
    ns.unobserveSparklineSize(element);
    const ro = new ResizeObserver(entries => {
      const e = entries[0];
      const cr = e.contentRect;
      try { dotnet.invokeMethodAsync(method || 'OnSizeChanged', cr.width, cr.height); } catch { }
    });
    ro.observe(element);
    element.__tvsSparklineRO = ro;
  };

  ns.unobserveSparklineSize = function (element) {
    if (!element || !element.__tvsSparklineRO) return;
    element.__tvsSparklineRO.disconnect();
    delete element.__tvsSparklineRO;
  };

  ns.scrollToBottom = function (selector, behavior) {
    const el = scrollTargetFor(selector);
    if (!el) return;
    el.scrollTo({ top: el.scrollHeight, behavior: behavior || 'auto' });
  };

  ns.scrollOffsetY = function (selector) {
    const el = scrollTargetFor(selector);
    if (!el) return 0;
    return (el === document.documentElement) ? window.scrollY : el.scrollTop;
  };

  // ——— Viewport / Breakpoint observer ————————————————————————————————
  // Single window.resize listener (debounced 100ms) dispatching to all
  // registered Blazor subscribers. Bootstrap-style thresholds:
  //   xs <576 | sm <768 | md <992 | lg <1200 | xl <1400 | xxl ≥1400
  // Subscriber receives the breakpoint NAME (string) as the only arg.
  function bpName(w) {
    if (w >= 1400) return 'xxl';
    if (w >= 1200) return 'xl';
    if (w >= 992)  return 'lg';
    if (w >= 768)  return 'md';
    if (w >= 576)  return 'sm';
    return 'xs';
  }

  const viewportSubs = new Map(); // id -> { dotnet, method }
  let viewportLastBp = null;
  let viewportTimer = null;
  let viewportListenerAttached = false;

  function viewportFire(force) {
    const bp = bpName(window.innerWidth);
    if (!force && bp === viewportLastBp) return;
    viewportLastBp = bp;
    viewportSubs.forEach(s => {
      try { s.dotnet.invokeMethodAsync(s.method || 'OnBreakpointChanged', bp); }
      catch { /* ref disposed during teardown */ }
    });
  }

  function viewportOnResize() {
    if (viewportTimer) clearTimeout(viewportTimer);
    viewportTimer = setTimeout(() => { viewportTimer = null; viewportFire(false); }, 100);
  }

  ns.subscribeViewport = function (id, dotnet, method) {
    if (!id || !dotnet) return null;
    viewportSubs.set(id, { dotnet, method });
    if (!viewportListenerAttached) {
      window.addEventListener('resize', viewportOnResize, { passive: true });
      viewportListenerAttached = true;
    }
    // Return the current breakpoint synchronously so the subscriber doesn't
    // have to wait for the first resize event.
    return bpName(window.innerWidth);
  };

  ns.unsubscribeViewport = function (id) {
    if (!id) return;
    viewportSubs.delete(id);
    if (viewportSubs.size === 0 && viewportListenerAttached) {
      window.removeEventListener('resize', viewportOnResize);
      viewportListenerAttached = false;
      if (viewportTimer) { clearTimeout(viewportTimer); viewportTimer = null; }
      viewportLastBp = null;
    }
  };

  ns.currentBreakpoint = function () { return bpName(window.innerWidth); };

  // Live media-query check. Use this (not the cached breakpoint) when a
  // decision must agree with a CSS `@media` rule — `window.matchMedia` is the
  // browser's canonical evaluator and reflects DevTools device-emulation /
  // page-zoom / actual resize instantly, with the same numeric threshold the
  // stylesheet uses.
  ns.matchesMedia = function (query) {
    try { return window.matchMedia(query).matches; } catch { return false; }
  };

  // Live subscription to a CSS media query. Returns the current `matches`
  // boolean synchronously AND attaches a listener that pings the .NET
  // component when the match status flips. Each (key, dotnet) pair has at
  // most one active subscription — calling subscribe again with the same
  // key swaps the query/listener cleanly (re-renders that change Query keep
  // the registry tidy).
  //
  // Used by OmniMediaQuery. Different from `subscribeViewport` which only
  // fires when the cached breakpoint NAME changes (xs→sm) — this fires on
  // any matches→!matches flip, which is the right behavior for arbitrary
  // CSS queries like "(prefers-color-scheme: dark)" or "(orientation: portrait)".
  const mediaQueryRegistry = new Map(); // key -> { mql, listener }

  ns.subscribeMediaQuery = function (key, query, dotnet, method) {
    if (!key || !query || !dotnet) return false;
    // Tear down prior subscription with same key (idempotent re-subscribe)
    ns.unsubscribeMediaQuery(key);
    try {
      const mql = window.matchMedia(query);
      const listener = function (e) {
        try { dotnet.invokeMethodAsync(method || 'OnMediaQueryChanged', e.matches); }
        catch { /* ref disposed */ }
      };
      // `addEventListener` is the modern API; `addListener` is the deprecated
      // fallback for older Safari. We try modern first, then fall back.
      if (mql.addEventListener) mql.addEventListener('change', listener);
      else mql.addListener(listener);
      mediaQueryRegistry.set(key, { mql, listener });
      return mql.matches;
    } catch {
      return false;
    }
  };

  ns.unsubscribeMediaQuery = function (key) {
    if (!key) return;
    const entry = mediaQueryRegistry.get(key);
    if (!entry) return;
    try {
      if (entry.mql.removeEventListener) entry.mql.removeEventListener('change', entry.listener);
      else entry.mql.removeListener(entry.listener);
    } catch { /* ignore */ }
    mediaQueryRegistry.delete(key);
  };
  // Expose for debugging
  Object.defineProperty(ns, '_viewportSubs', { get: () => viewportSubs });

  // Lightweight click-outside dispatcher.
  // The Blazor component registers a DotNet object reference and a CSS selector;
  // we invoke its `OnClickOutside` whenever a document click lands outside.
  const outsideRegistry = new Map();
  document.addEventListener('mousedown', function (e) {
    outsideRegistry.forEach(function (entry, key) {
      const target = entry.selector ? document.querySelector(entry.selector) : entry.el;
      if (target && !target.contains(e.target)) {
        entry.dotnet.invokeMethodAsync(entry.method || 'OnClickOutside').catch(() => {});
      }
    });
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

  // Focus first focusable inside a container (used by dialogs)
  ns.focusFirst = function (el) {
    if (!el) return;
    const focusable = el.querySelector(
      'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
    );
    if (focusable) focusable.focus();
  };

  // Scroll an element (by ElementReference or CSS selector) into view. Used by
  // keyboard-navigated lists (e.g. OmniCommandPalette) to follow the active item.
  ns.scrollIntoView = function (target, block) {
    try {
      const el = typeof target === "string" ? document.querySelector(target) : target;
      if (el) el.scrollIntoView({ block: block || "nearest", inline: "nearest" });
    } catch (e) { /* ignore */ }
  };

  // Read element position (used by tooltip/context menu auto-flip — kept simple)
  ns.elementRect = function (el) {
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { x: r.x, y: r.y, w: r.width, h: r.height, top: r.top, right: r.right, bottom: r.bottom, left: r.left };
  };

  // localStorage helpers — used by ThemeService to persist user preferences.
  // Safe in private mode / SSR (silently return null / swallow errors).
  ns.storageGet = function (key) {
    try { return window.localStorage.getItem(key); } catch { return null; }
  };
  ns.storageSet = function (key, value) {
    try { window.localStorage.setItem(key, value); } catch {}
  };
  ns.storageRemove = function (key) {
    try { window.localStorage.removeItem(key); } catch {}
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

  // Mask helper — ported in spirit from Radzen.mask. Mask chars:
  //   9 = digit, A = letter, * = alphanumeric. Anything else is a literal.
  // Filters el.value to keep only chars that could fit any slot, then walks the
  // mask emitting either the next valid char or a literal separator. Writes
  // back to the DOM and preserves caret like Radzen does: if the cursor sat at
  // the end of the old value, leave it at the end of the new one; otherwise
  // try to restore the original selection range (clamped).
  function isMaskSlot(m) { return m === '9' || m === 'A' || m === '*'; }
  function maskSlotAccepts(slot, c) {
    if (slot === '9') return c >= '0' && c <= '9';
    if (slot === 'A') return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    if (slot === '*') return /[0-9A-Za-z]/.test(c);
    return false;
  }
  function maskAcceptsAny(c, mask) {
    for (var i = 0; i < mask.length; i++) {
      if (maskSlotAccepts(mask[i], c)) return true;
    }
    return false;
  }
  function formatWithMask(value, mask) {
    if (!mask) return value;
    var chars = [];
    for (var i = 0; i < value.length; i++) {
      if (maskAcceptsAny(value[i], mask)) chars.push(value[i]);
    }
    var out = '';
    var count = 0;
    for (var i = 0; i < mask.length; i++) {
      var m = mask[i];
      if (count >= chars.length) break;
      if (isMaskSlot(m)) {
        if (maskSlotAccepts(m, chars[count])) {
          out += chars[count];
          count++;
        } else {
          count++; i--; // skip char that can't fill this slot
        }
      } else {
        out += m;
        // user typed the literal — consume so it doesn't shift
        if (chars[count] === m) count++;
      }
    }
    return out;
  }

  ns.applyMask = function (el, mask) {
    if (!el || !mask) return el ? el.value : '';
    var value = el.value;
    var formatted = formatWithMask(value, mask);
    if (formatted === value) return value;

    var atEnd = el.selectionStart === value.length;
    var start = el.selectionStart;
    var end = el.selectionEnd;
    el.value = formatted;
    if (el.setSelectionRange) {
      if (atEnd) {
        try { el.setSelectionRange(formatted.length, formatted.length); } catch {}
      } else {
        var s = Math.min(start ?? formatted.length, formatted.length);
        var e2 = Math.min(end ?? formatted.length, formatted.length);
        try { el.setSelectionRange(s, e2); } catch {}
      }
    }
    return formatted;
  };

  ns.getInputValue = function (el) { return el ? el.value : ''; };

  // Force-set an input's value (and optionally the caret). Used after blur-time
  // reformatting where Blazor's diff would skip the DOM update because the
  // bound string matches its previous render.
  ns.setInputValue = function (el, value, caret) {
    if (!el) return;
    if (el.value !== value) el.value = value;
    if (typeof caret === 'number' && el.setSelectionRange) {
      var p = Math.min(Math.max(caret, 0), value.length);
      try { el.setSelectionRange(p, p); } catch {}
    }
  };

  // Numeric input key filter — ported from Radzen.numericKeyPress.
  // Blocks any character that isn't a Unicode digit, minus sign, or (when the
  // bound type isn't integer) the culture's decimal separator. Translates the
  // numpad decimal key into the culture's separator. Control keys and meta/
  // ctrl/alt combinations pass through so copy/paste/select-all still work.
  ns.numericKeyPress = function (e, isInteger, decimalSeparator) {
    if (e.metaKey || e.ctrlKey || e.altKey) return;
    var k = e.key;
    if (k === 'Tab' || k === 'Backspace' || k === 'Delete' || k === 'Enter' ||
        k === 'ArrowLeft' || k === 'ArrowRight' || k === 'ArrowUp' || k === 'ArrowDown' ||
        k === 'Home' || k === 'End') return;

    if (e.code === 'NumpadDecimal' && !isInteger) {
      var t = e.target;
      var s = t.selectionStart, en = t.selectionEnd;
      // Only insert if not already at the cursor (avoid duplicate)
      if (t.value.indexOf(decimalSeparator) === -1) {
        t.value = t.value.slice(0, s) + decimalSeparator + t.value.slice(en);
        var pos = s + decimalSeparator.length;
        try { t.setSelectionRange(pos, pos); } catch {}
      }
      e.preventDefault();
      return;
    }

    if (/\p{Nd}/u.test(k) || k === '-' || (!isInteger && k === decimalSeparator)) return;
    e.preventDefault();
  };

  // Numeric paste validator — rejects pastes that can't be parsed as a number
  // under the user's locale or that fall outside Min/Max.
  // Reset a <input type="file"> value by id so that dropping/selecting the
  // same file again still fires the change event. Browsers suppress the event
  // when the FileList is identical to the previous one — this is the standard
  // Radzen-style workaround (see Radzen.removeFileFromUpload).
  ns.clearFileInput = function (id) {
    var el = document.getElementById(id);
    if (el && el.tagName === 'INPUT' && el.type === 'file') {
      try { el.value = ''; } catch {}
    }
  };

  ns.numericOnPaste = function (e, min, max) {
    if (!e.clipboardData) return;
    var value = e.clipboardData.getData('text');
    if (!value) { e.preventDefault(); return; }
    value = String(value).trim();

    var parts = new Intl.NumberFormat(navigator.language).formatToParts(1234567.89);
    var group = ',', dec = '.';
    for (var i = 0; i < parts.length; i++) {
      if (parts[i].type === 'group') group = parts[i].value;
      if (parts[i].type === 'decimal') dec = parts[i].value;
    }
    value = value.replace(/[  ]/g, ' ');
    if (group) value = value.split(group).join('');
    if (dec !== '.') value = value.split(dec).join('.');
    if (!/^[+-]?(\d+(\.\d*)?|\.\d+)$/.test(value)) { e.preventDefault(); return; }
    var n = Number(value);
    if (!isFinite(n)) { e.preventDefault(); return; }
    if (min != null && n < min) { e.preventDefault(); return; }
    if (max != null && n > max) { e.preventDefault(); return; }
  };

  // ——— Hotkeys ————————————————————————————————————————————————————————
  // Single document-level keydown listener dispatches to all registered hotkeys.
  // Each entry holds the combos to match, the C# callback target, and flags.
  // Memory-leak design:
  //   - Each registration is keyed by `id` and removed individually on unregister.
  //   - The document listener attaches once on first registration and detaches
  //     when the registry empties.
  //   - All DotNetObjectReferences are owned by C# (HotkeyService keeps one ref
  //     for the whole service); we never store extra refs JS-side.
  const hotkeys = new Map();
  // Expose for debugging / leak tests.
  Object.defineProperty(ns, '_hotkeys', { get: () => hotkeys });
  let hkListenerAttached = false;

  // Sequence ("g d") support: a rolling buffer of recent non-modifier keystrokes.
  const SEQ_TIMEOUT = 1200;   // ms allowed between consecutive sequence keys
  let seqBuffer = [];
  let seqMaxLen = 0;          // longest registered sequence — the buffer is capped to it

  function recomputeSeqMax() {
    let m = 0;
    for (const [, h] of hotkeys)
      if (h.sequences) for (const s of h.sequences) if (s.length > m) m = s.length;
    seqMaxLen = m;
  }

  function isModifierKey(k) {
    return k === 'Shift' || k === 'Control' || k === 'Alt' || k === 'Meta' ||
           k === 'CapsLock' || k === 'NumLock' || k === 'ScrollLock' ||
           k === 'AltGraph' || k === 'OS';
  }

  function comboKeyMatch(b, c) {
    if (b.ctrl  !== !!c.ctrl ) return false;
    if (b.alt   !== !!c.alt  ) return false;
    if (b.shift !== !!c.shift) return false;
    if (b.meta  !== !!c.meta ) return false;
    const k = (c.key || '').toLowerCase();
    if (!k) return false;
    return (b.key && b.key.toLowerCase() === k) || (b.code && b.code.toLowerCase() === k);
  }

  // Does the END of the buffer exactly equal this sequence (with in-time keystrokes)?
  function seqMatchesTail(seq) {
    if (seqBuffer.length < seq.length) return false;
    const start = seqBuffer.length - seq.length;
    for (let i = 0; i < seq.length; i++) {
      const b = seqBuffer[start + i];
      if (!comboKeyMatch(b, seq[i])) return false;
      if (i > 0 && (b.t - seqBuffer[start + i - 1].t) > SEQ_TIMEOUT) return false;
    }
    return true;
  }

  // Invoke a hotkey's C# handler.
  function invokeHotkey(h, e) {
    try {
      h.dotnet.invokeMethodAsync(h.method, h.id, e
        ? { key: e.key, code: e.code, ctrlKey: e.ctrlKey, altKey: e.altKey, shiftKey: e.shiftKey, metaKey: e.metaKey }
        : { key: '', code: '', ctrlKey: false, altKey: false, shiftKey: false, metaKey: false });
    } catch { /* dotnet ref may be disposed during teardown — ignore */ }
  }

  function isInEditable(el) {
    if (!el) return false;
    const tag = el.tagName;
    if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') {
      // Treat read-only inputs as non-editable so hotkeys still fire over them.
      if ('readOnly' in el && el.readOnly) return false;
      return true;
    }
    return !!el.isContentEditable;
  }

  function hkMatches(e, c, inEditable) {
    // Suppress modifier-less hotkeys while typing into editable fields.
    if (inEditable && !c.ctrl && !c.alt && !c.meta) return false;
    if (e.ctrlKey  !== !!c.ctrl ) return false;
    if (e.altKey   !== !!c.alt  ) return false;
    if (e.shiftKey !== !!c.shift) return false;
    if (e.metaKey  !== !!c.meta ) return false;
    const k = (c.key || '').toLowerCase();
    if (!k) return false;
    return (e.key && e.key.toLowerCase() === k) || (e.code && e.code.toLowerCase() === k);
  }

  function hkHandler(e) {
    if (e.repeat) return;            // ignore key-hold; one fire per press
    if (hotkeys.size === 0) return;  // defensive — listener should have detached
    const inEditable = isInEditable(e.target);

    function fire(h) {
      if (h.preventDefault) e.preventDefault();
      if (h.stopPropagation) e.stopPropagation();
      invokeHotkey(h, e);
    }

    // 1) Single combos — insertion order, first match wins.
    for (const [, h] of hotkeys) {
      if (h.disabled) continue;
      for (let i = 0; i < h.combos.length; i++) {
        if (hkMatches(e, h.combos[i], inEditable)) {
          seqBuffer = [];            // a real combo consumed this key
          fire(h);
          return;
        }
      }
    }

    // 2) Sequences ("g d") — global only, never while typing, never bare modifiers.
    if (seqMaxLen === 0 || inEditable || isModifierKey(e.key)) return;
    const now = (typeof performance !== 'undefined' && performance.now) ? performance.now() : 0;
    seqBuffer.push({ key: e.key, code: e.code, ctrl: e.ctrlKey, alt: e.altKey, shift: e.shiftKey, meta: e.metaKey, t: now });
    if (seqBuffer.length > seqMaxLen) seqBuffer.shift();

    // Fire the longest matching tail as soon as it completes. A sequence that is a
    // prefix of a longer one will win, so register distinct (non-overlapping) sequences.
    let bestH = null, bestLen = 0;
    for (const [, h] of hotkeys) {
      if (h.disabled || !h.sequences) continue;
      for (const seq of h.sequences) {
        if (seq.length > bestLen && seqMatchesTail(seq)) { bestH = h; bestLen = seq.length; }
      }
    }
    if (!bestH) return;               // no match yet — keep buffering
    seqBuffer = [];
    fire(bestH);
  }

  ns.registerHotkey = function (id, dotnet, method, combos, sequences, preventDefault, stopPropagation) {
    if (!id || !dotnet || !Array.isArray(combos)) return;
    const seqs = Array.isArray(sequences) ? sequences.filter(s => Array.isArray(s) && s.length) : [];
    if (combos.length === 0 && seqs.length === 0) return;
    // Replace any prior entry for the same id (re-registration from a re-render).
    hotkeys.set(id, {
      id, dotnet, method,
      combos,
      sequences: seqs,
      preventDefault: !!preventDefault,
      stopPropagation: !!stopPropagation,
      disabled: false
    });
    recomputeSeqMax();
    if (!hkListenerAttached) {
      document.addEventListener('keydown', hkHandler, true);
      hkListenerAttached = true;
    }
  };
  ns.unregisterHotkey = function (id) {
    if (!id) return;
    hotkeys.delete(id);
    recomputeSeqMax();
    if (hkListenerAttached && hotkeys.size === 0) {
      document.removeEventListener('keydown', hkHandler, true);
      hkListenerAttached = false;
      seqBuffer = [];
    }
  };
  ns.setHotkeyDisabled = function (id, disabled) {
    const h = hotkeys.get(id);
    if (h) h.disabled = !!disabled;
  };

  // ——— Drag & drop helper ———————————————————————————————————————————
  // HTML5 drag/drop quirks workaround, ported from Radzen.prepareDrag:
  //   - dragover with preventDefault is required for drop to ever fire
  //   - Firefox refuses to start a drag unless setData() is called in dragstart
  // We attach minimal handlers on the element; Blazor's @ondrag* still fires.
  ns.prepareDrag = function (el) {
    if (!el) return;
    el.addEventListener('dragover',  function (e) { e.preventDefault(); });
    el.addEventListener('dragstart', function (e) {
      // The actual payload is held in C# (Container.Payload); any non-empty
      // dataTransfer string satisfies Firefox.
      try { e.dataTransfer.setData('text/plain', ''); } catch {}
    });
  };

  // Focus an element by id — used by OmniKanban to keep focus on a card after a
  // keyboard move re-renders the board. No-op if the element is gone.
  ns.focusElement = function (id) {
    if (!id) return;
    const el = document.getElementById(id);
    if (el) { try { el.focus(); } catch {} }
  };

  // OmniKanban — horizontal auto-scroll while dragging a card near the board
  // edges. `dragover` only fires during an active drag, so the listener is inert
  // otherwise. Attached once per board (guarded); the listener is GC'd when the
  // board element leaves the DOM on component dispose.
  ns.kanbanAutoScroll = function (board) {
    if (!board || board._omniAutoScroll) return;
    const EDGE = 64;   // px from the edge that triggers scrolling
    const SPEED = 18;  // px per dragover tick
    const handler = function (e) {
      const r = board.getBoundingClientRect();
      if (e.clientX < r.left + EDGE) board.scrollLeft -= SPEED;
      else if (e.clientX > r.right - EDGE) board.scrollLeft += SPEED;
    };
    board.addEventListener('dragover', handler);
    board._omniAutoScroll = handler;
  };

  // ——— Element-scoped key interceptor ————————————————————————————————
  // Counterpart to the global hotkey service: listens on a specific element
  // (and its descendants) rather than the document. Useful when you want
  // ESC / arrow nav / Enter to only fire while focus is inside a specific
  // dialog, popover, or list.
  //
  // Per registration we store: { element, dotnet, method, keys, options }.
  // Multiple subscribers on the same element share the keydown listener via
  // a per-element counter stashed in dataset.
  const keyInterceptors = new Map(); // id -> { el, listener, dotnet, method, keys, options }
  Object.defineProperty(ns, '_keyInterceptors', { get: () => keyInterceptors });

  function keyMatchesOption(e, opt) {
    if (!opt.key) return false;
    const k = opt.key.toLowerCase();
    const matchesKey = (e.key && e.key.toLowerCase() === k) ||
                       (e.code && e.code.toLowerCase() === k);
    if (!matchesKey) return false;
    if (opt.ctrl  !== undefined && opt.ctrl  !== null && e.ctrlKey  !== !!opt.ctrl ) return false;
    if (opt.alt   !== undefined && opt.alt   !== null && e.altKey   !== !!opt.alt  ) return false;
    if (opt.shift !== undefined && opt.shift !== null && e.shiftKey !== !!opt.shift) return false;
    if (opt.meta  !== undefined && opt.meta  !== null && e.metaKey  !== !!opt.meta ) return false;
    return true;
  }

  ns.attachKeyListener = function (id, element, dotnet, method, keys) {
    if (!id || !element || !dotnet || !Array.isArray(keys)) return;
    if (keyInterceptors.has(id)) return; // idempotent

    const listener = function (e) {
      for (let i = 0; i < keys.length; i++) {
        const opt = keys[i];
        if (keyMatchesOption(e, opt)) {
          if (opt.preventDefault)  e.preventDefault();
          if (opt.stopPropagation) e.stopPropagation();
          try {
            dotnet.invokeMethodAsync(method, id, opt.key, {
              key: e.key, code: e.code,
              ctrlKey: e.ctrlKey, altKey: e.altKey,
              shiftKey: e.shiftKey, metaKey: e.metaKey
            });
          } catch { /* dotnet ref disposed during teardown */ }
          return;
        }
      }
    };

    element.addEventListener('keydown', listener);
    keyInterceptors.set(id, { el: element, listener, dotnet, method, keys });
  };

  ns.detachKeyListener = function (id) {
    if (!id) return;
    const e = keyInterceptors.get(id);
    if (!e) return;
    e.el.removeEventListener('keydown', e.listener);
    keyInterceptors.delete(id);
  };

  // ——— Exit prompt ———————————————————————————————————————————————————
  // Set of component ids that want beforeunload protection. The listener is
  // attached once when the set becomes non-empty and detached when it empties.
  const exitPromptIds = new Set();
  Object.defineProperty(ns, '_exitPromptIds', { get: () => exitPromptIds });

  function beforeUnloadHandler(e) {
    if (exitPromptIds.size === 0) return;
    // Modern browsers ignore custom text; presence of returnValue is what triggers
    // the native confirm. setting both for compatibility.
    e.preventDefault();
    e.returnValue = '';
    return '';
  }

  ns.enableExitPrompt = function (id) {
    if (!id) return;
    const wasEmpty = exitPromptIds.size === 0;
    exitPromptIds.add(id);
    if (wasEmpty) window.addEventListener('beforeunload', beforeUnloadHandler);
  };
  ns.disableExitPrompt = function (id) {
    if (!id) return;
    exitPromptIds.delete(id);
    if (exitPromptIds.size === 0) {
      window.removeEventListener('beforeunload', beforeUnloadHandler);
    }
  };

  // Trigger a browser download from a string body (used by DataGrid CSV export).
  ns.downloadFile = function (filename, content, mime) {
    const blob = new Blob([content], { type: mime || 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename || 'download.txt';
    document.body.appendChild(a); a.click();
    setTimeout(() => { URL.revokeObjectURL(url); a.remove(); }, 0);
  };

  // Copy text to the clipboard. Returns true on success. Falls back to a
  // hidden <textarea> + execCommand when the async Clipboard API is unavailable
  // (insecure contexts, older browsers).
  ns.copyText = async function (text) {
    if (text == null) return false;
    try {
      if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return true;
      }
    } catch { /* fall through to legacy path */ }
    try {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      const ok = document.execCommand('copy');
      ta.remove();
      return ok;
    } catch {
      return false;
    }
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

  // ——— prefers-color-scheme observer ———————————————————————————————————
  // Notify Blazor when the OS dark/light preference flips. Components opt in
  // via subscribeColorScheme (single shared MediaQueryList listener).
  let colorSchemeSubs = new Map(); // id -> { dotnet, method }
  let colorSchemeMQL = null;
  function colorSchemeOnChange(e) {
    colorSchemeSubs.forEach(s => {
      try { s.dotnet.invokeMethodAsync(s.method || 'OnColorSchemeChanged', e.matches); }
      catch { /* circuit gone */ }
    });
  }

  ns.subscribeColorScheme = function (id, dotnet, method) {
    if (!id || !dotnet) return null;
    colorSchemeSubs.set(id, { dotnet, method });
    if (!colorSchemeMQL && window.matchMedia) {
      colorSchemeMQL = window.matchMedia('(prefers-color-scheme: dark)');
      // Use addEventListener if available; older Safari needs addListener.
      if (colorSchemeMQL.addEventListener) colorSchemeMQL.addEventListener('change', colorSchemeOnChange);
      else if (colorSchemeMQL.addListener) colorSchemeMQL.addListener(colorSchemeOnChange);
    }
    return colorSchemeMQL ? colorSchemeMQL.matches : false;
  };

  ns.unsubscribeColorScheme = function (id) {
    if (!id) return;
    colorSchemeSubs.delete(id);
    if (colorSchemeSubs.size === 0 && colorSchemeMQL) {
      if (colorSchemeMQL.removeEventListener) colorSchemeMQL.removeEventListener('change', colorSchemeOnChange);
      else if (colorSchemeMQL.removeListener) colorSchemeMQL.removeListener(colorSchemeOnChange);
      colorSchemeMQL = null;
    }
  };

  ns.prefersColorSchemeDark = function () {
    try { return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches; }
    catch { return false; }
  };

  // ——— AppBar scroll observer ——————————————————————————————————————————
  // Watches the nearest scrolling ancestor of the AppBar and reports two
  // booleans back to Blazor: scrolled (the user has scrolled past the bar's
  // height) and hidden (currently scrolling DOWN — bar should hide). Both
  // toggle on simple thresholds; the AppBar component uses them to swap
  // data-scrolled and data-hidden attrs that CSS animates.
  const appBarObservers = new WeakMap(); // el -> { scrollTarget, onScroll, lastY, dotnet, method }

  // Renomeado de findScrollRoot pra findAppBarScrollAncestor — função de hoisting
  // do JS faz duplicatas com mesmo nome sobrescreverem umas às outras (a última
  // declarada vence pra TODA a IIFE). Antes, essa função (escopo AppBar, recebe
  // `el`) sobrescrevia a findScrollRoot global usada por scrollTargetFor →
  // resultado: scrollTo("auto") sempre retornava window mesmo em layouts onde
  // o scroll real estava em .omni-showcase-body. FAB "Voltar ao topo" não funcionava.
  function findAppBarScrollAncestor(el) {
    // Walk up looking for the first ancestor that actually scrolls.
    let node = el && el.parentElement;
    while (node && node !== document.body) {
      const s = getComputedStyle(node);
      if (/(auto|scroll)/.test(s.overflowY) && node.scrollHeight > node.clientHeight) return node;
      node = node.parentElement;
    }
    // Fall back to document/window.
    return document.documentElement.scrollHeight > document.documentElement.clientHeight
      ? document.documentElement
      : window;
  }

  ns.observeAppBarScroll = function (el, dotnet, method, hideOnScroll, elevateOnScroll) {
    if (!el || !dotnet) return false;
    // Tear down any previous registration on this same element.
    ns.unobserveAppBarScroll(el);
    const scrollTarget = findAppBarScrollAncestor(el);
    const isWindow = scrollTarget === window;
    const getY = () => isWindow ? window.scrollY : scrollTarget.scrollTop;
    const threshold = Math.max(el.getBoundingClientRect().height || 56, 24);
    let lastY = getY();
    let lastReported = { scrolled: false, hidden: false };

    function onScroll() {
      const y = getY();
      const goingDown = y > lastY + 4;
      const goingUp   = y < lastY - 4;
      const scrolled  = elevateOnScroll && y > threshold;
      const hidden    = hideOnScroll && goingDown && y > threshold * 1.5;
      // Only report when state changes (less interop chatter).
      if (scrolled !== lastReported.scrolled || (hideOnScroll && goingUp && lastReported.hidden) || (hideOnScroll && hidden && !lastReported.hidden)) {
        const newHidden = hideOnScroll ? (goingUp ? false : (hidden ? true : lastReported.hidden)) : false;
        lastReported = { scrolled, hidden: newHidden };
        try { dotnet.invokeMethodAsync(method || 'OnScrollChanged', scrolled, newHidden); } catch {}
      }
      lastY = y;
    }
    // Run once to set the initial state.
    onScroll();
    const target = isWindow ? window : scrollTarget;
    target.addEventListener('scroll', onScroll, { passive: true });
    appBarObservers.set(el, { scrollTarget, onScroll, isWindow });
    return true;
  };

  ns.unobserveAppBarScroll = function (el) {
    if (!el) return;
    const ctx = appBarObservers.get(el);
    if (!ctx) return;
    appBarObservers.delete(el);
    const target = ctx.isWindow ? window : ctx.scrollTarget;
    try { target.removeEventListener('scroll', ctx.onScroll); } catch {}
  };

  // ——— View Transitions API ———————————————————————————————————————————
  // P3.5 — Wraps an arbitrary DOM update in document.startViewTransition()
  // so the browser cross-fades the old → new pixel state. Used by Blazor
  // navigation: OmniLayout subscribes to LocationChanging and passes a
  // continuation here. Falls back to direct execution when unsupported.
  ns.supportsViewTransitions = function () {
    return typeof document.startViewTransition === 'function';
  };

  // Subscribes to "navigation about to happen" events from Blazor. The
  // callback returned by Blazor is invoked inside a view transition. Pure
  // helper — OmniLayout decides when/where to call this.
  ns.startViewTransition = function (callback) {
    if (typeof document.startViewTransition !== 'function') {
      // Fallback: just run the callback directly (no animation).
      try { return callback ? callback() : null; } catch { return null; }
    }
    try {
      // Returns a ViewTransition object; we ignore it (browser handles cleanup).
      document.startViewTransition(() => { try { callback && callback(); } catch {} });
    } catch {
      // Some browsers reject mid-flight; degrade gracefully.
      try { callback && callback(); } catch {}
    }
  };

  // Convenience: wraps `setAttr` in a view transition. Useful for theme
  // density/dark toggles that should fade in/out smoothly.
  ns.viewTransitionSetAttr = function (selector, name, value) {
    ns.startViewTransition(function () { ns.setAttr(selector, name, value); });
  };

  // ——— Splitter ————————————————————————————————————————————————————————
  // Tiny helpers used by OmniSplitter for resize math + pointer capture.
  // We capture the pointer on the bar element on pointerdown so the bar
  // continues to receive pointermove events even after the cursor leaves
  // its bounding box — otherwise drag would feel "sticky" near pane edges.

  // Returns the splitter's inner dimension (px) for the active axis.
  // horizontal=true → width; false → height.
  ns.splitterMeasure = function (el, horizontal) {
    if (!el) return 0;
    try {
      const r = el.getBoundingClientRect();
      return horizontal ? r.width : r.height;
    } catch { return 0; }
  };

  // setPointerCapture on a specific bar (by id) so pointermove keeps firing
  // even when the cursor leaves the bar's bounding box during drag.
  ns.splitterCapture = function (barId, pointerId) {
    const el = document.getElementById(barId);
    if (el && el.setPointerCapture) {
      try { el.setPointerCapture(pointerId); } catch { /* ignore */ }
    }
  };

  ns.splitterRelease = function (barId, pointerId) {
    const el = document.getElementById(barId);
    if (el && el.releasePointerCapture) {
      try { el.releasePointerCapture(pointerId); } catch { /* ignore */ }
    }
  };

  // ——— Slider ——————————————————————————————————————————————————————————
  // Returns [left, width] of the slider track element so the C# drag handler
  // can convert clientX → percentage. The track is horizontal (width-based);
  // vertical sliders would need a height variant.
  ns.sliderMeasure = function (el) {
    if (!el) return [0, 0];
    try {
      const r = el.getBoundingClientRect();
      return [r.left, r.width];
    } catch { return [0, 0]; }
  };

  // ——— SwipeArea ———————————————————————————————————————————————————————
  // Blazor wires @onpointer* events via passive listeners by default, which
  // means preventDefault() inside the Blazor handler is silently ignored by
  // the browser (per the Passive Event Listeners spec). To actually block
  // page scroll during a vertical swipe, we need to attach NATIVE listeners
  // with { passive: false } that call preventDefault() unconditionally.
  // We keep them in a registry keyed by element id so detach is symmetric.
  const swipeAreaRegistry = new Map();

  ns.swipeAreaAttachPreventDefault = function (elId) {
    const el = document.getElementById(elId);
    if (!el) return;
    if (swipeAreaRegistry.has(elId)) return;        // already attached
    const stop = function (e) { e.preventDefault(); };
    const opts = { passive: false, capture: false };
    const events = ['pointerdown', 'pointerup', 'pointermove', 'pointercancel', 'pointerleave', 'touchmove'];
    for (const name of events) el.addEventListener(name, stop, opts);
    swipeAreaRegistry.set(elId, { stop, events });
  };

  ns.swipeAreaDetachPreventDefault = function (elId) {
    const entry = swipeAreaRegistry.get(elId);
    if (!entry) return;
    const el = document.getElementById(elId);
    if (el) {
      const opts = { passive: false, capture: false };
      for (const name of entry.events) el.removeEventListener(name, entry.stop, opts);
    }
    swipeAreaRegistry.delete(elId);
  };

  // ─── SwipeArea LiveTransform — 60fps drag bypassando Blazor SignalR ───────
  // Em Blazor Server, cada pointermove via Blazor faz round-trip SignalR — fica
  // laggy em redes lentas. Esse modo anexa listeners JS nativos que atualizam
  // transform:translate(x,y) DIRETO no DOM via CSS variable, sem invocar C#
  // por frame. O OnSwipeEnd do C# continua disparando normalmente no pointerup.
  //
  // Axes: 'x' | 'y' | 'both'. Direção/translate é POSITIVO no sentido natural
  // do arraste (dragging right = +X, dragging down = +Y) — diferente do
  // delta interno do MudBlazor que usa xDown - currentX (invertido).
  const liveTransformRegistry = new Map();

  ns.swipeAreaAttachLiveTransform = function (elId, axes) {
    const el = document.getElementById(elId);
    if (!el) return;
    if (liveTransformRegistry.has(elId)) ns.swipeAreaDetachLiveTransform(elId);

    let startX = 0, startY = 0;
    let dragging = false;
    let pointerId = -1;
    const wantX = axes === 'x' || axes === 'both';
    const wantY = axes === 'y' || axes === 'both';

    const onDown = (e) => {
      if (e.isPrimary === false) return;
      startX = e.clientX;
      startY = e.clientY;
      dragging = true;
      pointerId = e.pointerId;
      try { el.setPointerCapture(pointerId); } catch { }
      el.style.setProperty('--omni-sa-dragging', '1');
    };
    const onMove = (e) => {
      if (!dragging || e.pointerId !== pointerId) return;
      const dx = wantX ? (e.clientX - startX) : 0;
      const dy = wantY ? (e.clientY - startY) : 0;
      // Atualiza CSS var no próprio elemento — o CSS aplica transform via
      // var(--omni-sa-dx, 0px) / var(--omni-sa-dy, 0px).
      if (wantX) el.style.setProperty('--omni-sa-dx', dx + 'px');
      if (wantY) el.style.setProperty('--omni-sa-dy', dy + 'px');
    };
    const onUp = (e) => {
      if (!dragging || e.pointerId !== pointerId) return;
      dragging = false;
      try { el.releasePointerCapture(pointerId); } catch { }
      // Reseta transform — o consumidor C# vai receber OnSwipeEnd com delta
      // total e pode decidir manter posição (snap) atualizando seu próprio state.
      el.style.removeProperty('--omni-sa-dragging');
      el.style.removeProperty('--omni-sa-dx');
      el.style.removeProperty('--omni-sa-dy');
    };

    el.addEventListener('pointerdown', onDown);
    el.addEventListener('pointermove', onMove);
    el.addEventListener('pointerup', onUp);
    el.addEventListener('pointercancel', onUp);
    liveTransformRegistry.set(elId, { onDown, onMove, onUp });
  };

  ns.swipeAreaDetachLiveTransform = function (elId) {
    const entry = liveTransformRegistry.get(elId);
    if (!entry) return;
    const el = document.getElementById(elId);
    if (el) {
      el.removeEventListener('pointerdown', entry.onDown);
      el.removeEventListener('pointermove', entry.onMove);
      el.removeEventListener('pointerup', entry.onUp);
      el.removeEventListener('pointercancel', entry.onUp);
      el.style.removeProperty('--omni-sa-dragging');
      el.style.removeProperty('--omni-sa-dx');
      el.style.removeProperty('--omni-sa-dy');
    }
    liveTransformRegistry.delete(elId);
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

  // ─── Speech-to-Text (Web Speech API) ──────────────────────────────────────
  // Wrapper sobre window.SpeechRecognition + webkit variant com state machine
  // explícito. Resolve 4 classes de bugs:
  //   1. Race conditions / double-click / InvalidStateError em Chrome
  //   2. UI mentindo "AO VIVO" quando Edge ainda está conectando ao serviço
  //   3. Auto-retry sobrescrevendo stop do usuário
  //   4. Stop preso (Edge não dispara onend após stop())
  //
  // Estados (espelhados em C# SpeechRecognitionState):
  //   idle       — sem sessão, pronto pra start
  //   connecting — clicou; aguardando audio capture (cobre onstart + retry loop)
  //   recording  — mic capturando de FATO (onaudiostart disparou)
  //   stopping   — usuário pediu stop, aguardando onend
  //   error      — falha não-recuperável; volta pra idle no próximo onend
  //
  // GATILHO CRÍTICO: a transição connecting → recording ocorre em onaudiostart,
  // NÃO em onstart. onstart só significa "browser criou o reconhecedor", não
  // "mic está capturando". No Edge especialmente, há uma janela onde onstart
  // já disparou mas o Microsoft Speech Service ainda está conectando.
  //
  // RETRY LOOP: erros de rede no Edge causam retry silencioso. Durante retry
  // o estado público FICA em 'connecting' (não pisca pra idle/recording).
  // O retry é cancelado se o usuário pedir stop (flag _speechUserStop).
  let _speechRec = null;
  let _speechState = 'idle';
  let _speechStartGuard = null;   // setTimeout id — watchdog do connecting
  let _speechPending = null;      // {dotnet, opts} aguardando 'end' atual
  let _speechLastToggleAt = 0;    // ms — debounce de cliques duplos
  let _speechRetryCount = 0;      // contador de retries por sessão (zera em onaudiostart)
  let _speechRetrying = false;    // true durante retry silencioso (mantém connecting)
  let _speechUserStop = false;    // true quando user pediu stop — bloqueia auto-retry
  const SPEECH_DEBOUNCE_MS = 250;

  // Detecção de Edge — UA contém "Edg/" (não "Edge/" que é o Edge legacy IE-based).
  // Edge usa Microsoft Speech Service, que tem cold start mais lento e é mais
  // sensível a rede.
  const _isEdge = (() => {
    try { return /\bEdg\//.test(navigator.userAgent); }
    catch { return false; }
  })();

  // Watchdog do connecting: tempo total esperado da chamada start() até
  // onaudiostart confirmar que o mic está ativo. Edge precisa de muito mais
  // tempo porque pode envolver retries do Microsoft Service.
  const SPEECH_CONNECT_TIMEOUT_MS = _isEdge ? 12000 : 5000;

  // Cooldown entre sessões (release de recurso de mic no browser).
  const SPEECH_PENDING_DELAY_MS = _isEdge ? 250 : 80;

  // Max retries automáticos por sessão pra erros transient (network).
  const SPEECH_MAX_RETRIES = _isEdge ? 2 : 0;

  // Map de string → int espelha SpeechRecognitionState C#.
  // ORDEM IMPORTANTE: deve casar com Models/SpeechRecognitionResult.cs.
  const _stateInt = { idle: 0, connecting: 1, recording: 2, stopping: 3, error: 4 };

  function _speechSetState(newState, dotnet, opts) {
    if (_speechState === newState) return;
    _speechState = newState;
    if (dotnet && opts && opts.stateMethod) {
      const stateIntVal = _stateInt[newState] ?? 0;
      try { dotnet.invokeMethodAsync(opts.stateMethod, stateIntVal); } catch { /* circuit gone */ }
    }
  }

  function _speechClearGuard() {
    if (_speechStartGuard) {
      clearTimeout(_speechStartGuard);
      _speechStartGuard = null;
    }
  }

  function _speechResetFlags() {
    _speechRetryCount = 0;
    _speechRetrying = false;
    _speechUserStop = false;
  }

  ns.speechSupported = function () {
    try { return !!(window.SpeechRecognition || window.webkitSpeechRecognition); }
    catch { return false; }
  };

  ns.speechState = function () { return _speechState; };

  /// Retorna info sobre o engine de reconhecimento (Chrome=Google, Edge=Microsoft).
  /// Permite UI customizar tooltip/hint pra avisar que Edge pode ser mais lento.
  ns.speechEngine = function () {
    if (_isEdge) return 'edge';
    try {
      if (/\bChrome\//.test(navigator.userAgent) && !/\bEdg\//.test(navigator.userAgent)) return 'chrome';
      if (/\bSafari\//.test(navigator.userAgent) && !/\bChrome\//.test(navigator.userAgent)) return 'safari';
    } catch { }
    return 'other';
  };

  ns.speechIsRecording = function (dotnet) {
    if (!_speechRec || _speechState !== 'recording') return false;
    if (!dotnet) return true;
    return _speechRec._dotnet && _speechRec._dotnet._id === dotnet._id;
  };

  ns.speechToggle = function (dotnet, opts) {
    if (!dotnet || !opts) return;
    const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SR) {
      try { dotnet.invokeMethodAsync(opts.unsupportedMethod || 'OnUnsupported'); }
      catch { /* circuit gone */ }
      return;
    }

    // Debounce contra dupla-click rápida (causa InvalidStateError em Chrome).
    const now = Date.now();
    if (now - _speechLastToggleAt < SPEECH_DEBOUNCE_MS) return;
    _speechLastToggleAt = now;

    if (_speechRec) {
      const sameComponent = _speechRec._dotnet && _speechRec._dotnet._id === dotnet._id;

      if (sameComponent) {
        // Mesmo componente:
        //   connecting/recording → user quer parar (sinaliza intenção + cancela retry)
        //   stopping/error       → ignora (esperando cleanup)
        if (_speechState === 'connecting' || _speechState === 'recording') {
          _speechUserStop = true;
          _speechRetrying = false;
          _speechPending = null;          // cancela qualquer retry pendente
          _speechRetryCount = SPEECH_MAX_RETRIES; // bloqueia novos retries
          _speechSetState('stopping', dotnet, opts);
          const target = _speechRec;
          try { target.stop(); } catch { try { target.abort(); } catch { } }
          // Edge: stop() às vezes não dispara onend; força abort após 1.5s
          if (_isEdge) {
            setTimeout(() => {
              if (_speechRec === target && _speechState === 'stopping') {
                try { target.abort(); } catch { }
              }
            }, 1500);
          }
        }
        return;
      }

      // Outro componente requisitou start. Enfileira pending e para o atual.
      // Se já havia outro pending (3+ cliques), avisa o displaced.
      if (_speechPending) {
        const displaced = _speechPending;
        try { displaced.dotnet.invokeMethodAsync(displaced.opts.errorMethod || 'OnError', 'superseded'); } catch { }
        try { displaced.dotnet.invokeMethodAsync(displaced.opts.stateMethod || 'OnStateChange', 0); } catch { }
      }
      _speechPending = { dotnet, opts };
      if (_speechState === 'recording' || _speechState === 'connecting') {
        const curDotnet = _speechRec._dotnet;
        const curOpts = _speechRec._opts;
        _speechUserStop = false; // cross-component não é user-stop
        _speechRetrying = false;
        _speechSetState('stopping', curDotnet, curOpts);
        try { _speechRec.stop(); } catch { try { _speechRec.abort(); } catch { } }
      }
      return;
    }

    // Sem instância ativa → inicia nova (limpa flags antes).
    _speechResetFlags();
    _speechStartNew(dotnet, opts, SR);
  };

  function _speechStartNew(dotnet, opts, SR) {
    let r;
    try {
      r = new SR();
    } catch (err) {
      _speechSetState('idle', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', 'ctor-failed'); } catch { }
      return;
    }
    r._dotnet = dotnet;
    r._opts = opts;
    r.continuous = opts.continuous ?? true;
    r.interimResults = opts.interimResults ?? false;
    r.maxAlternatives = Math.max(1, opts.maxAlternatives ?? 1);
    if (opts.language) r.lang = opts.language;

    // onstart: browser criou o recognizer. NÃO É AINDA "recording" — apenas
    // confirma que start() foi aceito. Mantém em 'connecting' até onaudiostart.
    r.onstart = () => {
      if (_speechState !== 'connecting') {
        _speechSetState('connecting', dotnet, opts);
      }
    };

    // onaudiostart: mic CAPTURANDO de fato. Esse é o gatilho real do Recording.
    // Limpa o watchdog porque chegamos no destino. Notifica OnStart (callback
    // user-facing que significa "estou ouvindo agora").
    r.onaudiostart = () => {
      _speechClearGuard();
      _speechResetFlags();
      _speechSetState('recording', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.startMethod || 'OnStart'); } catch { }
    };

    r.onresult = (e) => {
      if (!e.results || e.results.length === 0) return;
      const last = e.results[e.results.length - 1];
      const alt = last[0];
      try {
        dotnet.invokeMethodAsync(opts.resultMethod || 'OnResult',
          alt.transcript, last.isFinal, alt.confidence || 0);
      } catch { }
    };

    r.onerror = (e) => {
      const code = e.error || 'unknown';
      // Erros esperados que não devem virar 'error' state:
      //   no-speech — silêncio longo (Chrome/Edge dispara após ~10s sem fala)
      //   aborted   — stop programático (esperado, parte do fluxo normal)
      const isFatal = code !== 'no-speech' && code !== 'aborted';

      // Auto-retry pra erros transient no Edge (Microsoft Service é flaky).
      // Condições: erro retryable + ainda temos retries + user NÃO pediu stop.
      const isRetryableNet = (code === 'network' || code === 'audio-capture');
      if (isRetryableNet && _speechRetryCount < SPEECH_MAX_RETRIES && !_speechUserStop) {
        _speechRetryCount++;
        _speechRetrying = true;           // bloqueia idle no finalize
        _speechPending = { dotnet, opts };// agenda restart
        // Não notifica error pro C#, não muda state — fica em 'connecting'
        return;
      }

      _speechClearGuard();
      if (isFatal) _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', code); } catch { }
    };

    // Cleanup compartilhado: chamado em onend real OU watchdog defensivo.
    let _ended = false;
    const finalize = () => {
      if (_ended) return;
      _ended = true;
      _speechClearGuard();
      if (_speechRec === r) _speechRec = null;

      // CASO 1: User pediu stop — força idle, NÃO processa pending de retry.
      if (_speechUserStop) {
        _speechSetState('idle', dotnet, opts);
        try { dotnet.invokeMethodAsync(opts.endMethod || 'OnEnd'); } catch { }
        _speechResetFlags();
        _speechPending = null;
        return;
      }

      // CASO 2: Retry em andamento — MANTÉM em connecting, NÃO notifica OnEnd.
      // Estado público continua estável; só dispara o restart silencioso.
      if (_speechRetrying && _speechPending) {
        const pending = _speechPending;
        _speechPending = null;
        // Estado fica em 'connecting' (transição feita no onerror foi none)
        setTimeout(() => {
          _speechLastToggleAt = 0;
          ns.speechToggle(pending.dotnet, pending.opts);
        }, SPEECH_PENDING_DELAY_MS);
        return;
      }

      // CASO 3: Fim normal — vai pra idle + notifica OnEnd.
      _speechSetState('idle', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.endMethod || 'OnEnd'); } catch { }

      // Pending de OUTRO componente (cross-component switch)? Despacha.
      if (_speechPending && _speechRec === null) {
        const pending = _speechPending;
        _speechPending = null;
        setTimeout(() => {
          _speechLastToggleAt = 0;
          ns.speechToggle(pending.dotnet, pending.opts);
        }, SPEECH_PENDING_DELAY_MS);
      }
    };

    r.onend = () => {
      if (_speechRec !== r && _ended) return;
      finalize();
    };

    _speechRec = r;
    // Vai DIRETO pra 'connecting' (sem estado intermediário 'starting').
    // Se já estávamos em 'connecting' (retry path), no-op via guard interno.
    if (_speechState !== 'connecting') {
      _speechSetState('connecting', dotnet, opts);
    }

    // Watchdog: se onaudiostart não dispara em SPEECH_CONNECT_TIMEOUT_MS,
    // o reconhecedor está travado (permissão pendente, audio device sumiu,
    // ou Microsoft Service inalcançável após retries).
    _speechStartGuard = setTimeout(() => {
      if (_speechState !== 'connecting' || _speechRec !== r) return;
      _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', 'connect-timeout'); } catch { }
      try { r.abort(); } catch { /* abort dispara onend */ }
      setTimeout(() => { if (_ended) return; finalize(); }, 500);
    }, SPEECH_CONNECT_TIMEOUT_MS);

    try {
      r.start();
    } catch (err) {
      _speechClearGuard();
      _speechRec = null;
      const code = (err && err.name === 'InvalidStateError') ? 'invalid-state' : (err && err.name) || String(err);
      _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', code); } catch { }
      setTimeout(() => {
        if (_speechRec === null && _speechState === 'error') {
          _speechSetState('idle', dotnet, opts);
        }
      }, 200);
    }
  }

  ns.speechStop = function (dotnet) {
    if (!_speechRec) return;
    if (dotnet && _speechRec._dotnet && _speechRec._dotnet._id !== dotnet._id) return;
    if (_speechState === 'recording' || _speechState === 'connecting') {
      _speechUserStop = true;
      _speechRetrying = false;
      _speechPending = null;
      _speechRetryCount = SPEECH_MAX_RETRIES;
      _speechSetState('stopping', _speechRec._dotnet, _speechRec._opts);
    }
    const target = _speechRec;
    try { target.stop(); } catch { try { target.abort(); } catch { } }

    if (_isEdge) {
      setTimeout(() => {
        if (_speechRec === target && _speechState === 'stopping') {
          try { target.abort(); } catch { }
        }
      }, 1500);
    }
  };

  // ─── OmniFabMenu — outside-click + Esc handlers ────────────────────────────
  // FAB menu precisa fechar quando user clica fora ou pressiona Esc. Faz em JS
  // pra evitar circuit roundtrip a cada keypress (Blazor Server). Listeners
  // são registrados quando o menu abre, removidos quando fecha ou disposed.
  //
  // CONVENÇÃO: usamos o elemento DOM como chave. Cada element guarda seus
  // próprios handlers em __tvsFabMenu pra cleanup correto.
  //
  // SEM rAF DEFER: registramos os listeners SÍNCRONAMENTE. O fluxo é seguro
  // porque OnAfterRenderAsync do Blazor (que chama essa função) só dispara
  // DEPOIS do click event chain completar — então o click de "abrir" não
  // bubble pro nosso novo listener. rAF deferral causaria leak em fast-click
  // (close vinha antes do rAF disparar; addEventListener acabava órfão).
  ns.fabMenuOpen = function (element, dotnet, opts) {
    if (!element || !dotnet) return;
    // Cleanup defensivo se já estava aberto (re-open sem fechar antes).
    ns.fabMenuClose(element);

    const data = { dotnet, opts: opts || {} };

    if (data.opts.closeOnOutsideClick !== false) {
      data.clickHandler = function (e) {
        // element.contains(e.target) cobre o toggle button (vive dentro do
        // mesmo wrapper), então click no FAB nunca conta como "outside".
        if (!element.contains(e.target)) {
          try { dotnet.invokeMethodAsync('CloseAsync'); } catch { }
        }
      };
      // Capture phase (true) garante que pegamos o click ANTES de outros
      // listeners (importante pra menus aninhados / popovers).
      document.addEventListener('click', data.clickHandler, true);
    }

    if (data.opts.closeOnEsc !== false) {
      data.keyHandler = function (e) {
        if (e.key === 'Escape') {
          e.stopPropagation();
          try { dotnet.invokeMethodAsync('CloseAsync'); } catch { }
        }
      };
      document.addEventListener('keydown', data.keyHandler);
    }

    element.__tvsFabMenu = data;
  };

  ns.fabMenuClose = function (element) {
    if (!element || !element.__tvsFabMenu) return;
    const data = element.__tvsFabMenu;
    if (data.clickHandler) {
      document.removeEventListener('click', data.clickHandler, true);
    }
    if (data.keyHandler) {
      document.removeEventListener('keydown', data.keyHandler);
    }
    delete element.__tvsFabMenu;
  };

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

  // ─── DataGrid column resize ───────────────────────────────────────────
  // Mirrors Radzen's mechanism: the header handle's mousedown calls into C#,
  // which calls this. We grab the <col> for the column, then live-update its
  // width on mousemove and report the final width back on mouseup. Width lives
  // on a single <col> element (via <colgroup>), so one node changes per frame
  // instead of every cell.
  ns.gridStartColumnResize = function (colId, dotnetRef, index, startClientX, minWidth) {
    const col = document.getElementById(colId);
    if (!col) return;
    const startWidth = col.getBoundingClientRect().width;
    const min = minWidth || 40;
    let lastWidth = startWidth;

    const move = (e) => {
      lastWidth = Math.max(min, startWidth + (e.clientX - startClientX));
      col.style.width = lastWidth + 'px';
    };
    const up = () => {
      document.removeEventListener('mousemove', move);
      document.removeEventListener('mouseup', up);
      document.body.style.cursor = '';
      document.body.classList.remove('omni-grid-resizing');
      try { dotnetRef.invokeMethodAsync('OnColumnResized', index, Math.round(lastWidth)); } catch { }
    };

    document.body.style.cursor = 'col-resize';
    document.body.classList.add('omni-grid-resizing');
    document.addEventListener('mousemove', move);
    document.addEventListener('mouseup', up);
  };

  // ─── Gantt left-pane column resize ────────────────────────────────────
  // The pane (paneId) carries a CSS custom property per column (varName);
  // header AND body cells read it via var(), so updating the single property
  // resizes the whole column live. We also grow/shrink the pane's own width so
  // it keeps hugging its columns during the drag (no overflow into the
  // timeline). On mouseup the final width is reported to C#.
  ns.ganttStartColumnResize = function (paneId, headCellId, varName, dotnetRef, index, startClientX, minWidth) {
    var pane = document.getElementById(paneId);
    var cell = document.getElementById(headCellId);
    if (!pane || !cell) return;
    var startColWidth = cell.getBoundingClientRect().width;
    var startPaneWidth = pane.getBoundingClientRect().width;
    var min = minWidth || 60;
    var lastColWidth = startColWidth;

    var move = function (e) {
      lastColWidth = Math.max(min, startColWidth + (e.clientX - startClientX));
      var applied = lastColWidth - startColWidth; // clamped delta
      pane.style.setProperty(varName, lastColWidth + 'px');
      var newPane = startPaneWidth + applied;
      pane.style.width = newPane + 'px';
      pane.style.flexBasis = newPane + 'px';
    };
    var up = function () {
      document.removeEventListener('mousemove', move);
      document.removeEventListener('mouseup', up);
      document.body.style.cursor = '';
      document.body.classList.remove('omni-grid-resizing');
      try { dotnetRef.invokeMethodAsync('OnGanttColumnResized', index, Math.round(lastColWidth)); } catch { }
    };

    document.body.style.cursor = 'col-resize';
    document.body.classList.add('omni-grid-resizing');
    document.addEventListener('mousemove', move);
    document.addEventListener('mouseup', up);
  };

  // ─── HTML editor (contenteditable WYSIWYG) ────────────────────────────
  // Engine built on document.execCommand (deprecated but universal). Sync to
  // .NET is the native `input` event for typing + the execCommand return value
  // for toolbar commands. Selection is stashed on the element so toolbar popups
  // that steal focus can restore it before running a command.
  function omniEditorState(ref) {
    var q = function (c) { try { return document.queryCommandState(c); } catch (e) { return false; } };
    var v = function (c) { try { return document.queryCommandValue(c); } catch (e) { return ''; } };
    var en = function (c) { try { return document.queryCommandEnabled(c); } catch (e) { return false; } };
    return {
      html: ref ? ref.innerHTML : null,
      bold: q('bold'), italic: q('italic'), underline: q('underline'), strikeThrough: q('strikeThrough'),
      justifyLeft: q('justifyLeft'), justifyCenter: q('justifyCenter'), justifyRight: q('justifyRight'),
      insertOrderedList: q('insertOrderedList'), insertUnorderedList: q('insertUnorderedList'),
      subscript: q('subscript'), superscript: q('superscript'),
      formatBlock: (v('formatBlock') || '').toLowerCase(),
      undo: en('undo'), redo: en('redo'), unlink: en('unlink')
    };
  }

  function omniDecodeNumericEntities(s) {
    return s.replace(/&#(\d{1,7});|&#[xX]([0-9a-fA-F]{1,6});/g, function (m, dec, hex) {
      var code = dec ? parseInt(dec, 10) : parseInt(hex, 16);
      if (!(code > 0) || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF)) return m;
      try { return String.fromCodePoint(code); } catch (e) { return m; }
    });
  }
  function omniSafeDataImage(p) {
    return p.indexOf('data:image/png') === 0 || p.indexOf('data:image/jpeg') === 0
      || p.indexOf('data:image/jpg') === 0 || p.indexOf('data:image/gif') === 0
      || p.indexOf('data:image/webp') === 0;
  }
  function omniSanitizeEditorHtml(html) {
    // Decode numeric entities (&#9;/&#x09;) the browser resolves before the scheme check;
    // loop (capped) to defeat double-encoding. Regex is best-effort — prefer a real
    // sanitizer (DOMPurify / DOMParser) for hostile input.
    for (var i = 0; i < 5; i++) { var d = omniDecodeNumericEntities(html); if (d === html) break; html = d; }
    html = html.replace(/[\u0000-\u001F\u007F]/g, ' ');
    html = html.replace(/<(script|style|iframe|object|embed|form|svg|math)\b[\s\S]*?<\/\1\s*>/gi, '');
    html = html.replace(/<\/?(script|style|iframe|object|embed|form|svg|math|link|meta|base)\b[^>]*>/gi, '');
    html = html.replace(/[\s/]on\w+\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, '');
    html = html.replace(/(href|src)\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, function (m, attr, val) {
      var quote = (val && (val[0] === '"' || val[0] === "'")) ? val[0] : '';
      var inner = quote ? val.slice(1, -1) : val;
      var probe = inner.replace(/\s/g, '').toLowerCase();
      var bad = probe.indexOf('javascript:') === 0 || probe.indexOf('vbscript:') === 0
        || (probe.indexOf('data:') === 0 && !omniSafeDataImage(probe));
      return bad ? (attr + '=' + quote + '#' + quote) : m;
    });
    return html;
  }

  ns.htmlEditorCreate = function (ref, dotnetRef, shortcuts) {
    if (!ref) return;
    var selTimer = null;
    ref.__omniInput = function () { try { dotnetRef.invokeMethodAsync('OnContentChanged', ref.innerHTML); } catch (e) { } };
    ref.__omniKeydown = function (e) {
      var key = '';
      if (e.ctrlKey || e.metaKey) key += 'Ctrl+';
      if (e.altKey) key += 'Alt+';
      if (e.shiftKey) key += 'Shift+';
      key += (e.code || '').replace('Key', '').replace('Digit', '').replace('Numpad', '');
      if (shortcuts && shortcuts.indexOf(key) > -1) {
        e.preventDefault();
        try { dotnetRef.invokeMethodAsync('OnShortcut', key); } catch (er) { }
      }
    };
    ref.__omniSel = function () {
      if (document.activeElement !== ref) return;
      if (selTimer) clearTimeout(selTimer);
      selTimer = setTimeout(function () {
        try { dotnetRef.invokeMethodAsync('OnSelectionChanged', omniEditorState(ref)); } catch (e) { }
      }, 120);
    };
    ref.__omniPaste = function (e) {
      var dt = e.clipboardData;
      if (!dt) return;
      var html = dt.getData('text/html');
      if (html) {
        e.preventDefault();
        var s = html.indexOf('<!--StartFragment-->'), en = html.indexOf('<!--EndFragment-->');
        if (s > -1 && en > s) html = html.substring(s + 20, en);
        document.execCommand('insertHTML', false, omniSanitizeEditorHtml(html));
      }
    };
    ref.addEventListener('input', ref.__omniInput);
    ref.addEventListener('keydown', ref.__omniKeydown);
    ref.addEventListener('paste', ref.__omniPaste);
    document.addEventListener('selectionchange', ref.__omniSel);
    try { document.execCommand('styleWithCSS', false, true); } catch (e) { }
  };

  ns.htmlEditorExec = function (ref, name, value) {
    if (ref && document.activeElement !== ref) ref.focus();
    try { document.execCommand(name, false, value); } catch (e) { }
    return omniEditorState(ref);
  };

  ns.htmlEditorState = function (ref) { return omniEditorState(ref); };
  ns.htmlEditorPrompt = function (message, defaultValue) { return window.prompt(message, defaultValue || ''); };

  // ─── Chat ─────────────────────────────────────────────────────────────
  ns.chatScrollToBottom = function (el) { if (el) el.scrollTop = el.scrollHeight; };
  ns.chatIsNearBottom = function (el, threshold) {
    if (!el) return true;
    return el.scrollHeight - el.scrollTop - el.clientHeight <= (threshold || 60);
  };
  // Enter sends (Shift+Enter inserts a newline). Clears the textarea synchronously
  // and reports the typed value so the value never lags behind a fast keystroke.
  ns.chatEnterToSend = function (ta, dotnetRef) {
    if (!ta) return;
    ta.__omniEnter = function (e) {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        var v = ta.value;
        ta.value = '';
        try { dotnetRef.invokeMethodAsync('OnEnterPressed', v); } catch (er) { }
      }
    };
    ta.addEventListener('keydown', ta.__omniEnter);
  };
  ns.chatDetach = function (ta) { if (ta && ta.__omniEnter) ta.removeEventListener('keydown', ta.__omniEnter); };

  // ─── Security code (OTP/PIN cells) ────────────────────────────────────
  ns.securityCodeInit = function (container, dotnetRef, isNumeric) {
    if (!container) return;
    var inputs = [].slice.call(container.querySelectorAll('.omni-seccode-input'));
    function report() {
      var v = inputs.map(function (i) { return i.value; }).join('');
      try { dotnetRef.invokeMethodAsync('OnCodeChanged', v); } catch (e) { }
    }
    inputs.forEach(function (inp, idx) {
      inp.__omniInput = function () {
        var v = inp.value;
        if (v.length > 1) v = v.slice(-1);           // keep only the last typed char
        if (isNumeric && v && !/[0-9]/.test(v)) v = ''; // reject non-digits
        inp.value = v;
        report();
        if (v && idx < inputs.length - 1) inputs[idx + 1].focus();
      };
      inp.__omniKeydown = function (e) {
        if (e.key === 'Backspace' && inp.value === '' && idx > 0) {
          e.preventDefault();
          inputs[idx - 1].focus();
          inputs[idx - 1].value = '';
          report();
        } else if (e.key === 'ArrowLeft' && idx > 0) {
          e.preventDefault(); inputs[idx - 1].focus();
        } else if (e.key === 'ArrowRight' && idx < inputs.length - 1) {
          e.preventDefault(); inputs[idx + 1].focus();
        }
      };
      inp.__omniPaste = function (e) {
        e.preventDefault();
        var data = ((e.clipboardData || window.clipboardData).getData('text') || '');
        for (var i = 0; i < inputs.length && i < data.length; i++) {
          var ch = data[i];
          inputs[i].value = (isNumeric && !/[0-9]/.test(ch)) ? '' : ch;
        }
        report();
        var last = Math.min(data.length, inputs.length) - 1;
        if (last >= 0) inputs[Math.min(last, inputs.length - 1)].focus();
      };
      inp.addEventListener('input', inp.__omniInput);
      inp.addEventListener('keydown', inp.__omniKeydown);
      inp.addEventListener('paste', inp.__omniPaste);
    });
  };
  ns.securityCodeSet = function (container, value) {
    if (!container) return;
    value = value || '';
    [].slice.call(container.querySelectorAll('.omni-seccode-input'))
      .forEach(function (inp, i) { inp.value = value[i] || ''; });
  };
  ns.securityCodeFocus = function (container) {
    if (!container) return;
    var first = container.querySelector('.omni-seccode-input:not([disabled])');
    if (first) first.focus();
  };
  ns.securityCodeDestroy = function (container) {
    if (!container) return;
    [].slice.call(container.querySelectorAll('.omni-seccode-input')).forEach(function (inp) {
      if (inp.__omniInput) inp.removeEventListener('input', inp.__omniInput);
      if (inp.__omniKeydown) inp.removeEventListener('keydown', inp.__omniKeydown);
      if (inp.__omniPaste) inp.removeEventListener('paste', inp.__omniPaste);
    });
  };
  ns.htmlEditorSetHtml = function (ref, html) { if (ref) ref.innerHTML = html == null ? '' : omniSanitizeEditorHtml(html); };
  ns.htmlEditorGetHtml = function (ref) { return ref ? ref.innerHTML : ''; };
  ns.htmlEditorFocus = function (ref) { if (ref) ref.focus(); };

  ns.htmlEditorSaveSelection = function (ref) {
    if (!ref) return;
    var sel = getSelection();
    if (sel.rangeCount > 0) {
      var r = sel.getRangeAt(0);
      if (ref.contains(r.commonAncestorContainer)) ref.__omniRange = r;
    }
  };
  ns.htmlEditorRestoreSelection = function (ref) {
    if (!ref || !ref.__omniRange) return;
    var r = ref.__omniRange;
    delete ref.__omniRange;
    ref.focus();
    var sel = getSelection();
    sel.removeAllRanges();
    sel.addRange(r);
  };

  ns.htmlEditorDestroy = function (ref) {
    if (!ref) return;
    ref.removeEventListener('input', ref.__omniInput);
    ref.removeEventListener('keydown', ref.__omniKeydown);
    ref.removeEventListener('paste', ref.__omniPaste);
    document.removeEventListener('selectionchange', ref.__omniSel);
  };

  // ─── Carousel (scroll-snap slideshow) ──────────────────────────────────
  // Scrolls a track so the slide at `index` becomes active. The track is closed
  // over (never re-marshaled per call). Resolves the slide by index from the
  // container's children.
  //   duration === 0    → instant jump (no animation)
  //   duration < 0/null → smooth scroll at the default duration (~400ms)
  //   duration > 0      → smooth scroll, final position guaranteed after `duration`
  // NOTES:
  //  • `scroll-snap-type: x mandatory` snaps a programmatic scroll straight back
  //    to the origin, so snap is disabled for the move and re-enabled on the
  //    target (itself a snap point).
  //  • Native smooth scroll only animates while the page is actively painting;
  //    in a background tab / headless context it is a no-op. So after the
  //    animation window we ALWAYS force the final position instantly — the slide
  //    changes everywhere, and animates smoothly where the frame loop is alive.
  function carouselTween(container, index, duration) {
    if (!container) return;
    var el = container.children[index];
    if (!el) return;
    var target = el.offsetLeft;
    container.style.scrollSnapType = 'none';
    if (duration === 0) {
      container.style.scrollBehavior = 'auto';
      container.scrollLeft = target;
      container.style.scrollBehavior = '';
      container.style.scrollSnapType = '';
      return;
    }
    var dwell = (duration && duration > 0) ? duration : 400;
    container.style.scrollBehavior = 'smooth';
    try { container.scrollTo({ left: target, behavior: 'smooth' }); } catch (e) { container.scrollLeft = target; }
    if (container.__omniSnapT) clearTimeout(container.__omniSnapT);
    container.__omniSnapT = setTimeout(function () {
      container.style.scrollBehavior = 'auto';
      container.scrollLeft = target;          // guarantee the final position
      container.style.scrollBehavior = '';
      container.style.scrollSnapType = '';    // re-enable snap, already aligned
      container.__omniSnapT = null;
    }, dwell + 60);
  }

  // Standalone variant (used by tests / direct callers).
  ns.carouselScrollToItem = function (container, index, duration) { carouselTween(container, index, duration); };

  // Watches the track for user scroll/swipe and reports the centred slide index
  // back to .NET (debounced). The returned object also exposes scrollTo(index,
  // duration). Both the listener and scrollTo resolve the LIVE track element via
  // its stable data-omni-cid attribute every time — Blazor may replace the <ul>
  // across prerender/hydration, which would otherwise leave a stale (detached)
  // reference that scrolls nothing visible.
  ns.carouselCreate = function (container, dotnetRef) {
    if (!container) return null;
    var cid = container.getAttribute && container.getAttribute('data-omni-cid');
    function track() {
      if (cid) { var live = document.querySelector('[data-omni-cid="' + cid + '"]'); if (live) return live; }
      return container;
    }
    var t = null;
    function handler() {
      if (t) clearTimeout(t);
      t = setTimeout(function () {
        var trk = track();
        var kids = trk.children;
        if (!kids.length) return;
        var w = kids[0].offsetWidth;
        if (!w) return;
        var index = Math.round(trk.scrollLeft / w);
        if (index < 0) index = 0;
        if (index >= kids.length) index = kids.length - 1;
        try { dotnetRef.invokeMethodAsync('OnScroll', index); } catch (e) { }
      }, 100);
    }
    var listenEl = track();
    listenEl.addEventListener('scroll', handler, { passive: true });
    return {
      scrollTo: function (index, duration) { carouselTween(track(), index, duration); },
      dispose: function () { listenEl.removeEventListener('scroll', handler); if (t) clearTimeout(t); }
    };
  };

  // ——— Parallax ————————————————————————————————————————————————————————————
  // Fallback p/ browsers sem CSS scroll-driven animations + parallax de mouse.
  // Escreve UMA custom property por cena por frame (--omni-parallax-progress 0..1)
  // que as camadas consomem via calc()+translate3d no CSS. Um único rAF
  // compartilhado + IntersectionObserver — cenas fora da viewport não "tickam".
  ns.parallax = (function () {
    function supportsNative() {
      try { return !!(window.CSS && CSS.supports && CSS.supports('animation-timeline', 'view()')); }
      catch (e) { return false; }
    }

    var scenes = new Set();    // cenas que precisam de progresso via JS
    var visible = new Set();   // subconjunto atualmente na viewport
    var io = null;
    var scheduled = false;
    var rafId = 0;
    var sharedAttached = false;

    function ensureIO() {
      if (io) return;
      io = new IntersectionObserver(function (entries) {
        for (var i = 0; i < entries.length; i++) {
          if (entries[i].isIntersecting) visible.add(entries[i].target);
          else visible.delete(entries[i].target);
        }
        requestTick();
      }, { rootMargin: '0px' });
    }

    function requestTick() {
      if (scheduled) return;
      scheduled = true;
      rafId = requestAnimationFrame(tick);
    }

    function tick() {
      scheduled = false;
      var vh = window.innerHeight || document.documentElement.clientHeight || 0;
      visible.forEach(function (scene) {
        var r = scene.getBoundingClientRect();
        var denom = vh + r.height;
        var p = denom > 0 ? (vh - r.top) / denom : 0.5;
        p = p < 0 ? 0 : (p > 1 ? 1 : p);
        scene.style.setProperty('--omni-parallax-progress', p.toFixed(4));
      });
    }

    var onScrollResize = function () { requestTick(); };
    function attachShared() {
      if (sharedAttached) return;
      sharedAttached = true;
      window.addEventListener('scroll', onScrollResize, { passive: true });
      window.addEventListener('resize', onScrollResize, { passive: true });
    }
    function detachShared() {
      if (!sharedAttached) return;
      sharedAttached = false;
      window.removeEventListener('scroll', onScrollResize, { passive: true });
      window.removeEventListener('resize', onScrollResize, { passive: true });
      if (rafId) cancelAnimationFrame(rafId);
      scheduled = false;
    }

    function create(scene, opts) {
      opts = opts || {};
      if (!scene) return { dispose: function () {} };

      // a11y: prefers-reduced-motion → no-op (o CSS também força transform:none).
      try {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
          return { dispose: function () {} };
        }
      } catch (e) {}

      var needScroll = !opts.native;   // JS dirige o progresso só quando o CSS não dirige
      var wantMouse = !!opts.mouse;
      var fine = false;
      try { fine = window.matchMedia('(hover: hover) and (pointer: fine)').matches; } catch (e) {}

      var onMove = null, onLeave = null;

      if (needScroll) {
        ensureIO();
        scenes.add(scene);
        io.observe(scene);
        attachShared();
        requestTick();
      }

      if (wantMouse && fine) {
        onMove = function (ev) {
          var r = scene.getBoundingClientRect();
          var mx = r.width ? ((ev.clientX - r.left) / r.width - 0.5) * 2 : 0;
          var my = r.height ? ((ev.clientY - r.top) / r.height - 0.5) * 2 : 0;
          scene.style.setProperty('--omni-parallax-mx', mx.toFixed(4));
          scene.style.setProperty('--omni-parallax-my', my.toFixed(4));
        };
        onLeave = function () {
          scene.style.setProperty('--omni-parallax-mx', '0');
          scene.style.setProperty('--omni-parallax-my', '0');
        };
        scene.addEventListener('pointermove', onMove, { passive: true });
        scene.addEventListener('pointerleave', onLeave, { passive: true });
      }

      return {
        dispose: function () {
          if (needScroll) {
            scenes.delete(scene);
            visible.delete(scene);
            if (io) io.unobserve(scene);
            if (scenes.size === 0) detachShared();
          }
          if (onMove) scene.removeEventListener('pointermove', onMove, { passive: true });
          if (onLeave) scene.removeEventListener('pointerleave', onLeave, { passive: true });
        }
      };
    }

    return { supportsNative: supportsNative, create: create };
  })();
})();
