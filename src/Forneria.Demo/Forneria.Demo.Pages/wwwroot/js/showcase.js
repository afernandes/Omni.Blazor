// Lightweight Razor/C# syntax highlighter for the showcase code panels.
// Not a full parser — tokenises comments, strings, Razor expressions,
// tags and keywords with a single alternation regex. Good enough to make
// snippets readable without pulling in Prism/highlight.js.
window.omniShowcase = (function () {
    'use strict';

    function esc(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    const TOKEN = new RegExp(
        // 1: comments — @* *@ , <!-- -->, // line
        '(@\\*[\\s\\S]*?\\*@|<!--[\\s\\S]*?-->|//[^\\n]*)' +
        // 2: strings — "..." or '...'
        '|("(?:[^"\\\\]|\\\\.)*"|\'(?:[^\'\\\\]|\\\\.)*\')' +
        // 3: razor — @@ or @directive/@expression start
        '|(@@|@[A-Za-z_(][\\w.]*)' +
        // 4: tags — <Tag , </Tag , /> , >
        '|(</?[A-Za-z][\\w.:-]*|/?>)' +
        // 5: C# keywords
        '|(\\b(?:true|false|null|var|new|public|private|protected|internal|static|' +
        'async|await|return|if|else|foreach|for|while|in|int|long|decimal|double|' +
        'float|string|bool|void|class|record|struct|enum|using|namespace|get|set|' +
        'this|sealed|override|virtual|partial|readonly|const|nameof|switch|case)\\b)',
        'g');

    function highlight(code) {
        let out = '', last = 0, m;
        TOKEN.lastIndex = 0;
        while ((m = TOKEN.exec(code)) !== null) {
            out += esc(code.slice(last, m.index));
            if (m[1])      out += '<span class="omni-tok-comment">' + esc(m[1]) + '</span>';
            else if (m[2]) out += '<span class="omni-tok-string">'  + esc(m[2]) + '</span>';
            else if (m[3]) out += '<span class="omni-tok-razor">'   + esc(m[3]) + '</span>';
            else if (m[4]) out += '<span class="omni-tok-tag">'     + esc(m[4]) + '</span>';
            else if (m[5]) out += '<span class="omni-tok-keyword">' + esc(m[5]) + '</span>';
            last = m.index + m[0].length;
        }
        out += esc(code.slice(last));
        return out;
    }

    return {
        // Highlight a single <code> element in place. Idempotent — re-running
        // is a no-op (guarded by the data-omni-hl flag).
        highlightElement: function (el) {
            if (!el || el.dataset.tvsHl === '1') return;
            const raw = el.textContent || '';
            el.innerHTML = highlight(raw);
            el.dataset.tvsHl = '1';
        }
    };
})();
