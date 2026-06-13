// omni-diagram.js — high-frequency interaction module for OmniDiagramCanvas.
//
// Lazy ES module (imported via IJSObjectReference), deliberately OUTSIDE the
// global Omni.js bundle: pan/zoom/drag/connect run pointer loops that must
// never round-trip through the Blazor Server circuit. JS owns every gesture;
// .NET is notified only on COMMIT (mouse-up / debounced wheel):
//   JsCommitViewport(x, y, zoom)
//   JsSelect(nodeIds[], edgeId)
//   JsMoveNodes([{ id, x, y }])
//   JsConnect(sourceId, sourcePort, targetId)
//   JsExternalDrop(payload, worldX, worldY)
//   JsDeleteRequested()
//
// Geometry constants mirror Omni.Blazor.Models.DiagramGeometry — keep in sync.

const MIN_ZOOM = 0.25;
const MAX_ZOOM = 2.2;
const GRID = 22;

export function init(rootId, dotnet, opts) {
  const root = document.getElementById(rootId);
  if (!root) throw new Error(`omni-diagram: element #${rootId} not found`);
  const state = {
    root,
    dotnet,
    readOnly: !!opts.readOnly,
    payloadFormat: opts.payloadFormat || 'application/x-omni-diagram',
    vp: { x: opts.x ?? 40, y: opts.y ?? 30, zoom: opts.zoom ?? 0.85 },
    space: false,
    pan: null,        // { sx, sy, ox, oy }
    drag: null,       // { ids:[{el,id,x0,y0}], sx, sy, moved }
    marquee: null,    // { x0, y0, el }
    pending: null,    // { sourceId, sourcePort, ax, ay, path, hot }
    wheelTimer: 0,
    disposed: false,
  };

  const world = root.querySelector('.omni-diagram-world');
  const grid = root.querySelector('.omni-diagram-grid');
  const svg = root.querySelector('.omni-diagram-svg');

  // ───────────────────────── viewport ─────────────────────────

  function applyViewport() {
    const { x, y, zoom } = state.vp;
    world.style.transform = `translate(${x}px, ${y}px) scale(${zoom})`;
    grid.style.backgroundPosition = `${x}px ${y}px`;
    grid.style.backgroundSize = `${GRID * zoom}px ${GRID * zoom}px`;
    const label = root.querySelector('.omni-diagram-zoomlabel');
    if (label) label.textContent = Math.round(zoom * 100) + '%';
    updateMinimapView();
  }

  function commitViewport() {
    const { x, y, zoom } = state.vp;
    state.dotnet.invokeMethodAsync('JsCommitViewport', x, y, zoom);
  }

  function screenToWorld(cx, cy) {
    const r = root.getBoundingClientRect();
    return {
      x: (cx - r.left - state.vp.x) / state.vp.zoom,
      y: (cy - r.top - state.vp.y) / state.vp.zoom,
    };
  }

  function updateMinimapView() {
    const mm = root.querySelector('.omni-diagram-minimap svg');
    const view = root.querySelector('.omni-diagram-mmview');
    if (!mm || !view) return;
    const minX = parseFloat(mm.dataset.minx), minY = parseFloat(mm.dataset.miny);
    const scale = parseFloat(mm.dataset.scale);
    if (!isFinite(minX) || !isFinite(scale)) return;
    const r = root.getBoundingClientRect();
    const vx = -state.vp.x / state.vp.zoom, vy = -state.vp.y / state.vp.zoom;
    view.setAttribute('x', (vx - minX) * scale);
    view.setAttribute('y', (vy - minY) * scale);
    view.setAttribute('width', (r.width / state.vp.zoom) * scale);
    view.setAttribute('height', (r.height / state.vp.zoom) * scale);
  }

  // ───────────────────────── geometry from DOM ─────────────────────────
  // Anchors are read from the live DOM so any node template works: a port's
  // world position is derived from its bounding rect, undoing the transform.

  function worldRectOf(el) {
    const r = el.getBoundingClientRect();
    const base = root.getBoundingClientRect();
    const z = state.vp.zoom;
    return {
      x: (r.left - base.left - state.vp.x) / z,
      y: (r.top - base.top - state.vp.y) / z,
      w: r.width / z,
      h: r.height / z,
    };
  }

  function portAnchor(portEl) {
    const r = worldRectOf(portEl);
    return { x: r.x + r.w / 2, y: r.y + r.h / 2 };
  }

  function bezier(a, b) {
    const dx = Math.max(45, Math.abs(b.x - a.x) * 0.5);
    return `M ${a.x} ${a.y} C ${a.x + dx} ${a.y}, ${b.x - dx} ${b.y}, ${b.x} ${b.y}`;
  }

  function nodeEl(id) {
    return world.querySelector(`[data-dgnode="${CSS.escape(id)}"]`);
  }

  function refreshEdgesFor(ids) {
    const set = new Set(ids);
    svg.querySelectorAll('path[data-dgedge]').forEach((p) => {
      const s = p.dataset.source, t = p.dataset.target;
      if (!set.has(s) && !set.has(t)) return;
      const sEl = nodeEl(s), tEl = nodeEl(t);
      if (!sEl || !tEl) return;
      const out = sEl.querySelector(`[data-port="${CSS.escape(p.dataset.sourceport)}"]`);
      const inp = tEl.querySelector('[data-port-in]');
      if (!out || !inp) return;
      const d = bezier(portAnchor(out), portAnchor(inp));
      p.setAttribute('d', d);
      const hit = p.previousElementSibling;
      if (hit && hit.classList.contains('omni-diagram-edgehit')) hit.setAttribute('d', d);
    });
  }

  // ───────────────────────── selection helpers ─────────────────────────

  function selectedIds() {
    return [...world.querySelectorAll('[data-dgnode].omni-diagram-node-selected')]
      .map((el) => el.dataset.dgnode);
  }

  function commitSelect(nodeIds, edgeId) {
    state.dotnet.invokeMethodAsync('JsSelect', nodeIds, edgeId ?? null);
  }

  // ───────────────────────── pointer gestures ─────────────────────────

  function onMouseDown(e) {
    if (state.disposed) return;

    // pan: middle/right button or space+left, anywhere
    if (e.button === 1 || e.button === 2 || (e.button === 0 && state.space)) {
      state.pan = { sx: e.clientX, sy: e.clientY, ox: state.vp.x, oy: state.vp.y };
      root.style.cursor = 'grabbing';
      e.preventDefault();
      return;
    }
    if (e.button !== 0) return;

    const portOut = e.target.closest('[data-port]');
    const portIn = e.target.closest('[data-port-in]');
    const node = e.target.closest('[data-dgnode]');
    const edge = e.target.closest('[data-dgedgegrp]');

    if (portOut && node && !state.readOnly) {
      e.stopPropagation();
      const a = portAnchor(portOut);
      const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('class', 'omni-diagram-edge omni-diagram-edge-pending');
      path.setAttribute('d', bezier(a, a));
      svg.appendChild(path);
      state.pending = { sourceId: node.dataset.dgnode, sourcePort: portOut.dataset.port, a, path, hot: null };
      return;
    }

    if (node && !portIn) {
      e.stopPropagation();
      const id = node.dataset.dgnode;
      const additive = e.shiftKey || e.ctrlKey || e.metaKey;
      let ids = selectedIds();
      if (!ids.includes(id)) {
        ids = additive ? [...ids, id] : [id];
        ids.forEach((nid) => nodeEl(nid)?.classList.add('omni-diagram-node-selected'));
        if (!additive) {
          world.querySelectorAll('.omni-diagram-node-selected').forEach((el) => {
            if (!ids.includes(el.dataset.dgnode)) el.classList.remove('omni-diagram-node-selected');
          });
        }
        commitSelect(ids, null);
      }
      if (state.readOnly) return;
      const start = { sx: e.clientX, sy: e.clientY };
      state.drag = {
        ...start,
        moved: false,
        ids: ids.map((nid) => {
          const el = nodeEl(nid);
          return el && { el, id: nid, x0: parseFloat(el.dataset.x), y0: parseFloat(el.dataset.y) };
        }).filter(Boolean),
      };
      return;
    }

    if (edge && !state.readOnly) {
      e.stopPropagation();
      commitSelect([], edge.dataset.dgedgegrp);
      return;
    }

    // background → marquee (or simple clear in read-only)
    if (e.target === root || e.target.classList.contains('omni-diagram-grid') || e.target === world || e.target === svg) {
      if (state.readOnly) { commitSelect([], null); return; }
      const w = screenToWorld(e.clientX, e.clientY);
      const el = document.createElement('div');
      el.className = 'omni-diagram-marquee';
      root.appendChild(el);
      state.marquee = { x0: w.x, y0: w.y, x1: w.x, y1: w.y, el, additive: e.shiftKey };
      if (!e.shiftKey) commitSelect([], null);
    }
  }

  function onMouseMove(e) {
    if (state.pan) {
      state.vp.x = state.pan.ox + (e.clientX - state.pan.sx);
      state.vp.y = state.pan.oy + (e.clientY - state.pan.sy);
      applyViewport();
      return;
    }
    if (state.drag) {
      const dx = (e.clientX - state.drag.sx) / state.vp.zoom;
      const dy = (e.clientY - state.drag.sy) / state.vp.zoom;
      if (!state.drag.moved && Math.abs(dx) < 2 && Math.abs(dy) < 2) return;
      state.drag.moved = true;
      state.drag.dx = dx;
      state.drag.dy = dy;
      for (const n of state.drag.ids) {
        n.el.style.left = (n.x0 + dx) + 'px';
        n.el.style.top = (n.y0 + dy) + 'px';
      }
      refreshEdgesFor(state.drag.ids.map((n) => n.id));
      return;
    }
    if (state.marquee) {
      const w = screenToWorld(e.clientX, e.clientY);
      state.marquee.x1 = w.x; state.marquee.y1 = w.y;
      const { x0, y0, x1, y1, el } = state.marquee;
      const z = state.vp.zoom;
      el.style.left = (Math.min(x0, x1) * z + state.vp.x) + 'px';
      el.style.top = (Math.min(y0, y1) * z + state.vp.y) + 'px';
      el.style.width = (Math.abs(x1 - x0) * z) + 'px';
      el.style.height = (Math.abs(y1 - y0) * z) + 'px';
      return;
    }
    if (state.pending) {
      const w = screenToWorld(e.clientX, e.clientY);
      state.pending.path.setAttribute('d', bezier(state.pending.a, w));
      const under = document.elementFromPoint(e.clientX, e.clientY);
      const inPort = under && under.closest && under.closest('[data-port-in]');
      const hotEl = inPort && inPort.closest('[data-dgnode]');
      const hotId = hotEl && hotEl.dataset.dgnode !== state.pending.sourceId ? hotEl.dataset.dgnode : null;
      if (state.pending.hot && state.pending.hot !== hotId) {
        nodeEl(state.pending.hot)?.querySelector('[data-port-in]')?.classList.remove('omni-diagram-port-hot');
      }
      if (hotId) inPort.classList.add('omni-diagram-port-hot');
      state.pending.hot = hotId;
    }
  }

  function onMouseUp() {
    if (state.pan) {
      state.pan = null;
      root.style.cursor = '';
      commitViewport();
    }
    if (state.drag) {
      if (state.drag.moved) {
        const moves = state.drag.ids.map((n) => ({
          id: n.id,
          x: Math.round(n.x0 + state.drag.dx),
          y: Math.round(n.y0 + state.drag.dy),
        }));
        state.dotnet.invokeMethodAsync('JsMoveNodes', moves);
      }
      state.drag = null;
    }
    if (state.marquee) {
      const { x0, y0, x1, y1, el, additive } = state.marquee;
      el.remove();
      const minX = Math.min(x0, x1), maxX = Math.max(x0, x1);
      const minY = Math.min(y0, y1), maxY = Math.max(y0, y1);
      if (maxX - minX > 6 || maxY - minY > 6) {
        const hit = [...world.querySelectorAll('[data-dgnode]')].filter((n) => {
          const r = worldRectOf(n);
          return r.x < maxX && r.x + r.w > minX && r.y < maxY && r.y + r.h > minY;
        }).map((n) => n.dataset.dgnode);
        const ids = additive ? [...new Set([...selectedIds(), ...hit])] : hit;
        commitSelect(ids, null);
      }
      state.marquee = null;
    }
    if (state.pending) {
      const { sourceId, sourcePort, hot, path } = state.pending;
      path.remove();
      if (hot) {
        nodeEl(hot)?.querySelector('[data-port-in]')?.classList.remove('omni-diagram-port-hot');
        state.dotnet.invokeMethodAsync('JsConnect', sourceId, sourcePort, hot);
      }
      state.pending = null;
    }
  }

  function onWheel(e) {
    e.preventDefault();
    const r = root.getBoundingClientRect();
    const mx = e.clientX - r.left, my = e.clientY - r.top;
    const factor = e.deltaY < 0 ? 1.12 : 0.89;
    const z2 = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, state.vp.zoom * factor));
    const wx = (mx - state.vp.x) / state.vp.zoom;
    const wy = (my - state.vp.y) / state.vp.zoom;
    state.vp = { zoom: z2, x: mx - wx * z2, y: my - wy * z2 };
    applyViewport();
    clearTimeout(state.wheelTimer);
    state.wheelTimer = setTimeout(commitViewport, 160);
  }

  function onKeyDown(e) {
    const typing = /INPUT|TEXTAREA|SELECT/.test(e.target.tagName) || e.target.isContentEditable;
    if (e.code === 'Space' && !typing) {
      state.space = true;
      if (!state.pan) root.style.cursor = 'grab';
    }
    if ((e.key === 'Delete' || e.key === 'Backspace') && !typing && !state.readOnly
      && root.contains(document.activeElement)) {
      e.preventDefault();
      state.dotnet.invokeMethodAsync('JsDeleteRequested');
    }
  }

  function onKeyUp(e) {
    if (e.code === 'Space') {
      state.space = false;
      if (!state.pan) root.style.cursor = '';
    }
  }

  // external drop (palette → canvas)
  function onDragOver(e) {
    if (state.readOnly) return;
    if ([...e.dataTransfer.types].includes(state.payloadFormat)) e.preventDefault();
  }

  function onDrop(e) {
    if (state.readOnly) return;
    const payload = e.dataTransfer.getData(state.payloadFormat);
    if (!payload) return;
    e.preventDefault();
    const w = screenToWorld(e.clientX, e.clientY);
    state.dotnet.invokeMethodAsync('JsExternalDrop', payload, w.x, w.y);
  }

  // minimap click → jump
  function onMinimapDown(e) {
    const mm = e.target.closest('.omni-diagram-minimap');
    if (!mm) return;
    e.stopPropagation();
    const svgEl = mm.querySelector('svg');
    const minX = parseFloat(svgEl.dataset.minx), minY = parseFloat(svgEl.dataset.miny);
    const scale = parseFloat(svgEl.dataset.scale);
    const r = mm.getBoundingClientRect();
    const wx = minX + (e.clientX - r.left) / scale;
    const wy = minY + (e.clientY - r.top) / scale;
    const base = root.getBoundingClientRect();
    state.vp.x = base.width / 2 - wx * state.vp.zoom;
    state.vp.y = base.height / 2 - wy * state.vp.zoom;
    applyViewport();
    commitViewport();
  }

  root.addEventListener('mousedown', onMouseDown);
  root.addEventListener('mousedown', onMinimapDown, true);
  root.addEventListener('wheel', onWheel, { passive: false });
  root.addEventListener('dragover', onDragOver);
  root.addEventListener('drop', onDrop);
  root.addEventListener('contextmenu', (e) => e.preventDefault());
  window.addEventListener('mousemove', onMouseMove);
  window.addEventListener('mouseup', onMouseUp);
  window.addEventListener('keydown', onKeyDown);
  window.addEventListener('keyup', onKeyUp);

  applyViewport();

  return {
    setViewport(x, y, zoom) {
      state.vp = { x, y, zoom: Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom)) };
      applyViewport();
    },
    setReadOnly(ro) { state.readOnly = !!ro; },
    refreshOverlay() { updateMinimapView(); },
    measure() {
      const r = root.getBoundingClientRect();
      return { w: r.width, h: r.height };
    },
    dispose() {
      state.disposed = true;
      root.removeEventListener('mousedown', onMouseDown);
      root.removeEventListener('mousedown', onMinimapDown, true);
      root.removeEventListener('wheel', onWheel);
      root.removeEventListener('dragover', onDragOver);
      root.removeEventListener('drop', onDrop);
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseup', onMouseUp);
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
    },
  };
}
