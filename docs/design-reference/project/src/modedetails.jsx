// ============================================================
// Painel de detalhes do pedido por modo:
//   delivery  → endereço + zona de entrega
//   mesa      → número da mesa + garçom
//   comanda   → número da comanda
//   retirada  → nome para retirada + previsão
//   balcao    → (sem detalhes extras)
// ============================================================

const { useState: useMD, useMemo: useMDMemo } = React;

const NEIGHBORHOODS_DEFAULT = [
  { name: 'Centro',         fee: 5.00,  eta: 30 },
  { name: 'Vila Madalena',  fee: 8.00,  eta: 40 },
  { name: 'Pinheiros',      fee: 9.00,  eta: 45 },
  { name: 'Perdizes',       fee: 10.00, eta: 45 },
  { name: 'Higienópolis',   fee: 11.00, eta: 50 },
  { name: 'Jardins',        fee: 12.00, eta: 50 },
  { name: 'Sumaré',         fee: 9.00,  eta: 40 },
  { name: 'Vila Romana',    fee: 10.00, eta: 45 },
  { name: 'Lapa',           fee: 11.00, eta: 50 },
  { name: 'Morumbi',        fee: 14.00, eta: 55 },
];

// Persistent custom neighborhoods + last-known fees for ad-hoc orders.
const HOOD_LS = {
  customs() {
    try { return JSON.parse(localStorage.getItem('pdv:hoods:custom') || '[]'); }
    catch { return []; }
  },
  saveCustom(arr) {
    try { localStorage.setItem('pdv:hoods:custom', JSON.stringify(arr)); } catch {}
  },
  lastFees() {
    try { return JSON.parse(localStorage.getItem('pdv:hoods:lastFees') || '{}'); }
    catch { return {}; }
  },
  saveLastFee(name, fee) {
    try {
      const all = HOOD_LS.lastFees();
      all[name.toLowerCase().trim()] = fee;
      localStorage.setItem('pdv:hoods:lastFees', JSON.stringify(all));
    } catch {}
  },
};

const NEIGHBORHOODS = [...NEIGHBORHOODS_DEFAULT];
const ZONES_DELIVERY = NEIGHBORHOODS.map((n, i) => ({ id: 'z' + i, name: n.name, fee: n.fee, eta: n.eta }));

