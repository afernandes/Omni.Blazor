// PDV — Front of cash register
const { useState, useMemo, useRef, useEffect } = React;

function CustomerSearch({ value, onPick, onClear, expanded = false, deliveryMode = false, onChangeAddress }) {
  const [q, setQ] = useState('');
  const [open, setOpen] = useState(false);

  const results = useMemo(() => {
    if (!q) return [];
    const needle = q.toLowerCase().replace(/\D/g, '');
    const text = q.toLowerCase();
    return CUSTOMERS.filter(c =>
      c.phone.replace(/\D/g, '').includes(needle) ||
      c.name.toLowerCase().includes(text)
    ).slice(0, 4);
  }, [q]);

  if (value) {
    if (expanded) {
      // Expanded customer card — for mobile strip
      const lastOrder = value.lastOrderAgo || 'há 2 dias';
      return (
        <div className="cust-card-expanded">
          <div className="cust-card-expanded-row">
            <div className="cust-card-avatar">
              {value.name.split(' ').map(w => w[0]).slice(0,2).join('')}
            </div>
            <div className="cust-card-info">
              <div className="cust-card-name">{value.name}</div>
              <div className="cust-card-meta">
                <span className="cust-card-meta-item">{value.phone}</span>
                {value.points > 0 && <>
                  <span className="cust-card-meta-sep">·</span>
                  <span className="cust-card-meta-item" style={{ color: 'var(--accent)', fontWeight: 600 }}>
                    {value.points} pts
                  </span>
                </>}
                {value.orders > 0 && <>
                  <span className="cust-card-meta-sep">·</span>
                  <span className="cust-card-meta-item">{value.orders} pedidos</span>
                </>}
              </div>
            </div>
            <button className="cust-card-x" onClick={onClear} title="Remover cliente" aria-label="Remover cliente">
              <Icon name="x" size={14} />
            </button>
          </div>

          {deliveryMode && (
            <div className="cust-card-delivery">
              <div className="cust-card-delivery-head">
                <Icon name="truck" size={11} />
                <span>Endereço de entrega</span>
              </div>
              {value.address ? (
                <button className="cust-card-address" onClick={onChangeAddress}>
                  <span className="cust-card-address-text">{value.address}</span>
                  <span className="cust-card-address-edit">trocar →</span>
                </button>
              ) : (
                <button className="cust-card-address empty" onClick={onChangeAddress}>
                  <Icon name="plus" size={12} />
                  <span>Adicionar endereço</span>
                </button>
              )}
            </div>
          )}
        </div>
      );
    }
    return (
      <div className="cart-customer filled" onClick={onClear}>
        <div className="cust-ico"><Icon name="user" size={16} /></div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="cart-customer-name">{value.name}</div>
          <div className="cart-customer-meta">{value.phone} · {value.points} pts</div>
        </div>
        <button className="btn-ghost btn btn-icon" onClick={(e) => { e.stopPropagation(); onClear(); }} title="Remover">
          <Icon name="x" size={14} />
        </button>
      </div>
    );
  }

  return (
    <div style={{ position: 'relative' }}>
      <div className="input-with-ico">
        <span className="ico"><Icon name="phone" size={16} /></span>
        <input
          className="input"
          placeholder="Telefone ou nome do cliente…"
          value={q}
          onChange={(e) => { setQ(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onBlur={() => setTimeout(() => setOpen(false), 120)}
        />
      </div>
      {open && q && (
        <div className="cust-results" style={{ position: 'absolute', top: 'calc(100% + 4px)', left: 0, right: 0, zIndex: 30, boxShadow: 'var(--shadow-lg)' }}>
          {results.map(c => (
            <button key={c.id} className="cust-result" onMouseDown={() => onPick(c)}>
              <div className="cust-result-avatar">{c.name.split(' ').map(w => w[0]).slice(0,2).join('')}</div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="cust-result-name">{c.name}</div>
                <div className="cust-result-meta">{c.phone} · {c.orders} pedidos</div>
              </div>
              {c.points > 500 && <span className="badge accent">{c.points} pts</span>}
            </button>
          ))}
          {results.length === 0 && (
            <div style={{ padding: '14px', fontSize: 13, color: 'var(--fg-muted)' }}>
              Sem resultados.
              <button className="btn btn-primary" style={{ marginLeft: 10, padding: '6px 12px', fontSize: 12 }} onMouseDown={() => { onPick({ id: 'new-' + Date.now(), name: q, phone: '—', points: 0, orders: 0, fresh: true }); }}>
                <Icon name="user-plus" size={12} /> Cadastro rápido
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// React-controlled accordion for mode details (mobile-collapsible, desktop-always-open)
function ModeAccordion({ mode, details, customer, onChange }) {
  const [open, setOpen] = useState(false);
  // On desktop the header is hidden (CSS), so 'expanded' here is irrelevant
  // — we always render the body. On mobile, header is the toggle.
  const expanded = open;

  const modeMeta = {
    balcao:   { label: 'Balcão',   icon: 'store' },
    delivery: { label: 'Entrega',  icon: 'truck' },
    retirada: { label: 'Retirada', icon: 'bag' },
    mesa:     { label: 'Mesa',     icon: 'table' },
    comanda:  { label: 'Comanda',  icon: 'print' },
  }[mode] || { label: mode, icon: 'store' };

  // Match the typed neighborhood against defaults + customs to surface fee in the summary
  const matchedHood = mode === 'delivery' && details?.neighborhood
    ? (() => {
        const customs = (typeof window !== 'undefined' && window.HOOD_LS_LASTFEE) || {};
        const needle = details.neighborhood.trim().toLowerCase();
        const defaults = (window.NEIGHBORHOODS_DEFAULT || []);
        const matched = defaults.find(n => n.name.toLowerCase() === needle);
        if (matched) return matched;
        // fallback: any stored last-fee
        return null;
      })()
    : null;
  const deliveryFee = mode === 'delivery'
    ? (matchedHood?.fee ?? (parseFloat(details?.customFee) || null))
    : null;

  const summary =
    mode === 'delivery' && details.neighborhood
      ? details.neighborhood + (details.street ? ' · ' + details.street : '')
      : mode === 'mesa' && details.tableNum
        ? `Mesa ${details.tableNum}${details.covers ? ' · ' + details.covers + ' pess.' : ''}`
        : mode === 'retirada' && details.pickupName
          ? details.pickupName
          : mode === 'comanda' && details.comandaNum
            ? `Comanda #${details.comandaNum}`
            : mode === 'balcao'
              ? 'Consumo no local'
              : 'Toque para configurar';

  return (
    <div className={'mode-acc ' + (expanded ? 'is-open' : '')}>
      <button className="mode-acc-head" onClick={() => setOpen(o => !o)} type="button">
        <span className="mode-acc-ico"><Icon name={modeMeta.icon} size={13} /></span>
        <span className="mode-acc-title">{modeMeta.label}</span>
        <span className="mode-acc-meta">{summary}</span>
        {deliveryFee != null && (
          <span className="mode-acc-fee">{BRL(deliveryFee)}</span>
        )}
        <svg className="mode-acc-caret" width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>
      <div className="mode-acc-body">
        <ModeDetailsPanel mode={mode} details={details} customer={customer} onChange={onChange} />
      </div>
    </div>
  );
}

function ProductRow({ p, onSelect, onInspect }) {
  return (
    <button className="product-row" onClick={(e) => {
      if (e.shiftKey && onInspect) {
        e.preventDefault();
        const rect = e.currentTarget.getBoundingClientRect();
        onInspect(p, { left: rect.left, right: rect.right, top: rect.top, width: rect.width });
        return;
      }
      onSelect(p);
    }}>
      <span className={'product-row-cat cat-' + p.cat}>
        {p.cat === 'pizza' ? 'PZ' : p.cat === 'burger' ? 'BG' : p.cat === 'drink' ? 'DR' : p.cat === 'dessert' ? 'SB' : 'EN'}
      </span>
      <span className="product-row-code">{p.code || '—'}</span>
      <span className="product-row-name">
        {p.name}
        {p.tag && <span className="product-row-tag">{p.tag}</span>}
      </span>
      <span className="product-row-price">{BRL(p.price)}</span>
      <span className="product-row-add"><Icon name="plus" size={13} /></span>
    </button>
  );
}

function ProductCard({ p, onSelect, onInspect }) {
  return (
    <button className="product-card" onClick={(e) => {
      if (e.shiftKey && onInspect) {
        e.preventDefault();
        const rect = e.currentTarget.getBoundingClientRect();
        onInspect(p, { left: rect.left, right: rect.right, top: rect.top, width: rect.width });
        return;
      }
      onSelect(p);
    }}>
      <div className="product-thumb" data-label={p.tag}>
        <span style={{ fontSize: 20, color: 'var(--fg-soft)' }}>
          {p.cat === 'pizza' ? '◐' : p.cat === 'burger' ? '◯' : p.cat === 'drink' ? '◇' : '◈'}
        </span>
      </div>
      <div className="product-name">{p.name}</div>
      <div className="product-meta">
        <span className="product-price">{BRL(p.price)}</span>
        {p.configurable && <span className="product-tag">{p.size}</span>}
      </div>
    </button>
  );
}

function CartItem({ item, onInc, onDec, onRemove, onUpdate, onAdjust }) {
  const [editingFlavor, setEditingFlavor] = React.useState(null); // { code, idx }
  const [showGenericEdit, setShowGenericEdit] = React.useState(false);
  const [showBordaEdit, setShowBordaEdit] = React.useState(false);
  const [noteDraft, setNoteDraft] = React.useState((item.notes || []).join('\n'));

  const isPizza = !!item.config;
  const cfg = item.config || {};

  // Recalculate pizza price from config
  const recalcPizzaPrice = (newCfg) => {
    const bordaObj = (window.BORDAS || []).find(b => b.id === newCfg.bordaId);
    const bordaPrice = bordaObj ? bordaObj.price : 0;
    const flavCustoms = newCfg.flavorCustomizations || {};
    const flavExtrasTotal = Object.values(flavCustoms).reduce((s, fc) => s + (fc.extrasDelta || 0), 0);
    return (newCfg.basePrice || item.price) + bordaPrice + flavExtrasTotal;
  };

  const onSaveFlavor = (code, data) => {
    const newCustomizations = { ...(cfg.flavorCustomizations || {}), [code]: data };
    const newCfg = { ...cfg, flavorCustomizations: newCustomizations };
    const newPrice = recalcPizzaPrice(newCfg);
    // Rebuild notes
    const notes = buildPizzaNotes(newCfg);
    onUpdate({ config: newCfg, price: newPrice, notes });
    setEditingFlavor(null);
  };

  const buildPizzaNotes = (c) => {
    const notes = [];
    const bordaObj = (window.BORDAS || []).find(b => b.id === c.bordaId);
    if (bordaObj && bordaObj.id !== 'none') notes.push('Borda ' + bordaObj.name);
    const customs = c.flavorCustomizations || {};
    (c.flavorCodes || []).forEach(code => {
      const fl = (window.FLAVORS || []).find(f => f.code === code);
      const fc = customs[code];
      if (fc) {
        if (fc.removed && fc.removed.length) notes.push('½ ' + fl?.name + ': sem ' + fc.removed.join(', '));
        if (fc.extras && fc.extras.length) {
          const extNames = fc.extras.map(e => (window.FLAVOR_EXTRAS || []).find(x => x.id === e.id)?.name).filter(Boolean);
          if (extNames.length) notes.push('½ ' + fl?.name + ': + ' + extNames.join(', '));
        }
        if (fc.note) notes.push('½ ' + fl?.name + ': ' + fc.note);
      }
    });
    return notes;
  };

  if (isPizza) {
    const bordaObj = (window.BORDAS || []).find(b => b.id === cfg.bordaId);
    const sizeObj = (window.PIZZA_SIZES || []).find(s => s.size === cfg.size);
    const customs = cfg.flavorCustomizations || {};
    const flavorCount = (cfg.flavorCodes || []).length;

    return (
      <div className="ci ci-pizza">
        <div className="ci-head">
          <div className="ci-pizza-title">
            <span className="ci-pizza-size">{sizeObj?.name || cfg.size}</span>
            <span className="ci-pizza-label">Pizza</span>
          </div>
          <div className="ci-price">{BRL(item.price * item.qty)}</div>
        </div>

        <div className="ci-flavors">
          {(cfg.flavorCodes || []).map((code, idx) => {
            const fl = (window.FLAVORS || []).find(f => f.code === code);
            if (!fl) return null;
            const fc = customs[code] || {};
            const hasCustom = (fc.removed && fc.removed.length > 0) || (fc.extras && fc.extras.length > 0) || fc.note;
            const extrasDelta = fc.extrasDelta || 0;
            const baseFlPrice = (fl.prices && fl.prices[cfg.size]) || 0;
            return (
              <div key={code} className="ci-flavor-row" onClick={() => setEditingFlavor({ code, idx })}>
                <div className="ci-flavor-left">
                  <span className="ci-flavor-half">{flavorCount > 1 ? `½` : '●'}</span>
                  <div>
                    <div className="ci-flavor-name">{fl.name}</div>
                    {hasCustom ? (
                      <div className="ci-flavor-mods">
                        {fc.removed && fc.removed.length > 0 && (
                          <span className="ci-mod ci-mod-rm">− {fc.removed.join(', ')}</span>
                        )}
                        {fc.extras && fc.extras.length > 0 && (
                          <span className="ci-mod ci-mod-add">
                            + {fc.extras.map(e => (window.FLAVOR_EXTRAS || []).find(x => x.id === e.id)?.name).filter(Boolean).join(', ')}
                          </span>
                        )}
                        {fc.note && <span className="ci-mod ci-mod-note">✎ {fc.note}</span>}
                      </div>
                    ) : (
                      <div className="ci-flavor-desc">{fl.desc}</div>
                    )}
                  </div>
                </div>
                <div className="ci-flavor-right">
                  {extrasDelta > 0 && <span className="ci-flavor-delta">+{BRL(extrasDelta)}</span>}
                  <span className="ci-flavor-cta">Personalizar ›</span>
                </div>
              </div>
            );
          })}
        </div>

        {bordaObj && bordaObj.id !== 'none' && (
          <div className="ci-borda-row">
            <span>Borda {bordaObj.name}</span>
            <span className="ci-borda-price">+{BRL(bordaObj.price)}</span>
          </div>
        )}

        <div className="ci-foot">
          <div className="ci-qty">
            <button onClick={onDec}><Icon name="minus" size={12} /></button>
            <span className="qty-val">{item.qty}</span>
            <button onClick={onInc}><Icon name="plus" size={12} /></button>
          </div>
          <button className="ci-obs-btn" onClick={() => setShowBordaEdit(s => !s)}>
            <Icon name="pizza" size={13} /> Borda{bordaObj && bordaObj.id !== 'none' ? ': ' + bordaObj.name : ''}
          </button>
          <AdjustmentButton adjustment={item.adjustment} onClick={() => onAdjust(item)} compact />
          <button className="ci-remove" onClick={onRemove}><Icon name="trash" size={13} /></button>
        </div>

        {showBordaEdit && (
          <div className="ci-obs-editor">
            <div className="fe-section-head" style={{ marginBottom: 10 }}>
              <span className="fe-section-label">BORDA</span>
            </div>
            <div className="fe-chips">
              {(window.BORDAS || []).map(b => (
                <button key={b.id}
                  className={'fe-chip ' + (cfg.bordaId === b.id ? 'on' : 'off-neutral')}
                  style={cfg.bordaId !== b.id ? { borderColor: 'var(--line)', textDecoration: 'none', color: 'var(--fg)' } : {}}
                  onClick={() => {
                    const newCfg = { ...cfg, bordaId: b.id };
                    const newPrice = recalcPizzaPrice(newCfg);
                    const notes = buildPizzaNotes(newCfg);
                    onUpdate({ config: newCfg, price: newPrice, notes });
                    setShowBordaEdit(false);
                  }}>
                  {b.name}
                  {b.price > 0 && <span className="cie-chip-price">+{BRL(b.price)}</span>}
                </button>
              ))}
            </div>
          </div>
        )}

        {editingFlavor && (() => {
          const fl = (window.FLAVORS || []).find(f => f.code === editingFlavor.code);
          const idx = (cfg.flavorCodes || []).indexOf(editingFlavor.code);
          return (
            <FlavorEditor
              flavor={fl}
              partition={idx + 1}
              custom={customs[editingFlavor.code] || {}}
              onSave={(data) => onSaveFlavor(editingFlavor.code, data)}
              onClose={() => setEditingFlavor(null)}
            />
          );
        })()}
      </div>
    );
  }

  // Regular (non-pizza) item
  const cat = item.cat;
  const canPersonalize = cat === 'burger' || cat === 'entry' || cat === 'dessert';
  // Find product metadata (tag like LATA, 355ML, etc.)
  const prodMeta = (window.PRODUCTS || []).find(p => p.id === item.pid);
  const tag = prodMeta?.tag;
  const catLabel = { drink: 'Bebida', burger: 'Burger', dessert: 'Sobremesa', entry: 'Entrada' }[cat] || '';
  const showUnit = item.qty > 1;

  return (
    <div className={'ci ci-regular ' + (showGenericEdit ? 'ci-editing' : '')}>
      <div className="ci-head">
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="ci-regular-name">
            <span>{item.name}</span>
            {tag && <span className={'ci-tag ' + cat}>{tag}</span>}
          </div>
          {(catLabel || showUnit) && (
            <div className="ci-regular-meta">
              {catLabel && <span>{catLabel}</span>}
              {catLabel && showUnit && <span className="dot" />}
              {showUnit && <span>{item.qty} × {BRL(item.price)}</span>}
            </div>
          )}
        </div>
        <div className="ci-price-block">
          <div className="ci-price">{BRL(item.price * item.qty)}</div>
        </div>
      </div>
      {item.notes && item.notes.length > 0 && (
        <ul className="ci-notes">
          {item.notes.map((n, i) => <li key={i}>{n}</li>)}
        </ul>
      )}
      <div className="ci-foot">
        <div className="ci-qty">
          <button onClick={onDec}><Icon name="minus" size={12} /></button>
          <span className="qty-val">{item.qty}</span>
          <button onClick={onInc}><Icon name="plus" size={12} /></button>
        </div>
        <button className="ci-obs-btn" onClick={() => { setShowGenericEdit(o => !o); setNoteDraft((item.notes || []).join('\n')); }}>
          <Icon name={canPersonalize ? 'edit' : 'menu'} size={13} />
          {canPersonalize ? 'Personalizar' : 'Observação'}
        </button>
        <AdjustmentButton adjustment={item.adjustment} onClick={() => onAdjust(item)} compact />
        <button className="ci-remove" onClick={onRemove}><Icon name="trash" size={13} /></button>
      </div>
      {showGenericEdit && (
        <div className="ci-obs-editor">
          <div className="fe-section-head" style={{ marginBottom: 8 }}>
            <span className="fe-section-label">OBSERVAÇÕES</span>
            <span className="fe-section-sub">uma por linha</span>
          </div>
          <textarea
            className="fe-note"
            value={noteDraft}
            onChange={e => setNoteDraft(e.target.value)}
            placeholder={'Ex: sem cebola\nmolho à parte\nbem passado'}
            rows={3}
            autoFocus
          />
          <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 8 }}>
            <button className="btn btn-ghost" onClick={() => setShowGenericEdit(false)}>Cancelar</button>
            <button className="btn btn-primary" onClick={() => {
              onUpdate({ notes: noteDraft.split('\n').map(s => s.trim()).filter(Boolean) });
              setShowGenericEdit(false);
            }}>Salvar</button>
          </div>
        </div>
      )}
    </div>
  );
}

function PDVView({ state, dispatch, onOpenPizza, cartOpen, setCartOpen, recent, onOrderSent, pausedOrders, onPause, onResumePaused, onClosePaused, currentTabLabel, cartCount, cartTotal, onShowHelp, loyaltyApplied, setLoyaltyApplied }) {
  const [cat, setCat] = useState('all');
  const [q, setQ] = useState('');
  const [showPayment, setShowPayment] = useState(false);
  const [showPrechk, setShowPrechk] = useState(false);
  const [showCatalog, setShowCatalog] = useState(false);
  const [toast, setToast] = useState('');
  const [removeReason, setRemoveReason] = useState(null);
  const [inspect, setInspect] = useState(null);
  const [adjustTarget, setAdjustTarget] = useState(null); // { kind: 'item'|'order', itemId?, label, base, current }
  const modeDetails = state.modeDetails || {};

  // Toast helper exposed globally for OmniBox
  useEffect(() => {
    window.__omniToast = (msg) => {
      setToast(msg);
      setTimeout(() => setToast(''), 2200);
    };
    return () => { delete window.__omniToast; };
  }, []);

  // Global F-key shortcuts
  useEffect(() => {
    const onKey = (e) => {
      if (e.key === 'F5')      { e.preventDefault(); if (state.cart.length > 0) setShowPayment(true); }
      else if (e.key === 'End') { e.preventDefault(); if (state.cart.length > 0) setShowPayment(true); }
      else if (e.key === 'F2') { e.preventDefault(); setShowCatalog(s => !s); }
      else if (e.key === 'Escape' && showCatalog) { setShowCatalog(false); }
      else if (e.key === 'F9') { e.preventDefault(); if (confirm('Cancelar pedido?')) dispatch({ type: 'clear' }); }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [state.cart.length]);

  const filtered = useMemo(() =>
    PRODUCTS.filter(p =>
      (cat === 'all' || p.cat === cat) &&
      (!q || p.name.toLowerCase().includes(q.toLowerCase()))
    ), [cat, q]);

  // Helper: open cancel-reason modal for an item, then dispatch remove on confirm
  const requestRemove = (item) => setRemoveReason(item);
  const confirmRemove = (info) => {
    // info: { reason, note }
    if (removeReason) {
      dispatch({ type: 'remove', id: removeReason.id });
      // (Could log info.reason / info.note to a backend here)
    }
    setRemoveReason(null);
  };

  const onSelect = (p) => {
    if (p.configurable) {
      onOpenPizza(p);
    } else {
      dispatch({ type: 'add', item: { id: p.id + '-' + Date.now(), pid: p.id, name: p.name, price: p.price, cat: p.cat, qty: 1 } });
    }
  };

  // Add from recent rail — look up product, then go through onSelect
  const onAddRecent = (recentItem) => {
    const p = PRODUCTS.find(x => x.id === recentItem.id);
    if (p) onSelect(p);
  };

  // Subtotal honors per-item adjustments
  const itemAdjustments = state.cart.reduce((s, i) =>
    s + applyAdjustment(i.price * i.qty, i.adjustment), 0); // negative for discounts
  const subtotal = state.cart.reduce((s, i) => s + i.price * i.qty, 0) + itemAdjustments;
  const deliveryFee = state.mode === 'delivery' ? 8.0 : 0;
  // Order-level adjustment applies on subtotal + delivery (after item-level)
  const orderAdjustmentValue = applyAdjustment(subtotal + deliveryFee, state.orderAdjustment);
  // Loyalty discount calculations
  const pointsDiscount = (loyaltyApplied.points && state.customer?.points) ? state.customer.points * 0.05 : 0;
  const couponDiscount = (loyaltyApplied.coupon && state.customer?.coupon) ? (state.customer.coupon.value || 0) : 0;
  const totalDiscount = pointsDiscount + couponDiscount;
  const total = Math.max(0, subtotal + deliveryFee + orderAdjustmentValue - totalDiscount);

  return (
    <>
      <div className="pdv">
      <div className="pdv-left">
        {/* Mobile-only customer strip — searchable / card with delivery address.
            Desktop keeps the customer in the right sidebar. */}
        <div className="pdv-mobile-customer-strip">
          <CustomerSearch
            value={state.customer}
            onPick={(c) => dispatch({ type: 'customer', customer: c })}
            onClear={() => dispatch({ type: 'customer', customer: null })}
            expanded
            deliveryMode={state.mode === 'delivery'}
            onChangeAddress={() => setCartOpen(true)} />
          {/* Mode selector (compact strip) */}
          {state.cart.length > 0 && (
            <div className="pdv-mobile-mode-strip">
              {[
                { id: 'balcao',   label: 'Balcão',   icon: 'store' },
                { id: 'delivery', label: 'Delivery', icon: 'truck' },
                { id: 'retirada', label: 'Retirada', icon: 'bag' },
                { id: 'mesa',     label: 'Mesa',     icon: 'table' },
                { id: 'comanda',  label: 'Comanda',  icon: 'print' },
              ].map(m => (
                <button key={m.id}
                  className={'pdv-mobile-mode ' + (state.mode === m.id ? 'active' : '')}
                  onClick={() => dispatch({ type: 'mode', mode: m.id })}>
                  <Icon name={m.icon} size={13} />
                  <span>{m.label}</span>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Paused orders bar — lives inside the cart column so the right
            sidebar can extend all the way to the top of the PDV. */}
        <PausedOrdersBar
          orders={pausedOrders}
          current={{ label: currentTabLabel, count: cartCount, total: cartTotal }}
          onResume={onResumePaused}
          onClose={onClosePaused}
          onPauseCurrent={onPause}
          hasItems={state.cart.length > 0} />

        <OmniBox
          onAdd={(item) => dispatch({ type: 'add', item })}
          onCustomer={(c) => dispatch({ type: 'customer', customer: c })}
          onTable={(n) => { dispatch({ type: 'mode', mode: 'mesa' }); dispatch({ type: 'modeDetails', details: { ...modeDetails, tableNum: n } }); }}
          onPay={() => setShowPayment(true)}
        />

        {/* Recent / favorite products quick-add rail */}
        <FavoritesRail recent={recent} onAdd={onAddRecent} />

        <div className="pdv-cart-main">
          <div className="pdv-cart-main-head">
            <div>
              <div className="pdv-cart-main-title">Itens do pedido</div>
              <div className="pdv-cart-main-sub">{state.cart.length === 0 ? 'Nenhum item' : `${state.cart.reduce((n,i)=>n+i.qty,0)} item(s) · ${state.cart.length} linha(s)`}</div>
            </div>
            <div className="pdv-cart-main-actions">
              <button className="btn" onClick={() => setShowCatalog(true)}>
                <Icon name="menu" size={14} /> Cardápio <span className="kbd-soft">F2</span>
              </button>
              {state.cart.length > 0 && (
                <button className="btn btn-ghost" onClick={() => { if (confirm('Cancelar pedido?')) dispatch({ type: 'clear' }); }}>
                  <Icon name="trash" size={14} /> Limpar <span className="kbd-soft">F9</span>
                </button>
              )}
            </div>
          </div>

          <div className="pdv-cart-list">
            {state.cart.length === 0 ? (
              <EmptyState
                icon="cart"
                title="Comece o pedido"
                sub="Digite o código no Omni Box acima ou abra o catálogo completo"
                ctaLabel="Abrir catálogo"
                ctaIcon="menu"
                onCta={() => setShowCatalog(true)}
                secondaryLabel="Ver atalhos (?)"
                onSecondary={onShowHelp} />
            ) : (
              <div className="cart-list-group">
                {state.cart.map((item) =>
                <CartItem key={item.id}
                item={item}
                onInc={() => dispatch({ type: 'qty', id: item.id, delta: 1 })}
                onDec={() => dispatch({ type: 'qty', id: item.id, delta: -1 })}
                onUpdate={(patch) => dispatch({ type: 'update', id: item.id, patch })}
                onAdjust={(it) => setAdjustTarget({
                  kind: 'item',
                  itemId: it.id,
                  label: it.name,
                  base: it.price * it.qty,
                  current: it.adjustment,
                })}
                onRemove={() => requestRemove(item)} />
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className={'pdv-right ' + (cartOpen ? 'open' : '')}>
        {cartOpen && <div className="pdv-right-backdrop" onClick={() => setCartOpen(false)} style={{ display: window.innerWidth <= 760 ? 'block' : 'none' }} />}
        <div className="cart-head">
          <CustomerSearch
            value={state.customer}
            onPick={(c) => dispatch({ type: 'customer', customer: c })}
            onClear={() => dispatch({ type: 'customer', customer: null })}
          />
          <div className="cart-modes">
            {[
              { id: 'balcao',   label: 'Balcão',   icon: 'store' },
              { id: 'delivery', label: 'Delivery', icon: 'truck' },
              { id: 'retirada', label: 'Retirada', icon: 'bag' },
              { id: 'mesa',     label: 'Mesa',     icon: 'table' },
              { id: 'comanda',  label: 'Comanda',  icon: 'print' },
            ].map(m => (
              <button key={m.id}
                className={'cart-mode ' + (state.mode === m.id ? 'active' : '')}
                onClick={() => dispatch({ type: 'mode', mode: m.id })}>
                <Icon name={m.icon} size={16} />
                <span>{m.label}</span>
              </button>
            ))}
          </div>
        </div>

        <ModeAccordion mode={state.mode} details={modeDetails} customer={state.customer}
          onChange={(d) => dispatch({ type: 'modeDetails', details: d })} />

        {state.cart.length > 0 && state.customer && state.customer.points > 0 && (
          <LoyaltyMini
            customer={state.customer}
            applied={loyaltyApplied}
            onApplyPoints={(on) => setLoyaltyApplied(a => ({ ...a, points: on }))}
            onApplyCoupon={(on) => setLoyaltyApplied(a => ({ ...a, coupon: on }))} />
        )}
      </div>

      {/* Footer — totals + finalize spanning full PDV width */}
      <footer className="pdv-footer">
        {state.cart.length === 0 ? (
          <div className="pdv-footer-empty">
            <span>Pedido vazio · digite um código no <kbd>Omni Box</kbd> para começar</span>
            <span><kbd>?</kbd> ver atalhos</span>
          </div>
        ) : (
          <>
            <div className="pdv-footer-meta">
              <button className="pdv-sheet-trigger" onClick={() => setCartOpen(true)} style={{ display: 'none' }}>
                <Icon name="user" size={14} />
                <span>{state.customer ? state.customer.name.split(' ')[0] : 'Cliente'}</span>
                <span className="meta">· {state.mode === 'balcao' ? 'Balcão' : state.mode === 'delivery' ? 'Delivery' : state.mode === 'retirada' ? 'Retirada' : state.mode === 'mesa' ? 'Mesa' : 'Comanda'}</span>
              </button>
              <span className="pdv-footer-meta-grp">
                <span className="pdv-footer-meta-label">Itens</span>
                <span className="pdv-footer-meta-val">{cartCount}</span>
              </span>
              <span className="pdv-footer-divider" />
              <span className="pdv-footer-meta-grp">
                <span className="pdv-footer-meta-label">Subtotal</span>
                <span className="pdv-footer-meta-val">{BRL(subtotal)}</span>
              </span>
              {deliveryFee > 0 && (
                <>
                  <span className="pdv-footer-divider" />
                  <span className="pdv-footer-meta-grp">
                    <span className="pdv-footer-meta-label">Entrega</span>
                    <span className="pdv-footer-meta-val">{BRL(deliveryFee)}</span>
                  </span>
                </>
              )}
              {totalDiscount > 0 && (
                <>
                  <span className="pdv-footer-divider" />
                  <span className="pdv-footer-meta-grp">
                    <span className="pdv-footer-meta-label">Desconto</span>
                    <span className="pdv-footer-meta-val discount">−{BRL(totalDiscount)}</span>
                  </span>
                </>
              )}
              {orderAdjustmentValue !== 0 && (
                <>
                  <span className="pdv-footer-divider" />
                  <span className="pdv-footer-meta-grp">
                    <span className="pdv-footer-meta-label">{state.orderAdjustment?.kind === 'surcharge' ? 'Acréscimo' : 'Desconto pedido'}</span>
                    <span className="pdv-footer-meta-val discount">
                      {orderAdjustmentValue >= 0 ? '+' : '−'}{BRL(Math.abs(orderAdjustmentValue))}
                    </span>
                  </span>
                </>
              )}
            </div>
            <div className="pdv-footer-actions">
              <AdjustmentButton
                adjustment={state.orderAdjustment}
                onClick={() => setAdjustTarget({
                  kind: 'order',
                  label: 'Total do pedido',
                  base: subtotal + deliveryFee,
                  current: state.orderAdjustment,
                })} />
              <div className="pdv-footer-total">
                <span className="pdv-footer-total-label">Total a pagar</span>
                <span className="pdv-footer-total-val">{BRL(total)}</span>
              </div>
              <button className="btn btn-ghost btn-icon" title="Conferência (F4)" onClick={() => setShowPrechk(true)}>
                <Icon name="check" size={18} />
              </button>
              <button className="btn btn-primary"
                onClick={() => setShowPrechk(true)}>
                <Icon name="card" size={16} /> Finalizar
                <span className="kbd-soft" style={{ background: 'rgba(255,255,255,0.18)', borderColor: 'transparent', color: 'rgba(255,255,255,0.85)' }}>F5</span>
              </button>
            </div>
          </>
        )}
      </footer>

      {/* Pre-checkout review modal */}
      <PreCheckoutModal
        open={showPrechk}
        cart={state.cart}
        customer={state.customer}
        mode={state.mode}
        modeDetails={modeDetails}
        subtotal={subtotal}
        deliveryFee={deliveryFee}
        discounts={totalDiscount}
        total={total}
        onCancel={() => setShowPrechk(false)}
        onConfirm={() => { setShowPrechk(false); setShowPayment(true); }} />

      {/* Product inspect popover */}
      {inspect && <ProductInspect product={inspect.product} anchor={inspect.anchor} onClose={() => setInspect(null)} />}

      {showPayment &&
      <PaymentFlow
      total={total}
      customer={state.customer}
      items={state.cart}
      mode={state.mode}
      modeDetails={modeDetails}
      onClose={() => {
        dispatch({ type: 'clear' });
        setCartOpen(false);
        setShowPayment(false);
      }}
      onConfirm={() => {
        if (onOrderSent) onOrderSent(); else dispatch({ type: 'clear' });
      }} />
      }

      {/* Adjustment modal (item or order-level discount/surcharge) */}
      <AdjustmentModal
        open={!!adjustTarget}
        target={adjustTarget}
        current={adjustTarget?.current}
        onConfirm={(adj) => {
          if (adjustTarget.kind === 'item') {
            dispatch({ type: 'itemAdjust', id: adjustTarget.itemId, adjustment: adj });
          } else {
            dispatch({ type: 'orderAdjust', adjustment: adj });
          }
          setAdjustTarget(null);
        }}
        onClear={() => {
          if (adjustTarget.kind === 'item') {
            dispatch({ type: 'itemAdjust', id: adjustTarget.itemId, adjustment: null });
          } else {
            dispatch({ type: 'orderAdjust', adjustment: null });
          }
          setAdjustTarget(null);
        }}
        onCancel={() => setAdjustTarget(null)} />

      {/* Remove-reason confirmation modal */}
      <RemoveReasonModal
        item={removeReason}
        onConfirm={confirmRemove}
        onCancel={() => setRemoveReason(null)} />

      {toast && <div className="omni-toast">{toast}</div>}

      {showCatalog && (
        <div className="catalog-overlay" onClick={(e) => { if (e.target === e.currentTarget) setShowCatalog(false); }}>
          <div className="catalog-modal">
            <div className="catalog-head">
              <div>
                <div className="catalog-title">Cardápio</div>
                <div className="catalog-sub">Toque para adicionar · <kbd>Esc</kbd> para fechar</div>
              </div>
              <div className="catalog-search input-with-ico">
                <span className="ico"><Icon name="search" size={14} /></span>
                <input className="input" placeholder="Buscar produto…" value={q} onChange={e => setQ(e.target.value)} autoFocus />
              </div>
              <button className="btn btn-ghost btn-icon" onClick={() => setShowCatalog(false)}><Icon name="x" size={16} /></button>
            </div>
            <div className="catalog-cats">
              {CATEGORIES.map(c => (
                <button key={c.id}
                  className={'chip ' + (cat === c.id ? 'active' : '')}
                  onClick={() => setCat(c.id)}>
                  {c.name}
                </button>
              ))}
            </div>
            <div className="catalog-body">
              <div className="product-list">
                {filtered.map(p => <ProductRow key={p.id} p={p}
                  onSelect={(prod) => { onSelect(prod); if (!prod.configurable) setShowCatalog(false); }}
                  onInspect={(prod, anchor) => setInspect({ product: prod, anchor })} />)}
                {filtered.length === 0 && (
                  <div style={{ padding: 40, textAlign: 'center', color: 'var(--fg-soft)', fontSize: 13 }}>
                    Nenhum produto encontrado.
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
      </div>
    </>
  );
}

window.PDVView = PDVView;
window.CustomerSearch = CustomerSearch;
