// Omni.Blazor input and gesture services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

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

  const decimalDigitMap = (function () {
    const map = new Map();
    for (let digit = 0; digit <= 9; digit++) map.set(String(digit), String(digit));

    // Build mappings for every decimal numbering system supported by the
    // browser (arab, arabext, deva, fullwide, mathematical digits, etc.).
    // Intl is the source of truth for each glyph's numeric value; this avoids
    // maintaining a brittle table of Unicode code-point ranges.
    let numberingSystems = [];
    try {
      if (typeof Intl.supportedValuesOf === 'function') {
        numberingSystems = Intl.supportedValuesOf('numberingSystem');
      }
    } catch { }

    // Older engines may not expose supportedValuesOf, but their active locale
    // still needs to work.
    numberingSystems.push(undefined);
    for (const numberingSystem of numberingSystems) {
      try {
        const formatter = new Intl.NumberFormat(
          navigator.language,
          { useGrouping: false, numberingSystem });
        for (let digit = 0; digit <= 9; digit++) {
          const glyph = Array.from(formatter.format(digit))
            .find(character => /\p{Nd}/u.test(character));
          if (glyph) map.set(glyph, String(digit));
        }
      } catch { }
    }
    return map;
  })();

  function normalizeDecimalDigits(value) {
    return Array.from(String(value ?? ''))
      .map(character => decimalDigitMap.get(character) ?? character)
      .join('');
  }

  // Numeric input key filter — ported from Radzen.numericKeyPress.
  // Blocks any character that isn't a Unicode digit, minus sign, or (when the
  // bound type isn't integer) the culture's decimal separator. Translates the
  // numpad decimal key into the culture's separator. Control keys and meta/
  // ctrl/alt combinations pass through so copy/paste/select-all still work.
  function numericKeyPress(e, isInteger, decimalSeparator, autoDecimalSeparator) {
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

    if (autoDecimalSeparator) {
      if (decimalDigitMap.has(k) || k === '-') return;
      e.preventDefault();
      return;
    }

    if (/\p{Nd}/u.test(k) || k === '-' || (!isInteger && k === decimalSeparator)) return;
    e.preventDefault();
  }

  function formatFixedDecimalInput(value, decimalSeparator, scale) {
    const source = normalizeDecimalDigits(value);
    const negative = source.trimStart().startsWith('-');
    let digits = source.replace(/[^0-9]/g, '');
    digits = digits.replace(/^0+(?=\d)/, '');

    if (scale === 0) return (negative ? '-' : '') + (digits || '0');

    const padded = (digits || '0').padStart(scale + 1, '0');
    const whole = padded.slice(0, -scale);
    const fraction = padded.slice(-scale);
    return (negative ? '-' : '') + whole + decimalSeparator + fraction;
  }

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

  function numericOnPaste(e, min, max, autoDecimalSeparator, decimalSeparator, scale) {
    if (!e.clipboardData) return;
    var value = e.clipboardData.getData('text');
    if (!value) { e.preventDefault(); return; }
    value = String(value).trim();

    if (autoDecimalSeparator) {
      value = formatFixedDecimalInput(value, decimalSeparator, scale)
        .replace(decimalSeparator, '.');
    } else {
      var parts = new Intl.NumberFormat(navigator.language).formatToParts(1234567.89);
      var group = ',', dec = '.';
      for (var i = 0; i < parts.length; i++) {
        if (parts[i].type === 'group') group = parts[i].value;
        if (parts[i].type === 'decimal') dec = parts[i].value;
      }
      value = value.replace(/[  ]/g, ' ');
      if (group) value = value.split(group).join('');
      if (dec !== '.') value = value.split(dec).join('.');
    }
    if (!/^[+-]?(\d+(\.\d*)?|\.\d+)$/.test(value)) { e.preventDefault(); return; }
    var n = Number(value);
    if (!isFinite(n)) { e.preventDefault(); return; }
    if (min != null && n < min) { e.preventDefault(); return; }
    if (max != null && n > max) { e.preventDefault(); return; }
  }

  const numericRegistry = new WeakMap();

  ns.numericAttach = function (el, isInteger, decimalSeparator, min, max, autoDecimalSeparator, scale) {
    if (!el) return;
    ns.numericDetach(el);

    const onKeyDown = function (e) {
      numericKeyPress(e, isInteger, decimalSeparator, autoDecimalSeparator);
    };
    const onPaste = function (e) {
      numericOnPaste(e, min, max, autoDecimalSeparator, decimalSeparator, scale);
    };
    const onInput = function () {
      if (!autoDecimalSeparator) return;
      const formatted = formatFixedDecimalInput(el.value, decimalSeparator, scale);
      if (el.value !== formatted) el.value = formatted;
      const caret = formatted.length;
      try { el.setSelectionRange(caret, caret); } catch {}
    };

    el.addEventListener('keydown', onKeyDown);
    el.addEventListener('paste', onPaste);
    el.addEventListener('input', onInput);
    numericRegistry.set(el, { onKeyDown, onPaste, onInput });
  };

  ns.numericDetach = function (el) {
    if (!el) return;
    const entry = numericRegistry.get(el);
    if (!entry) return;

    el.removeEventListener('keydown', entry.onKeyDown);
    el.removeEventListener('paste', entry.onPaste);
    el.removeEventListener('input', entry.onInput);
    numericRegistry.delete(el);
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

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
