// ============================================================
// Omni Box — keyboard-first launcher for PDV
// Sintaxe:
//   101            → flavor at default size G
//   2x101          → 2 of flavor
//   101/102        → meio a meio
//   G 101/102      → meio a meio Grande
//   G 101/102 .C   → meio a meio Grande, borda Catupiry (direto)
//   201            → produto não-pizza (adiciona direto)
//   @nome          → busca cliente
//   #7             → vincula mesa
// ============================================================

const { useState: useOB, useEffect: useOBFx, useMemo: useOBMemo, useRef: useOBRef } = React;

const SIZE_ALIAS = { B: 'M', M: 'M', BR: 'M', BROTO: 'M', G: 'G', GR: 'G', GRANDE: 'G', F: 'GG', FAM: 'GG', FAMILIA: 'GG', GG: 'GG' };
const SIZE_LABEL = { M: 'Broto', G: 'Grande', GG: 'Família' };

// Parse a command — returns { qty, sizeHint, flavorCodes, bordaHint, productCode, customerQuery, tableNum, raw }
function parseCommand(raw) {
  const r = (raw || '').trim();
  if (!r) return null;

  // Customer @
  if (r.startsWith('@')) return { kind: 'customer', customerQuery: r.slice(1).trim() };
  // Table #
  if (r.startsWith('#')) return { kind: 'table', tableNum: r.slice(1).trim() };

  // Strip leading qty (Nx)
  let body = r;
  let qty = 1;
  const qtyMatch = body.match(/^(\d+)\s*[xX*]\s*(.*)$/);
  if (qtyMatch) { qty = parseInt(qtyMatch[1], 10) || 1; body = qtyMatch[2]; }

  // Strip leading size hint (G, M, GG, B, F)
  let sizeHint = null;
  const sizeMatch = body.match(/^(GG|G|M|B|F|Br|Gr|Fa|Fam|Familia|Broto|Grande)\s+(.+)$/i);
  if (sizeMatch) {
    const k = sizeMatch[1].toUpperCase();
    sizeHint = SIZE_ALIAS[k] || null;
    if (sizeHint) body = sizeMatch[2];
  }

  // Strip trailing borda (.C, .Ch, .Cr)
  let bordaHint = null;
  const bordaMatch = body.match(/^(.+?)\s*\.(\w+)\s*$/);
  if (bordaMatch) {
    const al = bordaMatch[2].toLowerCase();
    const found = BORDAS.find(b => b.alias.includes(al));
    if (found) { bordaHint = found.id; body = bordaMatch[1].trim(); }
  }

  // Meio a meio (101/102 or even 101/102/103)
  if (body.includes('/')) {
    const codes = body.split('/').map(s => s.trim()).filter(Boolean);
    const allFlavors = codes.every(c => FLAVORS.find(f => f.code === c));
    if (allFlavors && codes.length >= 2) {
      return { kind: 'pizza', qty, sizeHint, flavorCodes: codes, bordaHint, raw: r };
    }
  }

  // Single code or partial
  if (/^\d+$/.test(body)) {
    const flavor = FLAVORS.find(f => f.code === body);
    if (flavor) return { kind: 'pizza', qty, sizeHint, flavorCodes: [body], bordaHint, raw: r };
    const product = PRODUCTS.find(p => p.code === body);
    if (product) return { kind: 'product', qty, productCode: body, raw: r };
    return { kind: 'partial', qty, query: body, raw: r };
  }

  return { kind: 'partial', qty, query: body, sizeHint, bordaHint, raw: r };
}

// Fuzzy search — by code prefix or name substring
function searchAll(query) {
  if (!query) return [];
  const q = query.toLowerCase();
  const results = [];
  // pizzas (flavors at G default)
  for (const f of FLAVORS) {
    const codeMatch = f.code.startsWith(q.replace(/\D/g, '')) && /\d/.test(q);
    const nameMatch = f.name.toLowerCase().includes(q);
    if (codeMatch || nameMatch) {
      results.push({
        kind: 'flavor', code: f.code, label: 'Pizza Grande ' + f.name,
        flavor: f, price: f.prices.G, score: codeMatch ? 0 : 1
      });
    }
  }
  for (const p of PRODUCTS.filter(x => x.code && !x.configurable)) {
    const codeMatch = p.code.startsWith(q.replace(/\D/g, '')) && /\d/.test(q);
    const nameMatch = p.name.toLowerCase().includes(q);
    if (codeMatch || nameMatch) {
      results.push({ kind: 'product', code: p.code, label: p.name, product: p, price: p.price, score: codeMatch ? 0 : 1 });
    }
  }
  return results.sort((a, b) => a.score - b.score || a.code.localeCompare(b.code)).slice(0, 6);
}

