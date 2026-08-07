// Omni.Blazor core browser services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

ns.confirm = function (message) {
  return window.confirm(message == null ? '' : String(message));
};

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


  // Focus first focusable inside a container (used by dialogs)
  ns.focusFirst = function (el) {
    if (!el) return;
    const focusable = el.querySelector(
      'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
    );
    if (focusable) focusable.focus();
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

  // Focus an element by id — used by OmniKanban to keep focus on a card after a
  // keyboard move re-renders the board. No-op if the element is gone.
  ns.focusElement = function (id, preventScroll) {
    if (!id) return;
    const root = document.getElementById(id);
    if (!root) return;
    const selector = 'input:not([disabled]),textarea:not([disabled]),select:not([disabled]),button:not([disabled]),[tabindex]:not([tabindex="-1"])';
    const el = typeof root.focus === 'function' && root.matches(selector)
      ? root
      : root.querySelector(selector);
    if (el) { try { el.focus({ preventScroll: preventScroll !== false }); } catch {} }
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

  // Trigger a browser download from a .NET stream. The stream is consumed
  // incrementally across interop, avoiding a second managed string/byte[] copy.
  ns.downloadStream = async function (filename, contentStreamReference, mime) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: mime || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    try {
      a.href = url; a.download = filename || 'download.bin';
      document.body.appendChild(a); a.click();
    } finally {
      setTimeout(() => { URL.revokeObjectURL(url); a.remove(); }, 0);
    }
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

export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
