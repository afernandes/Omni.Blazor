// Omni.Blazor scroll and measurement services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

  // Scroll an element (by ElementReference or CSS selector) into view. Used by
  // keyboard-navigated lists to follow the active item.
  ns.scrollIntoView = function (target, options) {
    try {
      const el = typeof target === "string" ? document.querySelector(target) : target;
      if (!el) return;
      const opts = typeof options === "string" ? { block: options } : (options || {});
      el.scrollIntoView({
        behavior: opts.behavior || "auto",
        block: opts.block || "nearest",
        inline: opts.inline || "nearest"
      });
    } catch (e) { /* ignore */ }
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


export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
