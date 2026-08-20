// Omni.Blazor navigation, hotkey and drag services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

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

  // Horizontal auto-scroll while dragging a Kanban card near the board edges.
  ns.kanbanAutoScroll = function (board) {
    if (!board || board._omniAutoScroll) return;
    const edge = 64;
    const speed = 18;
    const handler = function (event) {
      const rect = board.getBoundingClientRect();
      if (event.clientX < rect.left + edge) board.scrollLeft -= speed;
      else if (event.clientX > rect.right - edge) board.scrollLeft += speed;
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
        if (element.contains(e.target)) return;
        // NÃO despacha o close daqui. Capture phase roda ANTES do click chegar
        // ao alvo, então fechar agora escreve `false` no campo do consumidor
        // ANTES do @onclick dele rodar — e um handler que DERIVA o novo valor do
        // atual (`_menuOpen = !_menuOpen`, o toggle externo documentado no
        // OmniFabMenu) leria um valor que o usuário nunca viu e reabriria o
        // menu. Os dois updates se anulam e o clique vira no-op.
        //
        // Mesma disciplina do dispatcher de click-outside em omni-overlay.js:
        // adia pro fim do turno (o click já foi despachado pro .NET, e SignalR
        // preserva a ordem de envio, então o handler do consumidor é processado
        // primeiro) e reconfere que o registro ainda é este antes de invocar.
        // Se o próprio clique já fechou o menu, CloseAsync é no-op idempotente.
        setTimeout(function () {
          if (element.__tvsFabMenu !== data) return;
          dotnet.invokeMethodAsync('CloseAsync').catch(function () { });
        }, 0);
      };
      // Capture phase (true) garante que OBSERVAMOS o click ANTES de outros
      // listeners (importante pra menus aninhados / popovers) — quem é adiado
      // é só o despacho pro .NET, não a detecção.
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

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
