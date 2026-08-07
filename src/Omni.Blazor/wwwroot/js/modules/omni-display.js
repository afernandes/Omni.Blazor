// Omni.Blazor display, carousel, parallax and signature services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

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

  ns.signaturePad = (function () {
    function create(canvas, dotNet, options) {
      if (!canvas) return null;

      var state = {
        canvas: canvas,
        context: canvas.getContext('2d'),
        dotNet: dotNet,
        options: options || {},
        strokes: [],
        activeStroke: null,
        initialImage: null,
        baseValue: options && options.initialValue ? options.initialValue : null,
        currentValue: options && options.initialValue ? options.initialValue : null,
        loadGeneration: 0,
        disposed: false
      };

      function resize() {
        if (state.disposed) return;
        var rect = canvas.getBoundingClientRect();
        var ratio = Math.max(1, window.devicePixelRatio || 1);
        var width = Math.max(1, Math.round(rect.width * ratio));
        var height = Math.max(1, Math.round(rect.height * ratio));
        if (canvas.width !== width || canvas.height !== height) {
          canvas.width = width;
          canvas.height = height;
        }
        state.context.setTransform(ratio, 0, 0, ratio, 0, 0);
        render();
      }

      function render() {
        var context = state.context;
        var width = canvas.clientWidth || 1;
        var height = canvas.clientHeight || 1;
        context.clearRect(0, 0, width, height);
        context.fillStyle = state.options.backgroundColor || '#ffffff';
        context.fillRect(0, 0, width, height);

        if (state.initialImage) {
          context.drawImage(state.initialImage, 0, 0, width, height);
        }

        context.strokeStyle = state.options.strokeColor || '#111827';
        context.fillStyle = context.strokeStyle;
        context.lineWidth = Math.max(0.5, Number(state.options.strokeWidth) || 2);
        context.lineCap = 'round';
        context.lineJoin = 'round';

        for (var i = 0; i < state.strokes.length; i++) {
          drawStroke(context, state.strokes[i]);
        }
        if (state.activeStroke) drawStroke(context, state.activeStroke);
      }

      function drawStroke(context, stroke) {
        if (!stroke || stroke.length === 0) return;
        if (stroke.length === 1) {
          context.beginPath();
          context.arc(stroke[0].x, stroke[0].y, context.lineWidth / 2, 0, Math.PI * 2);
          context.fill();
          return;
        }

        context.beginPath();
        context.moveTo(stroke[0].x, stroke[0].y);
        for (var i = 1; i < stroke.length; i++) {
          context.lineTo(stroke[i].x, stroke[i].y);
        }
        context.stroke();
      }

      function point(event) {
        var rect = canvas.getBoundingClientRect();
        return { x: event.clientX - rect.left, y: event.clientY - rect.top };
      }

      function canDraw(event) {
        return !state.disposed
          && !state.options.disabled
          && !state.options.readOnly
          && (event.isPrimary !== false);
      }

      function onPointerDown(event) {
        if (!canDraw(event)) return;
        event.preventDefault();
        state.activeStroke = [point(event)];
        canvas.setPointerCapture(event.pointerId);
        render();
      }

      function onPointerMove(event) {
        if (!state.activeStroke || !canDraw(event)) return;
        event.preventDefault();
        state.activeStroke.push(point(event));
        render();
      }

      function finishStroke(event) {
        if (!state.activeStroke) return;
        if (canDraw(event)) state.activeStroke.push(point(event));
        state.strokes.push(state.activeStroke);
        state.activeStroke = null;
        render();
        notify();
      }

      function onPointerCancel() {
        state.activeStroke = null;
        render();
      }

      function hasContent() {
        return !!state.initialImage || state.strokes.length > 0;
      }

      function escapeXml(value) {
        return String(value)
          .replace(/&/g, '&amp;')
          .replace(/"/g, '&quot;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;');
      }

      function exportSvg() {
        var width = canvas.clientWidth || 1;
        var height = canvas.clientHeight || 1;
        var background = escapeXml(state.options.backgroundColor || '#ffffff');
        var stroke = escapeXml(state.options.strokeColor || '#111827');
        var strokeWidth = Math.max(0.5, Number(state.options.strokeWidth) || 2);
        var parts = [
          '<svg xmlns="http://www.w3.org/2000/svg" width="', width,
          '" height="', height, '" viewBox="0 0 ', width, ' ', height, '">',
          '<rect width="100%" height="100%" fill="', background, '"/>'
        ];

        if (state.baseValue) {
          parts.push('<image href="', escapeXml(state.baseValue),
            '" width="100%" height="100%" preserveAspectRatio="none"/>');
        }

        for (var i = 0; i < state.strokes.length; i++) {
          var current = state.strokes[i];
          if (current.length === 1) {
            parts.push('<circle cx="', current[0].x, '" cy="', current[0].y,
              '" r="', strokeWidth / 2, '" fill="', stroke, '"/>');
            continue;
          }
          var points = [];
          for (var j = 0; j < current.length; j++) {
            points.push(current[j].x + ',' + current[j].y);
          }
          parts.push('<polyline points="', points.join(' '), '" fill="none" stroke="',
            stroke, '" stroke-width="', strokeWidth,
            '" stroke-linecap="round" stroke-linejoin="round"/>');
        }
        parts.push('</svg>');
        return 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(parts.join(''));
      }

      function snapshot() {
        if (!hasContent()) {
          state.currentValue = null;
          return { value: null, isEmpty: true };
        }
        var format = Number(state.options.format);
        var value = format === 2
          ? exportSvg()
          : canvas.toDataURL(format === 1 ? 'image/jpeg' : 'image/png',
              Math.max(0, Math.min(1, Number(state.options.quality) || 0.92)));
        state.currentValue = value;
        return { value: value, isEmpty: false };
      }

      function notify() {
        var current = snapshot();
        if (state.dotNet) {
          state.dotNet.invokeMethodAsync('OnSignatureChangedAsync', current.value, current.isEmpty)
            .catch(function () {});
        }
        return current;
      }

      function clear() {
        state.strokes.length = 0;
        state.activeStroke = null;
        state.initialImage = null;
        state.baseValue = null;
        state.currentValue = null;
        state.options.initialValue = null;
        render();
        return { value: null, isEmpty: true };
      }

      function undo() {
        if (state.strokes.length > 0) state.strokes.pop();
        else {
          state.initialImage = null;
          state.baseValue = null;
          state.currentValue = null;
          state.options.initialValue = null;
        }
        render();
        return snapshot();
      }

      function update(nextOptions) {
        if (!nextOptions) return;
        var incomingValue = nextOptions.initialValue || null;
        var isExternalValue = incomingValue !== state.currentValue;
        state.options = nextOptions;
        // A value equal to our last export is the normal Blazor binding echo.
        // Only a genuinely external value replaces the captured strokes.
        if (isExternalValue) {
          state.strokes.length = 0;
          state.activeStroke = null;
          state.initialImage = null;
          state.baseValue = incomingValue;
          state.currentValue = incomingValue;
          loadInitial(incomingValue);
        }
        state.options.initialValue = state.baseValue;
        render();
      }

      function loadInitial(value) {
        var generation = ++state.loadGeneration;
        if (!value) {
          state.initialImage = null;
          render();
          return;
        }
        var image = new Image();
        image.onload = function () {
          if (state.disposed || generation !== state.loadGeneration) return;
          state.initialImage = image;
          render();
        };
        image.src = value;
      }

      canvas.addEventListener('pointerdown', onPointerDown);
      canvas.addEventListener('pointermove', onPointerMove);
      canvas.addEventListener('pointerup', finishStroke);
      canvas.addEventListener('pointercancel', onPointerCancel);
      var resizeObserver = typeof ResizeObserver === 'function'
        ? new ResizeObserver(resize)
        : null;
      if (resizeObserver) resizeObserver.observe(canvas);
      window.addEventListener('resize', resize, { passive: true });
      loadInitial(state.options.initialValue);
      resize();

      return {
        update: update,
        clear: clear,
        undo: undo,
        exportValue: snapshot,
        dispose: function () {
          if (state.disposed) return;
          state.disposed = true;
          canvas.removeEventListener('pointerdown', onPointerDown);
          canvas.removeEventListener('pointermove', onPointerMove);
          canvas.removeEventListener('pointerup', finishStroke);
          canvas.removeEventListener('pointercancel', onPointerCancel);
          window.removeEventListener('resize', resize, { passive: true });
          if (resizeObserver) resizeObserver.disconnect();
          state.strokes.length = 0;
          state.activeStroke = null;
          state.initialImage = null;
          state.baseValue = null;
          state.currentValue = null;
          state.dotNet = null;
        }
      };
    }

    return { create: create };
  })();

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
