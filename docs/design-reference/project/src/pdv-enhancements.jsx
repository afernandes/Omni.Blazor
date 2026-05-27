// ════════════════════════════════════════════════
// PDV Enhancements — recursos adicionais sem reescrever o PDV
// - Multi-pedido pausado (tabs)
// - Favoritos / recentes (rail acima do grid)
// - Help modal (atalhos de teclado)
// - Loyalty inline (cupom + pontos)
// - Remove reason (motivo de cancelamento)
// - Density toggle (tweak)
// ════════════════════════════════════════════════

const { useState: useEnh, useEffect: useEnhEffect, useMemo: useEnhMemo } = React;

// ─── localStorage helpers ──────────────────────────
const LS = {
  get(key, fallback) {
    try { return JSON.parse(localStorage.getItem(key)) ?? fallback; }
    catch { return fallback; }
  },
  set(key, value) {
    try { localStorage.setItem(key, JSON.stringify(value)); } catch {}
  },
};

// ════════════════════════════════════════════════
// Paused orders — barra horizontal de pedidos suspensos
// ════════════════════════════════════════════════

function PausedOrdersBar({ orders, current, onResume, onPauseCurrent, onClose, hasItems }) {
  if (orders.length === 0 && !hasItems) return null;
  return (
    <div className="paused-bar">
      <div className="paused-bar-label">
        <Icon name="cart" size={13} />
        Pedidos em atendimento
      </div>
      <div className="paused-bar-tabs">
        <button className="paused-tab active">
          <span className="paused-tab-dot" />
          <span className="paused-tab-label">{current.label}</span>
          {current.count > 0 && <span className="paused-tab-count">{current.count}</span>}
        </button>

        {orders.map(o => (
          <button key={o.id} className="paused-tab" onClick={() => onResume(o.id)}>
            <Icon name={o.icon || 'cart'} size={11} />
            <span className="paused-tab-label">{o.label}</span>
            <span className="paused-tab-count">{o.count}</span>
            <span className="paused-tab-amount">{BRL(o.total)}</span>
            <span className="paused-tab-close" onClick={(e) => { e.stopPropagation(); onClose(o.id); }}>
              <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   strokeWidth="2.5" strokeLinecap="round"><path d="M18 6 6 18M6 6l12 12"/></svg>
            </span>
          </button>
        ))}

        {hasItems && (
          <button className="paused-pause-btn" onClick={onPauseCurrent}>
            <Icon name="clock" size={12} />
            Pausar e iniciar novo
            <span className="kbd-soft">Ctrl+N</span>
          </button>
        )}
      </div>
    </div>
  );
}

// ════════════════════════════════════════════════
// Favorites / Recent products rail
// ════════════════════════════════════════════════

