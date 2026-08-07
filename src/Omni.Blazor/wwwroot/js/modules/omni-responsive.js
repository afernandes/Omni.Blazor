// Omni.Blazor responsive and appearance services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

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


export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