// Highlight matched chars in label
function Highlight({ text, query }) {
  if (!query) return text;
  const q = query.toLowerCase();
  const i = text.toLowerCase().indexOf(q);
  if (i < 0) return text;
  return (
    <>
      {text.slice(0, i)}
      <mark>{text.slice(i, i + q.length)}</mark>
      {text.slice(i + q.length)}
    </>
  );
}

// Compute price for a pizza command — sizeHint required, defaults to G
function pizzaPrice(flavorCodes, sizeKey, bordaId) {
  const flavors = flavorCodes.map(c => FLAVORS.find(f => f.code === c)).filter(Boolean);
  if (!flavors.length) return 0;
  const sk = sizeKey || 'G';
  const max = Math.max(...flavors.map(f => f.prices[sk] || 0));
  const borda = BORDAS.find(b => b.id === bordaId);
  return max + (borda?.price || 0);
}

function pizzaDisplayName(flavorCodes, sizeKey, bordaId) {
  const flavors = flavorCodes.map(c => FLAVORS.find(f => f.code === c)).filter(Boolean);
  if (flavors.length === 1) return `Pizza ${SIZE_LABEL[sizeKey]} · ${flavors[0].name}`;
  return `Pizza ${SIZE_LABEL[sizeKey]} · ${flavors.map(f => '½ ' + f.name).join(' + ')}`;
}