function FavoritesRail({ recent, onAdd }) {
  if (!recent || recent.length === 0) return null;
  return (
    <div className="fav-rail">
      <div className="fav-rail-label">
        <Icon name="star" size={11} />
        Recentes &amp; favoritos
      </div>
      <div className="fav-rail-items">
        {recent.map(p => (
          <button key={p.id} className="fav-chip" onClick={() => onAdd(p)}>
            <span className="fav-chip-name">{p.name}</span>
            <span className="fav-chip-price">{BRL(p.price)}</span>
            <span className="fav-chip-plus">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   strokeWidth="2.5" strokeLinecap="round"><path d="M12 5v14M5 12h14"/></svg>
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}

// ════════════════════════════════════════════════
// Help modal — atalhos de teclado
// ════════════════════════════════════════════════

const SHORTCUTS = [
  { group: 'Navegação', items: [
    { keys: ['F1', '?'],       label: 'Abrir ajuda · atalhos' },
    { keys: ['F2'],            label: 'Abrir cardápio completo' },
    { keys: ['Esc'],           label: 'Fechar painel ativo' },
    { keys: ['Ctrl', 'K'],     label: 'Buscar pedido, cliente ou produto' },
  ]},
  { group: 'Pedido', items: [
    { keys: ['Ctrl', 'N'],     label: 'Pausar e iniciar novo pedido' },
    { keys: ['F5', 'End'],     label: 'Finalizar e ir para pagamento' },
    { keys: ['F9'],            label: 'Cancelar pedido' },
    { keys: ['+'],             label: 'Incrementar quantidade do item' },
    { keys: ['−'],             label: 'Decrementar quantidade do item' },
    { keys: ['Del'],           label: 'Remover item selecionado' },
  ]},
  { group: 'Modo de venda', items: [
    { keys: ['Ctrl', '1'],     label: 'Balcão' },
    { keys: ['Ctrl', '2'],     label: 'Delivery' },
    { keys: ['Ctrl', '3'],     label: 'Retirada' },
    { keys: ['Ctrl', '4'],     label: 'Mesa' },
    { keys: ['Ctrl', '5'],     label: 'Comanda' },
  ]},
  { group: 'OmniBox · entrada rápida', items: [
    { keys: ['códigos'],       label: 'Digite o código numérico do produto' },
    { keys: ['M07'],           label: 'Selecionar mesa 07' },
    { keys: ['C:tel'],         label: 'Buscar cliente por telefone' },
    { keys: ['$'],             label: 'Encerrar pedido' },
  ]},
];

function HelpModal({ open, onClose }) {
  if (!open) return null;
  return (
    <div className="help-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="help-modal" role="dialog" aria-label="Atalhos de teclado">
        <div className="help-head">
          <div>
            <div className="help-title">Atalhos do PDV</div>
            <div className="help-sub">Toda função do PDV pode ser feita sem mouse</div>
          </div>
          <button className="btn-ghost btn-icon" onClick={onClose} aria-label="Fechar">
            <Icon name="x" size={16} />
          </button>
        </div>
        <div className="help-body">
          {SHORTCUTS.map(g => (
            <section key={g.group} className="help-group">
              <h3>{g.group}</h3>
              <ul>
                {g.items.map((s, i) => (
                  <li key={i}>
                    <div className="help-keys">
                      {s.keys.map((k, j) => (
                        <React.Fragment key={j}>
                          {j > 0 && <span className="help-plus">+</span>}
                          <kbd>{k}</kbd>
                        </React.Fragment>
                      ))}
                    </div>
                    <span className="help-label">{s.label}</span>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
        <div className="help-foot">
          <span>Pressione <kbd>Esc</kbd> ou <kbd>?</kbd> para fechar</span>
          <a className="linklike" href="#">Personalizar atalhos</a>
        </div>
      </div>
    </div>
  );
}

// ════════════════════════════════════════════════
// Loyalty mini-card — cupom + pontos
// ════════════════════════════════════════════════

function LoyaltyMini({ customer, onApplyCoupon, onApplyPoints, applied }) {
  if (!customer) return null;
  const points = customer.points || 0;
  const coupon = customer.coupon;
  if (points === 0 && !coupon) return null;

  // Open by default if any benefit is already applied; otherwise collapsed
  const [open, setOpen] = React.useState(!!(applied?.points || applied?.coupon));
  React.useEffect(() => {
    if (applied?.points || applied?.coupon) setOpen(true);
  }, [applied?.points, applied?.coupon]);

  // Listen for external "open" event (from view-head star button on mobile)
  React.useEffect(() => {
    const onOpen = () => setOpen(true);
    window.addEventListener('pdv:open-loyalty', onOpen);
    return () => window.removeEventListener('pdv:open-loyalty', onOpen);
  }, []);

  // Compute previewable savings (if benefits were applied)
  const pointsValue = points * 0.05;
  const couponValue = coupon?.value || 0;
  const maxSavings = pointsValue + couponValue;
  const anyApplied = !!(applied?.points || applied?.coupon);
  const appliedSavings =
    (applied?.points ? pointsValue : 0) + (applied?.coupon ? couponValue : 0);

  // Summary chips for the collapsed header
  const summaryBits = [];
  if (points > 0)  summaryBits.push(`${points} pts`);
  if (coupon)      summaryBits.push(coupon.label);

  return (
    <div className={'loyalty-mini ' + (open ? 'is-open' : 'is-collapsed') + (anyApplied ? ' is-applied' : '')}>
      <button
        type="button"
        className="loyalty-mini-toggle"
        aria-expanded={open}
        onClick={() => setOpen(o => !o)}>
        <span className="loyalty-mini-head">
          <span className="loyalty-mini-ico">
            <Icon name="star" size={12} />
          </span>
          <span className="loyalty-mini-title">
            <span className="loyalty-mini-eyebrow">FIDELIDADE</span>
            <span className="loyalty-mini-sub">
              {anyApplied
                ? `Aplicado · economia R$ ${appliedSavings.toFixed(2).replace('.', ',')}`
                : summaryBits.length
                  ? summaryBits.join(' · ')
                  : 'Sem benefícios disponíveis'}
            </span>
          </span>
        </span>
        <span className="loyalty-mini-trail">
          {!anyApplied && maxSavings > 0 && (
            <span className="loyalty-mini-savings" title="Economia máxima disponível">
              até −R$ {maxSavings.toFixed(2).replace('.', ',')}
            </span>
          )}
          <svg className="loyalty-mini-caret" width="12" height="12" viewBox="0 0 24 24" fill="none"
               stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </span>
      </button>

      {open && (
        <div className="loyalty-mini-rows">
          {points > 0 && (
            <label className="loyalty-row">
              <input type="checkbox"
                checked={!!applied?.points}
                onChange={(e) => onApplyPoints(e.target.checked)} />
              <span className="loyalty-row-label">
                <span className="loyalty-row-name">Usar pontos</span>
                <span className="loyalty-row-meta">{points} pts disponíveis · vale R$ {pointsValue.toFixed(2).replace('.', ',')}</span>
              </span>
              <span className="loyalty-row-val">−R$ {pointsValue.toFixed(2).replace('.', ',')}</span>
            </label>
          )}
          {coupon && (
            <label className="loyalty-row">
              <input type="checkbox"
                checked={!!applied?.coupon}
                onChange={(e) => onApplyCoupon(e.target.checked)} />
              <span className="loyalty-row-label">
                <span className="loyalty-row-name">{coupon.label}</span>
                <span className="loyalty-row-meta">expira {coupon.expires}</span>
              </span>
              <span className="loyalty-row-val">−{coupon.discount}</span>
            </label>
          )}
        </div>
      )}
    </div>
  );
}

// ════════════════════════════════════════════════
// Remove reason — motivo de cancelamento
// ════════════════════════════════════════════════

const REMOVE_REASONS = [
  { id: 'mind',    label: 'Cliente mudou de ideia',     icon: 'user' },
  { id: 'price',   label: 'Achou caro',                  icon: 'cash' },
  { id: 'stock',   label: 'Sem estoque',                 icon: 'store' },
  { id: 'time',    label: 'Tempo de espera longo',       icon: 'clock' },
  { id: 'mistake', label: 'Erro do operador',            icon: 'edit' },
  { id: 'other',   label: 'Outro motivo',                icon: 'menu' },
];

function RemoveReasonModal({ item, onConfirm, onCancel }) {
  if (!item) return null;
  const [reason, setReason] = useEnh(null);
  const [note, setNote] = useEnh('');

  return (
    <div className="help-overlay" onClick={(e) => { if (e.target === e.currentTarget) onCancel(); }}>
      <div className="reason-modal">
        <div className="reason-head">
          <Icon name="trash" size={16} />
          <div>
            <div className="reason-title">Remover do pedido</div>
            <div className="reason-sub">{item.qty}× {item.name}</div>
          </div>
        </div>
        <div className="reason-body">
          <div className="reason-label">Motivo</div>
          <div className="reason-grid">
            {REMOVE_REASONS.map(r => (
              <button key={r.id}
                className={'reason-chip ' + (reason === r.id ? 'on' : '')}
                onClick={() => setReason(r.id)}>
                <Icon name={r.icon} size={13} />
                {r.label}
              </button>
            ))}
          </div>
          {reason === 'other' && (
            <textarea className="reason-note"
              placeholder="Descreva o motivo…"
              value={note}
              onChange={e => setNote(e.target.value)}
              autoFocus rows={2} />
          )}
        </div>
        <div className="reason-foot">
          <button className="btn btn-ghost" onClick={onCancel}>Cancelar</button>
          <button className="btn btn-primary"
            disabled={!reason || (reason === 'other' && !note.trim())}
            style={{ background: 'var(--danger)', borderColor: 'var(--danger)' }}
            onClick={() => onConfirm({ reason, note })}>
            Remover item
          </button>
        </div>
      </div>
    </div>
  );
}

// ════════════════════════════════════════════════
// Hook: useRecentProducts — track adds, persist
// ════════════════════════════════════════════════

function useRecentProducts() {
  const [recent, setRecent] = useEnh(() => LS.get('pdv:recent', []));
  useEnhEffect(() => { LS.set('pdv:recent', recent); }, [recent]);

  const trackAdd = (product) => {
    if (!product || product.configurable) return; // skip pizzas (configurator-flow)
    setRecent(prev => {
      const filtered = prev.filter(p => p.id !== product.id);
      return [{ id: product.id, name: product.name, price: product.price, cat: product.cat, hits: (prev.find(p => p.id === product.id)?.hits || 0) + 1 }, ...filtered].slice(0, 8);
    });
  };
  return { recent, trackAdd };
}

// Expose to window for cross-file use
Object.assign(window, {
  PausedOrdersBar,
  FavoritesRail,
  HelpModal,
  LoyaltyMini,
  RemoveReasonModal,
  useRecentProducts,
  AdjustmentButton,
  AdjustmentModal,
  applyAdjustment,
  PDV_LS: LS,
});

// ════════════════════════════════════════════════
// Adjustment (discount / surcharge) — item + order
// ════════════════════════════════════════════════

const ADJUST_PRESETS = {
  discount: [
    { mode: 'percent', value: 5 },
    { mode: 'percent', value: 10 },
    { mode: 'percent', value: 15 },
    { mode: 'percent', value: 20 },
  ],
  surcharge: [
    { mode: 'percent', value: 5 },
    { mode: 'percent', value: 10 },
  ],
};

const ADJUST_REASONS = {
  discount: [
    { id: 'courtesy', label: 'Cortesia' },
    { id: 'loyal', label: 'Cliente fiel' },
    { id: 'damage', label: 'Cobrança incorreta' },
    { id: 'delay', label: 'Compensação por atraso' },
    { id: 'manager', label: 'Aprovado pelo gerente' },
    { id: 'other', label: 'Outro' },
  ],
  surcharge: [
    { id: 'service', label: 'Taxa de serviço' },
    { id: 'fee', label: 'Taxa de embalagem' },
    { id: 'rush', label: 'Pedido urgente' },
    { id: 'other', label: 'Outro' },
  ],
};

// Compute the discount/surcharge value (in R$) from an adjustment vs a base price.
function applyAdjustment(base, adjustment) {
  if (!adjustment || !base) return 0;
  const sign = adjustment.kind === 'surcharge' ? 1 : -1;
  if (adjustment.mode === 'percent') {
    return sign * Math.round(base * (adjustment.value / 100) * 100) / 100;
  }
  return sign * Math.min(adjustment.value, base);
}

function AdjustmentButton({ adjustment, onClick, compact = false }) {
  if (!adjustment) {
    return (
      <button className={'adj-btn ' + (compact ? 'compact' : '')} onClick={onClick} title="Aplicar desconto ou acréscimo">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <line x1="19" y1="5" x2="5" y2="19"/>
          <circle cx="8" cy="8" r="2.2"/>
          <circle cx="16" cy="16" r="2.2"/>
        </svg>
        {!compact && <span>Ajustar</span>}
      </button>
    );
  }
  const isDisc = adjustment.kind === 'discount';
  return (
    <button className={'adj-btn applied ' + (isDisc ? 'discount' : 'surcharge')} onClick={onClick}
            title="Editar ajuste">
      <span className="adj-btn-sign">{isDisc ? '−' : '+'}</span>
      <span className="adj-btn-val">
        {adjustment.mode === 'percent' ? `${adjustment.value}%` : BRL(adjustment.value)}
      </span>
    </button>
  );
}

function AdjustmentModal({ open, target, current, onConfirm, onClear, onCancel }) {
  // target = { kind: 'item'|'order', label: string, base: number }
  const [kind, setKind] = React.useState(current?.kind || 'discount');
  const [mode, setMode] = React.useState(current?.mode || 'percent');
  const [value, setValue] = React.useState(current?.value ?? '');
  const [reason, setReason] = React.useState(current?.reason || '');
  const [reasonNote, setReasonNote] = React.useState(current?.reasonNote || '');
  const ref = React.useRef(null);
  useFocusTrap(open, ref, onCancel);

  React.useEffect(() => {
    if (!open) return;
    setKind(current?.kind || 'discount');
    setMode(current?.mode || 'percent');
    setValue(current?.value ?? '');
    setReason(current?.reason || '');
    setReasonNote(current?.reasonNote || '');
  }, [open, current]);

  if (!open || !target) return null;

  const numVal = parseFloat(String(value).replace(',', '.')) || 0;
  const previewAdj = { kind, mode, value: numVal };
  const previewDelta = applyAdjustment(target.base, previewAdj);
  const finalAmount = target.base + previewDelta;
  const reasons = ADJUST_REASONS[kind];
  const presets = ADJUST_PRESETS[kind];
  const canConfirm = numVal > 0 && (mode === 'percent' ? numVal <= 100 : numVal <= target.base);

  const confirm = () => {
    if (!canConfirm) return;
    onConfirm({
      kind,
      mode,
      value: numVal,
      reason: reason || undefined,
      reasonNote: reason === 'other' ? reasonNote : undefined,
    });
  };

  return (
    <div className="adj-overlay" onClick={(e) => { if (e.target === e.currentTarget) onCancel(); }}>
      <div className="adj-modal" ref={ref} role="dialog" aria-label="Ajustar valor">
        <div className="adj-head">
          <div>
            <div className="adj-eyebrow">{target.kind === 'item' ? 'AJUSTE DE ITEM' : 'AJUSTE NO PEDIDO'}</div>
            <div className="adj-title">{target.label}</div>
            <div className="adj-base">Valor base: <b>{BRL(target.base)}</b></div>
          </div>
          <button className="prechk-close" onClick={onCancel} aria-label="Fechar">
            <Icon name="x" size={18} />
          </button>
        </div>

        <div className="adj-body">
          {/* Kind: discount or surcharge */}
          <div className="adj-section">
            <div className="adj-label">Tipo</div>
            <div className="adj-kind">
              <button className={'adj-kind-btn ' + (kind === 'discount' ? 'on discount' : '')}
                      onClick={() => setKind('discount')}>
                <span className="adj-kind-ico">−</span>
                Desconto
              </button>
              <button className={'adj-kind-btn ' + (kind === 'surcharge' ? 'on surcharge' : '')}
                      onClick={() => setKind('surcharge')}>
                <span className="adj-kind-ico">+</span>
                Acréscimo
              </button>
            </div>
          </div>

          {/* Mode: % or fixed */}
          <div className="adj-section">
            <div className="adj-label">Forma</div>
            <div className="adj-mode">
              <button className={'adj-mode-btn ' + (mode === 'percent' ? 'on' : '')}
                      onClick={() => setMode('percent')}>
                <span>%</span> Percentual
              </button>
              <button className={'adj-mode-btn ' + (mode === 'fixed' ? 'on' : '')}
                      onClick={() => setMode('fixed')}>
                <span>R$</span> Valor fixo
              </button>
            </div>
          </div>

          {/* Value input + presets */}
          <div className="adj-section">
            <div className="adj-label">Valor</div>
            <div className="adj-value-row">
              <div className="adj-input-wrap">
                <span className="adj-input-prefix">{mode === 'percent' ? '%' : 'R$'}</span>
                <input className="adj-input"
                  type="number" inputMode="decimal" step="0.01" min="0"
                  max={mode === 'percent' ? 100 : target.base}
                  value={value}
                  onChange={(e) => setValue(e.target.value)}
                  placeholder="0"
                  autoFocus />
              </div>
              <div className="adj-presets">
                {presets.map(p => (
                  <button key={p.mode + p.value}
                          className={'adj-preset ' + (mode === p.mode && numVal === p.value ? 'on' : '')}
                          onClick={() => { setMode(p.mode); setValue(p.value); }}>
                    {p.mode === 'percent' ? `${p.value}%` : BRL(p.value)}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Reason (for audit) */}
          <div className="adj-section">
            <div className="adj-label">Motivo {kind === 'discount' && <span className="adj-label-hint">(para auditoria)</span>}</div>
            <div className="adj-reasons">
              {reasons.map(r => (
                <button key={r.id}
                  className={'adj-reason ' + (reason === r.id ? 'on' : '')}
                  onClick={() => setReason(r.id)}>
                  {r.label}
                </button>
              ))}
            </div>
            {reason === 'other' && (
              <input className="input adj-reason-note"
                type="text"
                placeholder="Descreva o motivo…"
                value={reasonNote}
                onChange={e => setReasonNote(e.target.value)} />
            )}
          </div>
        </div>

        {/* Live preview */}
        <div className={'adj-preview ' + kind}>
          <div className="adj-preview-row">
            <span>Valor base</span>
            <b>{BRL(target.base)}</b>
          </div>
          <div className="adj-preview-row delta">
            <span>{kind === 'discount' ? 'Desconto' : 'Acréscimo'}</span>
            <b>{previewDelta >= 0 ? '+' : '−'}{BRL(Math.abs(previewDelta))}</b>
          </div>
          <div className="adj-preview-row grand">
            <span>Valor final</span>
            <b>{BRL(Math.max(0, finalAmount))}</b>
          </div>
        </div>

        <div className="adj-foot">
          {current && (
            <button className="btn btn-ghost" onClick={onClear}>
              <Icon name="trash" size={13} /> Remover ajuste
            </button>
          )}
          <div style={{ flex: 1 }} />
          <button className="btn btn-ghost" onClick={onCancel}>Cancelar</button>
          <button className="btn btn-primary"
            disabled={!canConfirm}
            onClick={confirm}>
            Aplicar {previewDelta !== 0 && `(${kind === 'discount' ? '−' : '+'}${BRL(Math.abs(previewDelta))})`}
          </button>
        </div>
      </div>
    </div>
  );
}