// ——— Delivery ———
function HoodCombo({ value, onChange, options }) {
  const [open, setOpen] = React.useState(false);
  const ref = React.useRef(null);

  React.useEffect(() => {
    if (!open) return;
    const onDoc = (e) => {
      if (ref.current?.contains(e.target)) return;
      setOpen(false);
    };
    const onKey = (e) => { if (e.key === 'Escape') setOpen(false); };
    document.addEventListener('mousedown', onDoc);
    window.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDoc);
      window.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const matched = options.find(n => n.name.toLowerCase() === (value || '').trim().toLowerCase());
  const visible = value
    ? options.filter(n => n.name.toLowerCase().includes(value.trim().toLowerCase()))
    : options;

  return (
    <div className="hood-combo" ref={ref}>
      <div className={'hood-combo-input ' + (open ? 'is-open' : '')}>
        <input className="input"
          placeholder="Digite ou selecione o bairro…"
          value={value}
          onChange={(e) => { onChange(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)} />
        <button className="hood-combo-toggle" onClick={() => setOpen(o => !o)} type="button"
                aria-label="Abrir lista de bairros">
          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </button>
      </div>
      {open && (
        <div className="hood-combo-list" role="listbox">
          {visible.length === 0 ? (
            <div className="hood-combo-empty">Nenhum bairro corresponde · siga digitando para fora da área</div>
          ) : visible.map(n => (
            <button key={n.name}
              type="button"
              className={'hood-combo-item ' + (matched?.name === n.name ? 'on' : '')}
              onClick={() => { onChange(n.name); setOpen(false); }}>
              <div className="hood-combo-name">{n.name}</div>
              <div className="hood-combo-meta">
                <span className="hood-combo-eta">{n.eta} min</span>
                <span className="hood-combo-fee">{BRL(n.fee)}</span>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

function DeliveryDetails({ details, onChange, customer }) {
  const d = details || {};
  const set = (k, v) => onChange({ ...d, [k]: v });
  const [showExtras, setShowExtras] = React.useState(!!(d.complement || d.reference));
  const [cepSearching, setCepSearching] = React.useState(false);
  const [customs, setCustoms] = React.useState(() => HOOD_LS.customs());
  const [lastFees] = React.useState(() => HOOD_LS.lastFees());
  const [justSaved, setJustSaved] = React.useState(false);

  // Active list = defaults ∪ persisted customs
  const allHoods = React.useMemo(() => {
    const map = new Map(NEIGHBORHOODS_DEFAULT.map(n => [n.name.toLowerCase(), n]));
    customs.forEach(n => map.set(n.name.toLowerCase(), n));
    return Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name));
  }, [customs]);

  const matchedNeighborhood = React.useMemo(() => {
    if (!d.neighborhood) return null;
    const needle = d.neighborhood.trim().toLowerCase();
    return allHoods.find(n => n.name.toLowerCase() === needle) || null;
  }, [d.neighborhood, allHoods]);

  const isUnknownNeighborhood = !!d.neighborhood && !matchedNeighborhood;

  // Pre-fill customFee from last known fee for the typed hood
  React.useEffect(() => {
    if (!isUnknownNeighborhood) return;
    if (d.customFee) return;
    const last = lastFees[d.neighborhood.trim().toLowerCase()];
    if (last) set('customFee', String(last));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isUnknownNeighborhood, d.neighborhood]);

  const effectiveFee = matchedNeighborhood?.fee ?? (parseFloat(d.customFee) || 0);

  // Auto-fill from customer address (one-shot)
  const filledFromCustomer = React.useRef(false);
  React.useEffect(() => {
    if (filledFromCustomer.current) return;
    if (!customer?.address || d.street) return;
    onChange({
      ...d,
      street: customer.address,
      neighborhood: customer.neighborhood || '',
      zip: customer.zip || '',
    });
    filledFromCustomer.current = true;
  }, [customer]);

  const lookupCep = () => {
    if (!d.zip || d.zip.length < 8) return;
    setCepSearching(true);
    setTimeout(() => {
      onChange({
        ...d,
        street: d.street || 'Rua das Laranjeiras',
        neighborhood: d.neighborhood || 'Centro',
        city: 'São Paulo',
        state: 'SP',
      });
      setCepSearching(false);
    }, 700);
  };

  const registerHood = (notServed = false) => {
    if (!d.neighborhood) return;
    const fee = parseFloat(d.customFee);
    if (!notServed && !fee) return;
    const newHood = {
      name: d.neighborhood.trim(),
      fee: notServed ? 0 : fee,
      eta: parseInt(d.customEta, 10) || 45,
      served: !notServed,
    };
    const next = [...customs.filter(n => n.name.toLowerCase() !== newHood.name.toLowerCase()), newHood];
    HOOD_LS.saveCustom(next);
    if (!notServed) HOOD_LS.saveLastFee(newHood.name, newHood.fee);
    setCustoms(next);
    setJustSaved(notServed ? 'blocked' : 'saved');
    setTimeout(() => setJustSaved(false), 2200);
  };

  return (
    <div className="mode-details">
      {/* CEP */}
      <div className="mode-field">
        <label className="label">CEP</label>
        <div className="cep-row">
          <input className="input"
            inputMode="numeric"
            placeholder="00000-000"
            maxLength={9}
            value={d.zip || ''}
            onChange={e => set('zip', e.target.value)}
            onBlur={lookupCep} />
          <button className="btn" onClick={lookupCep} disabled={cepSearching} title="Buscar CEP">
            <Icon name={cepSearching ? 'clock' : 'search'} size={13} />
            {cepSearching ? 'Buscando…' : 'Buscar'}
          </button>
        </div>
      </div>

      {/* Street + number */}
      <div className="mode-field">
        <div className="address-row">
          <input className="input" placeholder="Rua / Avenida"
            value={d.street || ''} onChange={e => set('street', e.target.value)} />
          <input className="input num" placeholder="Nº"
            inputMode="numeric"
            value={d.number || ''} onChange={e => set('number', e.target.value)} />
        </div>
      </div>

      {/* Neighborhood — custom combo with fee/ETA preview */}
      <div className="mode-field hood-combo-wrap">
        <label className="label">Bairro</label>
        <HoodCombo
          value={d.neighborhood || ''}
          onChange={(v) => set('neighborhood', v)}
          options={allHoods} />
      </div>

      {/* Neighborhood status: matched / not-served / unknown / empty */}
      {matchedNeighborhood && matchedNeighborhood.served === false && (
        <div className="hood-info blocked">
          <div className="hood-info-icon"><Icon name="x" size={12} /></div>
          <div className="hood-info-body">
            <div className="hood-info-name">{matchedNeighborhood.name} · bairro não atendido</div>
            <div className="hood-info-sub">
              A loja não faz delivery para este bairro. Sugira retirada no balcão.
            </div>
          </div>
        </div>
      )}
      {matchedNeighborhood && matchedNeighborhood.served !== false && (
        <div className="hood-info matched">
          <div className="hood-info-icon"><Icon name="check" size={12} /></div>
          <div className="hood-info-body">
            <div className="hood-info-name">
              {matchedNeighborhood.name}
              <span className="hood-info-eta">{matchedNeighborhood.eta} min</span>
            </div>
            <div className="hood-info-sub">Bairro atendido · taxa automática</div>
          </div>
          <div className="hood-info-fee">{BRL(matchedNeighborhood.fee)}</div>
        </div>
      )}
      {isUnknownNeighborhood && (
        <div className={'hood-info unknown ' + (justSaved === 'saved' ? 'just-saved' : justSaved === 'blocked' ? 'just-blocked' : '')}>
          <div className="hood-info-icon">
            <Icon name={justSaved === 'saved' ? 'check' : justSaved === 'blocked' ? 'x' : 'flame'} size={12} />
          </div>
          <div className="hood-info-body">
            <div className="hood-info-name">
              {justSaved === 'saved'   ? 'Bairro cadastrado · taxa salva' :
               justSaved === 'blocked' ? 'Bairro marcado como não atendido' :
               `${d.neighborhood} fora da lista`}
            </div>
            <div className="hood-info-sub">
              {justSaved === 'saved'
                ? 'Próximos pedidos deste bairro já usam essa taxa automaticamente'
                : justSaved === 'blocked'
                  ? 'Nos próximos pedidos, o sistema avisa que esse bairro não recebe entrega'
                  : (lastFees[d.neighborhood.trim().toLowerCase()]
                    ? 'Última taxa cobrada pré-preenchida abaixo'
                    : 'Informe a taxa e cadastre o bairro pra próximos pedidos')}
            </div>
            {!justSaved && (
              <>
                <div className="hood-info-actions">
                  <div className="hood-info-fee-input">
                    <span className="prefix">R$</span>
                    <input type="number" step="0.50" inputMode="decimal"
                      placeholder="0,00"
                      value={d.customFee || ''}
                      onChange={e => set('customFee', e.target.value)} />
                  </div>
                  <div className="hood-info-eta-input">
                    <input type="number" step="5" inputMode="numeric"
                      placeholder="min"
                      value={d.customEta || ''}
                      onChange={e => set('customEta', e.target.value)} />
                    <span className="suffix">min</span>
                  </div>
                  <button className="btn btn-primary hood-info-save"
                    onClick={() => registerHood(false)}
                    disabled={!parseFloat(d.customFee)}
                    title="Salvar bairro + taxa">
                    <Icon name="plus" size={12} /> Cadastrar
                  </button>
                </div>
                <button className="hood-info-block-link"
                  onClick={() => registerHood(true)}
                  title="Marca este bairro como não atendido permanentemente">
                  <Icon name="x" size={11} /> Não atendemos esse bairro
                </button>
              </>
            )}
          </div>
        </div>
      )}

      {/* Extras toggle */}
      {!showExtras ? (
        <button className="address-extra-toggle" onClick={() => setShowExtras(true)}>
          <Icon name="plus" size={12} />
          Complemento ou referência
        </button>
      ) : (
        <>
          <div className="mode-field">
            <input className="input" placeholder="Complemento (Bloco A · Ap 102 · Casa 2)"
              value={d.complement || ''} onChange={e => set('complement', e.target.value)} />
          </div>
          <div className="mode-field">
            <input className="input" placeholder="Referência (em frente ao mercado X)"
              value={d.reference || ''} onChange={e => set('reference', e.target.value)} />
          </div>
        </>
      )}

      {/* Entregador removido — só é vinculado quando o pedido sai pra entrega */}
    </div>
  );
}

// ——— Mesa ———
function MesaDetails({ details, onChange }) {
  const d = details || {};
  const set = (k, v) => onChange({ ...d, [k]: v });

  // Mock: 40 mesas, várias delas já abertas
  const TABLES = Array.from({ length: 40 }, (_, i) => ({
    num: i + 1,
    open: [3, 5, 7, 12, 14, 22, 28, 31].includes(i + 1),
  }));
  const [tq, setTq] = React.useState('');
  const visibleTables = tq
    ? TABLES.filter(t => String(t.num).startsWith(tq.trim()))
    : TABLES;

  return (
    <div className="mode-details">
      <div className="mode-field">
        <div className="tables-search">
          <input className="input" type="number" inputMode="numeric"
            placeholder="Buscar mesa…"
            value={tq} onChange={e => setTq(e.target.value)} />
          {d.tableNum && (
            <span className="badge accent" style={{ fontFamily: 'var(--font-mono)', padding: '4px 8px' }}>
              Mesa {d.tableNum}
            </span>
          )}
        </div>
        <div className="tables-picker compact">
          {visibleTables.map(t => (
            <button key={t.num}
              className={'table-pick ' + (d.tableNum === t.num ? 'selected' : t.open ? 'open' : '')}
              onClick={() => set('tableNum', t.num)}>
              {t.num}
            </button>
          ))}
          {visibleTables.length === 0 && (
            <div style={{ gridColumn: '1 / -1', padding: 8, fontSize: 11, color: 'var(--fg-soft)', textAlign: 'center' }}>
              Nenhuma mesa encontrada
            </div>
          )}
        </div>
        {d.tableNum && (
          <div style={{ marginTop: 6, fontSize: 11.5, color: 'var(--fg-muted)' }}>
            {TABLES.find(t => t.num === d.tableNum)?.open
              ? <span style={{ color: 'var(--warn)' }}>⚠ Mesa já aberta — adicionando ao pedido existente</span>
              : <span style={{ color: 'var(--good)' }}>✓ Mesa livre</span>
            }
          </div>
        )}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '90px 1fr', gap: 8 }}>
        <input className="input" type="number" min={1} value={d.covers || ''} onChange={e => set('covers', +e.target.value)} placeholder="Pessoas" />
        <select className="input" value={d.waiter || ''} onChange={e => set('waiter', e.target.value)}>
          <option value="">Garçom…</option>
          <option value="auto">Automático</option>
          <option value="carlos">Carlos</option>
          <option value="ana">Ana</option>
        </select>
      </div>

      {d.tableNum && (
        <div className="mode-fee-badge" style={{ background: 'color-mix(in oklab, var(--good) 10%, transparent)', color: 'var(--good)', borderColor: 'color-mix(in oklab, var(--good) 20%, transparent)' }}>
          <Icon name="table" size={12} />
          <span>Mesa <b>{d.tableNum}</b> · {d.covers || 1} {(d.covers || 1) === 1 ? 'pessoa' : 'pessoas'}</span>
        </div>
      )}
    </div>
  );
}

// ——— Comanda ———
function ComandaDetails({ details, onChange }) {
  const d = details || {};
  const set = (k, v) => onChange({ ...d, [k]: v });

  return (
    <div className="mode-details">
      <div className="mode-field" style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        <input className="input" value={d.comandaNum || ''} onChange={e => set('comandaNum', e.target.value)}
          placeholder="Nº comanda (ex: 042)"
          style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, fontSize: 16, maxWidth: 160 }} />
        <button className="btn" onClick={() => set('comandaNum', String(Math.floor(Math.random() * 900) + 100))}>
          <Icon name="sparkle" size={12} /> Gerar
        </button>
      </div>

      <div className="mode-field">
        <input className="input" value={d.location || ''} onChange={e => set('location', e.target.value)}
          placeholder="Local / observação (área externa, mesa do fundo…)" />
      </div>

      {d.comandaNum && (
        <div className="mode-fee-badge" style={{ background: 'color-mix(in oklab, var(--fg) 8%, transparent)', color: 'var(--fg)', borderColor: 'var(--line-strong)' }}>
          <Icon name="print" size={12} />
          <span>Comanda <b>#{d.comandaNum}</b> — imprimir ao confirmar</span>
        </div>
      )}
    </div>
  );
}

// ——— Retirada (Takeaway) ———
function RetiradaDetails({ details, onChange }) {
  const d = details || {};
  const set = (k, v) => onChange({ ...d, [k]: v });

  const now = new Date();
  const etaOptions = [15, 20, 25, 30, 45].map(m => {
    const t = new Date(now.getTime() + m * 60000);
    return { mins: m, label: `${m} min (${t.getHours().toString().padStart(2,'0')}:${t.getMinutes().toString().padStart(2,'0')})` };
  });

  return (
    <div className="mode-details">
      <div className="mode-field">
        <input className="input" value={d.pickupName || ''} onChange={e => set('pickupName', e.target.value)}
          placeholder="Nome para retirada" />
      </div>

      <div className="mode-field">
        <label className="label">Previsão</label>
        <div className="eta-options compact">
          {etaOptions.map(o => (
            <button key={o.mins}
              className={'eta-option ' + (d.etaMins === o.mins ? 'active' : '')}
              onClick={() => set('etaMins', o.mins)}>
              {o.label}
            </button>
          ))}
        </div>
      </div>

      {d.pickupName && (
        <div className="mode-fee-badge" style={{ background: 'color-mix(in oklab, var(--accent) 8%, transparent)', color: 'var(--accent)', borderColor: 'color-mix(in oklab, var(--accent) 20%, transparent)' }}>
          <Icon name="bag" size={12} />
          <span>Retirada: <b>{d.pickupName}</b>{d.etaMins ? ` · em ${d.etaMins} min` : ''}</span>
        </div>
      )}
    </div>
  );
}

// ——— Balcão ———
function BalcaoDetails({ details, onChange }) {
  const d = details || {};
  const set = (k, v) => onChange({ ...d, [k]: v });
  return (
    <div className="mode-details">
      <div className="mode-field">
        <input className="input" value={d.label || ''} onChange={e => set('label', e.target.value)}
          placeholder="Identificação opcional (ex: João, senha 42…)" />
      </div>
    </div>
  );
}

// ——— Dispatcher: renderiza o painel correto por modo ———
function ModeDetailsPanel({ mode, details, onChange, customer }) {
  if (mode === 'delivery') return <DeliveryDetails details={details} onChange={onChange} customer={customer} />;
  if (mode === 'mesa')     return <MesaDetails     details={details} onChange={onChange} />;
  if (mode === 'comanda')  return <ComandaDetails  details={details} onChange={onChange} />;
  if (mode === 'retirada') return <RetiradaDetails details={details} onChange={onChange} />;
  if (mode === 'balcao')   return <BalcaoDetails   details={details} onChange={onChange} />;
  return null;
}

window.ModeDetailsPanel = ModeDetailsPanel;
window.ZONES_DELIVERY = ZONES_DELIVERY;
window.NEIGHBORHOODS_DEFAULT = NEIGHBORHOODS_DEFAULT;
