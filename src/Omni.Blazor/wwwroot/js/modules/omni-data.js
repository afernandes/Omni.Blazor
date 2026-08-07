// Omni.Blazor data component and rich editor services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};


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

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