// ============================================================
// INLINE PANEL — pizza configuration
// ============================================================
function InlinePanel({ cmd, onConfirm, onCancel, panelRef }) {
  const flavors = cmd.flavorCodes.map(c => FLAVORS.find(f => f.code === c)).filter(Boolean);
  const isHalf = flavors.length >= 2;

  const [size, setSize] = useOB(cmd.sizeHint || 'G');
  const [borda, setBorda] = useOB(cmd.bordaHint || 'none');
  const [group, setGroup] = useOB(cmd.sizeHint ? 'borda' : 'size');

  const total = pizzaPrice(cmd.flavorCodes, size, borda) * cmd.qty;

  const sizes = ['M', 'G', 'GG'];
  const bordasList = BORDAS;

  // Keyboard nav
  useOBFx(() => {
    const onKey = (e) => {
      if (e.key === 'Tab') {
        e.preventDefault();
        setGroup(g => g === 'size' ? 'borda' : 'size');
      } else if (e.key === 'Escape') {
        e.preventDefault(); onCancel();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        onConfirm({ size, borda });
      } else if (['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)) {
        e.preventDefault();
        const dir = (e.key === 'ArrowRight' || e.key === 'ArrowDown') ? 1 : -1;
        if (group === 'size') {
          const i = sizes.indexOf(size);
          setSize(sizes[(i + dir + sizes.length) % sizes.length]);
        } else {
          const ids = bordasList.map(b => b.id);
          const i = ids.indexOf(borda);
          setBorda(ids[(i + dir + ids.length) % ids.length]);
        }
      }
    };
    window.addEventListener('keydown', onKey, true);
    return () => window.removeEventListener('keydown', onKey, true);
  }, [size, borda, group, cmd]);

  return (
    <div className="omni-panel" ref={panelRef}>
      <div className="omni-panel-head">
        <div className="omni-panel-icon">🍕</div>
        <div className="omni-panel-title">
          {isHalf ? (
            <>
              <span>{flavors.map(f => `½ ${f.name} (${f.code})`).join('  +  ')}</span>
              <span className="omni-panel-tag">Meio a meio</span>
            </>
          ) : (
            <span>{flavors[0].name} ({flavors[0].code})</span>
          )}
        </div>
        {isHalf && <div className="omni-panel-sub">Preço: sabor mais caro</div>}
      </div>

      <div className="omni-panel-body">
        <div className={'omni-group ' + (group === 'size' ? 'focused' : '')}>
          <div className="omni-group-label">TAMANHO</div>
          <div className="omni-options">
            {sizes.map(s => {
              const pr = pizzaPrice(cmd.flavorCodes, s, borda);
              return (
                <button key={s}
                  className={'omni-opt ' + (size === s ? 'on' : '')}
                  onClick={() => { setSize(s); setGroup('size'); }}>
                  <div className="omni-opt-label">{SIZE_LABEL[s]}</div>
                  <div className="omni-opt-price">{BRL(pr)}</div>
                </button>
              );
            })}
          </div>
        </div>

        <div className={'omni-group ' + (group === 'borda' ? 'focused' : '')}>
          <div className="omni-group-label">BORDA</div>
          <div className="omni-options">
            {bordasList.map(b => (
              <button key={b.id}
                className={'omni-opt ' + (borda === b.id ? 'on' : '')}
                onClick={() => { setBorda(b.id); setGroup('borda'); }}>
                <div className="omni-opt-label">{b.name}</div>
                <div className="omni-opt-price">{b.price > 0 ? '+ ' + BRL(b.price) : (b.id === 'none' ? 'incluso' : BRL(0))}</div>
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="omni-panel-foot">
        <div className="omni-keyhints">
          <span><kbd>TAB</kbd> próximo</span>
          <span><kbd>↑↓←→</kbd> selecionar</span>
          <span><kbd>ESC</kbd> cancelar</span>
        </div>
        <button className="btn btn-primary btn-lg" onClick={() => onConfirm({ size, borda })}>
          <kbd className="kbd-on-primary">ENTER</kbd> Adicionar · {BRL(total)}
        </button>
      </div>
    </div>
  );
}

// ============================================================
// HELP CARD
// ============================================================
function OmniHelp({ onClose }) {
  return (
    <div className="omni-help">
      <div className="omni-help-head">
        <div><span style={{ fontSize: 16 }}>📖</span> Como usar o Omni Box</div>
        <button className="btn-ghost btn btn-icon" onClick={onClose}><Icon name="x" size={14} /></button>
      </div>
      <div className="omni-help-body">
        <div className="omni-help-section">
          <div className="omni-help-h">Sintaxe</div>
          <div className="omni-help-row"><code>101</code><span>Pizza Grande Calabresa (tamanho padrão)</span></div>
          <div className="omni-help-row"><code>2x101</code><span>2× Pizza Grande Calabresa</span></div>
          <div className="omni-help-row"><code>101/102</code><span>Meio a meio: Calabresa + 4 Queijos</span></div>
          <div className="omni-help-row"><code>G 101/102</code><span>Meio a meio Grande (só pede borda)</span></div>
          <div className="omni-help-row"><code>G 101/102 .C</code><span>Meio a meio Grande, Borda Catupiry (direto)</span></div>
          <div className="omni-help-row"><code>201</code><span>Coca-Cola 350ml (adiciona direto)</span></div>
          <div className="omni-help-row"><code>@carlos</code><span>Busca e vincula cliente</span></div>
          <div className="omni-help-row"><code>#7</code><span>Vincula pedido à Mesa 7</span></div>
        </div>
        <div className="omni-help-section">
          <div className="omni-help-h">Atalhos</div>
          <div className="omni-help-grid">
            <div><kbd>F1</kbd> Ajuda</div><div><kbd>F5</kbd> Pagamento</div>
            <div><kbd>F2</kbd> Desconto</div><div><kbd>F6</kbd> Cliente</div>
            <div><kbd>F3</kbd> Observação</div><div><kbd>F7</kbd> Mesa</div>
            <div><kbd>F4</kbd> Caixa</div><div><kbd>F9</kbd> Cancelar</div>
            <div><kbd>Del</kbd> Remover item</div><div><kbd>Ctrl+Z</kbd> Desfazer</div>
            <div><kbd>Ins</kbd> Foco Omni</div><div><kbd>End</kbd> Pagar</div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ============================================================
// OMNI BOX — main component
// ============================================================
function OmniBox({ onAdd, onCustomer, onTable, onPay, onShortcut, registerFocus }) {
  const [value, setValue] = useOB('');
  const [showHelp, setShowHelp] = useOB(false);
  const [focused, setFocused] = useOB(false);
  const [hi, setHi] = useOB(0); // highlighted suggestion index
  const [error, setError] = useOB('');
  const [shake, setShake] = useOB(false);
  const inputRef = useOBRef(null);
  const panelRef = useOBRef(null);

  const cmd = useOBMemo(() => parseCommand(value), [value]);
  const showSugg = !!value && (cmd?.kind === 'partial' || (!cmd && value.length > 0));
  const suggestions = useOBMemo(() => showSugg ? searchAll(cmd?.query || value) : [], [cmd, value, showSugg]);
  const showPanel = cmd && cmd.kind === 'pizza' && (!cmd.sizeHint || !cmd.bordaHint);
  const hasFullCommand = cmd && cmd.kind === 'pizza' && cmd.sizeHint && cmd.bordaHint;

  // Register focus method
  useOBFx(() => {
    if (registerFocus) registerFocus(() => inputRef.current?.focus());
  }, [registerFocus]);

  // Auto focus on mount
  useOBFx(() => { inputRef.current?.focus(); }, []);

  // Reset hi when query changes
  useOBFx(() => { setHi(0); }, [value]);

  const reset = () => { setValue(''); setError(''); inputRef.current?.focus(); };

  const flashError = (msg) => {
    setError(msg);
    setShake(true);
    setTimeout(() => setShake(false), 380);
    setTimeout(() => setError(''), 2200);
  };

  const commitProduct = (productCode, qty = 1) => {
    const p = PRODUCTS.find(x => x.code === productCode);
    if (!p) return;
    for (let i = 0; i < qty; i++) {
      onAdd({ id: p.id + '-' + Date.now() + '-' + i, pid: p.id, name: p.name, price: p.price, cat: p.cat, qty: 1 });
    }
  };

  const commitPizza = ({ flavorCodes, size, borda, qty }) => {
    const flavors = flavorCodes.map(c => FLAVORS.find(f => f.code === c));
    const price = pizzaPrice(flavorCodes, size, borda);
    const bordaDef = BORDAS.find(b => b.id === borda);
    const name = pizzaDisplayName(flavorCodes, size, borda);
    const notes = [];
    if (bordaDef && bordaDef.id !== 'none') notes.push('Borda ' + bordaDef.name);
    const basePrice = price - (bordaDef ? bordaDef.price : 0);
    for (let i = 0; i < qty; i++) {
      onAdd({
        id: 'pz-' + Date.now() + '-' + i, pid: 'pz-' + size,
        name, price, qty: 1, notes,
        config: { size, flavorCodes, bordaId: borda, extras: [], basePrice }
      });
    }
  };

  const handleEnter = () => {
    if (!cmd) return;
    if (cmd.kind === 'partial') {
      // pick highlighted suggestion
      const s = suggestions[hi];
      if (!s) { flashError(`"${value}" não encontrado. Tente um código ou nome.`); return; }
      if (s.kind === 'flavor') {
        setValue(s.code);
        return; // re-parse opens panel
      }
      commitProduct(s.code, cmd.qty);
      reset();
      return;
    }
    if (cmd.kind === 'product') {
      commitProduct(cmd.productCode, cmd.qty);
      window.__omniToast?.(`✓ ${PRODUCTS.find(p => p.code === cmd.productCode)?.name} · ${BRL(PRODUCTS.find(p => p.code === cmd.productCode)?.price)}`);
      reset(); return;
    }
    if (cmd.kind === 'pizza') {
      if (cmd.sizeHint && cmd.bordaHint) {
        commitPizza({ flavorCodes: cmd.flavorCodes, size: cmd.sizeHint, borda: cmd.bordaHint, qty: cmd.qty });
        const name = pizzaDisplayName(cmd.flavorCodes, cmd.sizeHint, cmd.bordaHint);
        const total = pizzaPrice(cmd.flavorCodes, cmd.sizeHint, cmd.bordaHint) * cmd.qty;
        window.__omniToast?.(`✓ ${name} · ${BRL(total)}`);
        reset(); return;
      }
      // else, panel will handle
      return;
    }
    if (cmd.kind === 'customer') {
      const found = CUSTOMERS.find(c => c.name.toLowerCase().includes(cmd.customerQuery.toLowerCase()));
      if (found) { onCustomer(found); reset(); }
      else flashError(`Cliente "${cmd.customerQuery}" não encontrado.`);
      return;
    }
    if (cmd.kind === 'table') {
      onTable(cmd.tableNum); reset(); return;
    }
  };

  const onKeyDown = (e) => {
    if (showSugg && suggestions.length) {
      if (e.key === 'ArrowDown') { e.preventDefault(); setHi(i => Math.min(i + 1, suggestions.length - 1)); return; }
      if (e.key === 'ArrowUp')   { e.preventDefault(); setHi(i => Math.max(i - 1, 0)); return; }
    }
    if (e.key === 'Enter') { e.preventDefault(); handleEnter(); return; }
    if (e.key === 'Escape') { e.preventDefault(); reset(); }
  };

  const panelConfirm = ({ size, borda }) => {
    commitPizza({ flavorCodes: cmd.flavorCodes, size, borda, qty: cmd.qty });
    const name = pizzaDisplayName(cmd.flavorCodes, size, borda);
    const total = pizzaPrice(cmd.flavorCodes, size, borda) * cmd.qty;
    window.__omniToast?.(`✓ ${name} · ${BRL(total)}`);
    reset();
  };

  // Rotating placeholder
  const PLACEHOLDERS = [
    'Digite código, nome ou use 101/102 para meio a meio…',
    '101 → Pizza Grande Calabresa',
    '101/102 → Meio a meio Calabresa + 4 Queijos',
    '2x201 → 2 Coca-Colas',
    'G 101/102 .C → Meio a meio Grande, Borda Catupiry',
  ];
  const [phIdx, setPhIdx] = useOB(0);
  useOBFx(() => {
    if (value) return;
    const t = setInterval(() => setPhIdx(i => (i + 1) % PLACEHOLDERS.length), 3500);
    return () => clearInterval(t);
  }, [value]);

  const slashHint = value.includes('/') && cmd?.kind === 'partial';

  return (
    <div className={'omni-wrap ' + (focused ? 'is-focused' : '')}>
      {focused && <div className="omni-mobile-backdrop" onClick={() => inputRef.current?.blur()} />}
      <div className={'omni-box ' + (shake ? 'shake' : '') + ' ' + (error ? 'err' : '')}>
        {focused && (
          <button className="omni-mobile-close"
            onMouseDown={(e) => { e.preventDefault(); inputRef.current?.blur(); setValue(''); setShowSugg(false); }}
            type="button" aria-label="Fechar">
            <Icon name="x" size={16} />
          </button>
        )}
        <span className="omni-ico"><Icon name="search" size={18} /></span>
        <input
          ref={inputRef}
          className="omni-input"
          value={value}
          onChange={e => setValue(e.target.value)}
          onFocus={() => setFocused(true)}
          onBlur={() => setTimeout(() => setFocused(false), 100)}
          onKeyDown={onKeyDown}
          placeholder={PLACEHOLDERS[phIdx]}
          spellCheck={false}
          autoComplete="off"
        />
        <button
          className={'omni-help-btn ' + (showHelp ? 'on' : '')}
          onClick={() => setShowHelp(h => !h)}
          title="Ajuda (F1)">?
        </button>
      </div>

      {error && <div className="omni-error">{error}</div>}

      {/* Autocomplete dropdown */}
      {showSugg && suggestions.length > 0 && (
        <div className="omni-sugg">
          {suggestions.map((s, i) => (
            <button key={s.kind + s.code}
              className={'omni-sugg-row ' + (i === hi ? 'on' : '')}
              onMouseEnter={() => setHi(i)}
              onMouseDown={(e) => { e.preventDefault(); setValue(s.code); setTimeout(() => inputRef.current?.focus(), 0); }}>
              <code className="omni-sugg-code">{s.code}</code>
              <span className="omni-sugg-name"><Highlight text={s.label} query={cmd?.query || value} /></span>
              <span className="omni-sugg-price">{BRL(s.price)}</span>
            </button>
          ))}
          {slashHint && (
            <div className="omni-sugg-tip">💡 Digite <code>101/102</code> para meio a meio</div>
          )}
        </div>
      )}

      {/* Inline panel */}
      {showPanel && (
        <InlinePanel
          cmd={cmd}
          onConfirm={panelConfirm}
          onCancel={reset}
          panelRef={panelRef}
        />
      )}

      {/* Help card */}
      {showHelp && <OmniHelp onClose={() => setShowHelp(false)} />}
    </div>
  );
}

window.OmniBox = OmniBox;
window.parseCommand = parseCommand;
