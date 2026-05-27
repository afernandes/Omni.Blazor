// App shell + router + state
const { useState: useAppState, useReducer, useEffect: useAppEffect } = React;

function cartReducer(state, action) {
  switch (action.type) {
    case 'add': {
      const existing = state.cart.find(i => i.pid === action.item.pid && !i.notes);
      if (existing) {
        return { ...state, cart: state.cart.map(i => i.id === existing.id ? { ...i, qty: i.qty + 1 } : i) };
      }
      return { ...state, cart: [...state.cart, action.item] };
    }
    case 'qty': {
      return { ...state, cart: state.cart.map(i => i.id === action.id ? { ...i, qty: Math.max(1, i.qty + action.delta) } : i) };
    }
    case 'remove': return { ...state, cart: state.cart.filter(i => i.id !== action.id) };
    case 'update': return { ...state, cart: state.cart.map(i => i.id === action.id ? { ...i, ...action.patch } : i) };
    case 'itemAdjust': {
      // action.id, action.adjustment: { kind: 'discount'|'surcharge', mode: 'percent'|'fixed', value: number, reason?: string } | null
      return { ...state, cart: state.cart.map(i => i.id === action.id ? { ...i, adjustment: action.adjustment } : i) };
    }
    case 'orderAdjust': {
      // action.adjustment: same shape as itemAdjust (or null to clear)
      return { ...state, orderAdjustment: action.adjustment };
    }
    case 'clear':  return { ...state, cart: [], customer: null, orderAdjustment: null };
    case 'customer': return { ...state, customer: action.customer };
    case 'mode':   return { ...state, mode: action.mode, modeDetails: {} };
    case 'modeDetails': return { ...state, modeDetails: action.details };
    default: return state;
  }
}

// ─── PDV customer head — desktop view-head replacement ─
// Compact button showing avatar+name+phone or empty state. Click opens
// a small popover with the CustomerSearch input.
function PdvCustomerHead({ customer, modeLabel, onPick, onClear }) {
  const [open, setOpen] = useAppState(false);
  const ref = React.useRef(null);
  const popRef = React.useRef(null);

  useAppEffect(() => {
    if (!open) return;
    const onClick = (e) => {
      if (popRef.current?.contains(e.target)) return;
      if (ref.current?.contains(e.target)) return;
      setOpen(false);
    };
    const onKey = (e) => { if (e.key === 'Escape') setOpen(false); };
    document.addEventListener('mousedown', onClick);
    window.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onClick);
      window.removeEventListener('keydown', onKey);
    };
  }, [open]);

  if (customer) {
    const initials = customer.name.split(' ').map(w => w[0]).slice(0,2).join('');
    return (
      <div className="pdv-head-cust" ref={ref}>
        <button className="pdv-head-cust-card"
          onClick={() => setOpen(o => !o)}
          title="Editar ou trocar cliente">
          <div className="pdv-head-cust-av">{initials}</div>
          <div className="pdv-head-cust-info">
            <div className="pdv-head-cust-name">{customer.name}</div>
            <div className="pdv-head-cust-meta">
              <span>{customer.phone}</span>
              {customer.points > 0 && <>
                <span className="dot-sep">·</span>
                <span style={{ color: 'var(--accent)', fontWeight: 600 }}>{customer.points} pts</span>
              </>}
              <span className="dot-sep">·</span>
              <span>{modeLabel}</span>
            </div>
          </div>
        </button>
        <button className="pdv-head-cust-x" onClick={onClear} title="Remover cliente" aria-label="Remover cliente">
          <Icon name="x" size={13} />
        </button>
        {open && (
          <div className="pdv-head-cust-pop" ref={popRef}>
            <div className="pdv-head-cust-pop-label">Trocar cliente</div>
            <CustomerSearch value={null} onPick={(c) => { onPick(c); setOpen(false); }} onClear={onClear} />
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="pdv-head-cust" ref={ref}>
      <button className="pdv-head-cust-empty"
        onClick={() => setOpen(o => !o)}
        title="Buscar cliente">
        <Icon name="phone" size={14} />
        <span>Buscar cliente…</span>
        <kbd className="kbd-soft" style={{ marginLeft: 'auto' }}>F3</kbd>
      </button>
      {open && (
        <div className="pdv-head-cust-pop" ref={popRef}>
          <div className="pdv-head-cust-pop-label">Cliente</div>
          <CustomerSearch value={null} onPick={(c) => { onPick(c); setOpen(false); }} onClear={() => {}} />
        </div>
      )}
    </div>
  );
}

// LoyaltyModal — full popup that lets the operator pick + remove loyalty benefits.
function LoyaltyModal({ open, customer, applied, onApply, onClose }) {
  const ref = React.useRef(null);
  if (!open || !customer) return null;
  const points = customer.points || 0;
  const pointsValue = points * 0.05;
  const coupon = customer.coupon;
  const couponValue = coupon?.value || 0;
  const savings =
    (applied?.points ? pointsValue : 0) +
    (applied?.coupon ? couponValue : 0);

  const togglePoints = () => onApply({ ...applied, points: !applied?.points });
  const toggleCoupon = () => onApply({ ...applied, coupon: !applied?.coupon });
  const clear = () => onApply({ points: false, coupon: false });

  return (
    <div className="loy-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="loy-modal" ref={ref} role="dialog" aria-label="Fidelidade">
        <div className="loy-head">
          <div>
            <div className="loy-eyebrow">FIDELIDADE</div>
            <div className="loy-title">{customer.name.split(' ')[0]} · {points} pontos</div>
          </div>
          <button className="prechk-close" onClick={onClose} aria-label="Fechar">
            <Icon name="x" size={18} />
          </button>
        </div>

        <div className="loy-body">
          <div className="loy-option">
            <label className="loy-option-row">
              <input type="checkbox" checked={!!applied?.points} onChange={togglePoints} disabled={points === 0} />
              <div className="loy-option-info">
                <div className="loy-option-title">Usar {points} pontos</div>
                <div className="loy-option-sub">Vale R$ {pointsValue.toFixed(2).replace('.', ',')} no pedido</div>
              </div>
              <div className="loy-option-val">−R$ {pointsValue.toFixed(2).replace('.', ',')}</div>
            </label>
          </div>

          {coupon && (
            <div className="loy-option">
              <label className="loy-option-row">
                <input type="checkbox" checked={!!applied?.coupon} onChange={toggleCoupon} />
                <div className="loy-option-info">
                  <div className="loy-option-title">{coupon.label}</div>
                  <div className="loy-option-sub">Expira {coupon.expires}</div>
                </div>
                <div className="loy-option-val">−{coupon.discount}</div>
              </label>
            </div>
          )}
        </div>

        <div className="loy-foot">
          <div className="loy-foot-savings">
            <span className="lbl">Economia</span>
            <span className="val">R$ {savings.toFixed(2).replace('.', ',')}</span>
          </div>
          <div style={{ flex: 1 }} />
          {savings > 0 && (
            <button className="btn btn-ghost" onClick={clear} style={{ color: 'var(--danger)' }}>
              Remover tudo
            </button>
          )}
          <button className="btn btn-primary" onClick={onClose}>
            {savings > 0 ? 'Aplicar' : 'Pronto'}
          </button>
        </div>
      </div>
    </div>
  );
}

function App() {
  const [view, setView] = useAppState('pdv');
  const [pizzaProduct, setPizzaProduct] = useAppState(null);
  const [toast, setToast] = useAppState(null);
  const [editMode, setEditMode] = useAppState(false);
  const [tweaks, setTweaks] = useAppState(window.TWEAKS);
  const [navOpen, setNavOpen] = useAppState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useAppState(() => {
    try { return localStorage.getItem('app:sidebar-collapsed') === '1'; } catch { return false; }
  });
  const [cartOpen, setCartOpen] = useAppState(false);
  const [showHelp, setShowHelp] = useAppState(false);
  const [showNotif, setShowNotif] = useAppState(false);
  const [showReceipt, setShowReceipt] = useAppState(false);
  const [showTour, setShowTour] = useAppState(() => {
    try { return localStorage.getItem('pdv:tour-done') !== '1'; } catch { return false; }
  });
  const [pausedOrders, setPausedOrders] = useAppState([]);
  const [orderCounter, setOrderCounter] = useAppState(8417);
  const [loyaltyApplied, setLoyaltyApplied] = useAppState({ points: false, coupon: false });
  const [loyaltyOpen, setLoyaltyOpen] = useAppState(false);

  // Track recent products for the favorites rail
  const { recent, trackAdd } = useRecentProducts();

  // Initial cart
  const [state, dispatch] = useReducer(cartReducer, {
    cart: [
      { id: 'seed1', pid: 'bg-1', name: 'Cheeseburger Duplo', price: 28.00, qty: 2 },
      { id: 'seed2', pid: 'dr-1', name: 'Coca-Cola 350ml', price: 7.50, qty: 2 },
    ],
    customer: CUSTOMERS[0],
    mode: 'balcao',
    modeDetails: {},
  });

  // apply theme / accent / density
  useAppEffect(() => {
    document.documentElement.dataset.accent = tweaks.accent || 'crimson';
    document.documentElement.dataset.theme = tweaks.dark ? 'dark' : 'light';
    document.documentElement.dataset.density = tweaks.density || 'comfort';
  }, [tweaks]);

  // Edit mode protocol
  useAppEffect(() => {
    const onMsg = (e) => {
      if (!e.data || !e.data.type) return;
      if (e.data.type === '__activate_edit_mode') setEditMode(true);
      if (e.data.type === '__deactivate_edit_mode') setEditMode(false);
    };
    window.addEventListener('message', onMsg);
    window.parent.postMessage({ type: '__edit_mode_available' }, '*');
    return () => window.removeEventListener('message', onMsg);
  }, []);

  // Global shortcuts — Help (?, F1) + Pause order (Ctrl+N) + mode (Ctrl+1..5)
  useAppEffect(() => {
    const onKey = (e) => {
      // Don't grab keys when typing in an input
      const inField = e.target.matches?.('input,textarea,select') && !e.target.matches('input[type="checkbox"]');
      if (inField) return;

      if (e.key === '?' || e.key === 'F1') {
        e.preventDefault();
        setShowHelp(s => !s);
      } else if (e.key === 'Escape' && showHelp) {
        setShowHelp(false);
      } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'n' && view === 'pdv') {
        e.preventDefault();
        pauseCurrent();
      } else if ((e.ctrlKey || e.metaKey) && /^[1-5]$/.test(e.key) && view === 'pdv') {
        e.preventDefault();
        const modes = ['balcao', 'delivery', 'retirada', 'mesa', 'comanda'];
        dispatch({ type: 'mode', mode: modes[parseInt(e.key, 10) - 1] });
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [showHelp, view, state]);

  // Hamburger handler — desktop toggles collapse, mobile toggles drawer
  const onHamburger = () => {
    if (window.innerWidth <= 760) {
      setNavOpen(v => !v);
    } else {
      setSidebarCollapsed(v => {
        const next = !v;
        try { localStorage.setItem('app:sidebar-collapsed', next ? '1' : '0'); } catch {}
        return next;
      });
    }
  };

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), 1800);
  };

  // ─── Multi-pedido: pausar e retomar ─────────────
  const cartTotal = state.cart.reduce((s, i) => s + i.price * i.qty, 0);
  const cartCount = state.cart.reduce((n, i) => n + i.qty, 0);
  const currentTabLabel = state.customer
    ? state.customer.name.split(' ').slice(0, 2).join(' ')
    : (state.mode === 'mesa' && state.modeDetails?.tableNum
        ? `Mesa ${state.modeDetails.tableNum}`
        : 'Pedido atual');

  const pauseCurrent = () => {
    if (state.cart.length === 0) {
      showToast('Nada para pausar — pedido vazio');
      return;
    }
    const snapshot = {
      id: 'p-' + Date.now(),
      label: currentTabLabel,
      icon: state.mode === 'delivery' ? 'truck' : state.mode === 'mesa' ? 'table' : 'cart',
      cart: state.cart,
      customer: state.customer,
      mode: state.mode,
      modeDetails: state.modeDetails,
      count: cartCount,
      total: cartTotal,
      pausedAt: new Date().toISOString(),
    };
    setPausedOrders(p => [...p, snapshot]);
    dispatch({ type: 'clear' });
    showToast('Pedido pausado · iniciando novo');
  };

  const resumePaused = (pausedId) => {
    const paused = pausedOrders.find(o => o.id === pausedId);
    if (!paused) return;
    // If current cart has items, push it to paused first
    if (state.cart.length > 0) {
      const current = {
        id: 'p-' + Date.now(),
        label: currentTabLabel,
        icon: state.mode === 'delivery' ? 'truck' : state.mode === 'mesa' ? 'table' : 'cart',
        cart: state.cart,
        customer: state.customer,
        mode: state.mode,
        modeDetails: state.modeDetails,
        count: cartCount,
        total: cartTotal,
        pausedAt: new Date().toISOString(),
      };
      setPausedOrders(p => [...p.filter(o => o.id !== pausedId), current]);
    } else {
      setPausedOrders(p => p.filter(o => o.id !== pausedId));
    }
    // Restore the picked one
    dispatch({ type: 'clear' });
    setTimeout(() => {
      paused.cart.forEach(item => dispatch({ type: 'add', item }));
      dispatch({ type: 'customer', customer: paused.customer });
      dispatch({ type: 'mode', mode: paused.mode });
      dispatch({ type: 'modeDetails', details: paused.modeDetails });
    }, 10);
    showToast('Pedido retomado');
  };

  const closePaused = (pausedId) => {
    setPausedOrders(p => p.filter(o => o.id !== pausedId));
    showToast('Pedido descartado');
  };

  const navItems = [
    { id: 'pdv',       label: 'PDV',              icon: 'pos',    count: state.cart.length || null, group: 'op' },
    { id: 'pizza',     label: 'Montar Pizza',     icon: 'pizza',                                    group: 'op' },
    { id: 'hub',       label: 'HUB de Pedidos',   icon: 'hub',    count: 10,                        group: 'op' },
    { id: 'menu',      label: 'Cardápio Digital', icon: 'menu',                                     group: 'op' },
    { id: 'waiter',    label: 'Garçom',           icon: 'waiter',                                   group: 'floor' },
    { id: 'cadastros', label: 'Cadastros',        icon: 'store',                                    group: 'mgmt' },
    { id: 'admin',     label: 'Configurações',    icon: 'admin',                                    group: 'tenant' },
  ];

  const openPizza = (product) => {
    setPizzaProduct(product);
    setView('pizza');
  };

  const confirmPizza = (item) => {
    dispatch({ type: 'add', item });
    showToast('Pizza adicionada ao pedido');
    setPizzaProduct(null);
    setView('pdv');
  };

  // Wrap dispatch to track recents on 'add'
  const wrappedDispatch = (action) => {
  const showToast = (msg) => {
      // Find product metadata to track
      const p = PRODUCTS.find(p => p.id === action.item.pid);
      if (p) trackAdd(p);
    }
    return dispatch(action);
  };

  // Order completion with toast feedback
  const onOrderSent = () => {
    const orderId = orderCounter;
    setOrderCounter(c => c + 1);
    showToast({ kind: 'order', id: orderId, msg: 'Pedido enviado à cozinha' });
    dispatch({ type: 'clear' });
  };

  const modeLabels = { balcao: 'Balcão', delivery: 'Delivery', retirada: 'Retirada', mesa: 'Mesa', comanda: 'Comanda' };
  const pdvSubText = state.customer
    ? `${state.customer.name.split(' ')[0]} · ${modeLabels[state.mode]}`
    : (state.mode === 'mesa' && state.modeDetails?.tableNum)
      ? `Mesa ${state.modeDetails.tableNum}`
      : 'Caixa #02 · aberto 08:12';

  const titles = {
    pdv:       { title: 'Frente de Caixa', sub: pdvSubText },
    pizza:     { title: 'Montar Pizza', sub: 'Partição · sabores · adicionais' },
    hub:       { title: 'HUB de Pedidos', sub: '10 ativos · 4 canais' },
    menu:      { title: 'Cardápio Digital', sub: 'Preview do self-service acessado por QR / link' },
    waiter:    { title: 'Módulo Garçom', sub: 'Tablet 10" — mesas, comandas, envio para cozinha' },
    cadastros: { title: 'Cadastros', sub: 'Produtos, sabores, ingredientes, clientes, entregadores, promoções e zonas de entrega' },
    admin:     { title: 'Configurações do Tenant', sub: 'Plano, domínio, papéis e integrações' },
  };

  return (
    <div className={'app ' + (tweaks.pdvLayout === 'grid' ? 'layout-grid ' : '') + (sidebarCollapsed ? 'sidebar-collapsed' : '')}>
      <div className="topbar">
        <button className="mobile-menu-btn" onClick={onHamburger} aria-label={sidebarCollapsed ? 'Expandir menu' : 'Colapsar menu'} title={sidebarCollapsed ? 'Expandir menu' : 'Colapsar menu'}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M3 6h18M3 12h18M3 18h18"/></svg>
        </button>
        <div className="brand">
          <div className="brand-mark">F</div>
          <span className="brand-name">Forneria</span>
          <span className="brand-slash">/</span>
          <span className="brand-tenant">Forno do Bairro — Unidade Vila Madalena</span>
        </div>
        <div className="topbar-spacer"></div>
        <div className="topbar-kbd">
          <span className="kbd">⌘K</span>
          <span>Buscar pedido, cliente ou produto</span>
        </div>
        <div className="topbar-user">
          <div style={{ position: 'relative' }}>
            <NotificationCenter
              open={showNotif}
              onToggle={() => setShowNotif(s => !s)}
              onClose={() => setShowNotif(false)} />
          </div>
          <button className="help-trigger" onClick={() => setShowHelp(true)} title="Atalhos (?)" aria-label="Atalhos do teclado">?</button>
          <div className="avatar">CR</div>
          <div className="topbar-user-meta">
            <div className="topbar-user-name">Carla Rocha</div>
            <div className="topbar-user-role">Caixa · PDV-02</div>
          </div>
        </div>
      </div>

      {navOpen && <div className="sidebar-backdrop" onClick={() => setNavOpen(false)} />}
      <aside className={'sidebar ' + (navOpen ? 'open' : '')} onClick={() => setNavOpen(false)}>
        {[
          { group: 'op',     label: 'Operação' },
          { group: 'floor',  label: 'Salão' },
          { group: 'mgmt',   label: 'Gestão' },
          { group: 'tenant', label: 'Tenant' },
        ].map(({ group, label }) => {
          const groupItems = navItems.filter(n => n.group === group);
          return (
            <React.Fragment key={group}>
              <div className="nav-section-label">{label}</div>
              {groupItems.map(n => (
                <button key={n.id}
                  className={'nav-item ' + (view === n.id ? 'active' : '')}
                  onClick={() => { setView(n.id); setNavOpen(false); if (n.id !== 'pizza') setPizzaProduct(null); }}>
                  <Icon name={n.icon} className="nav-ico" size={17} />
                  <span className="nav-label">{n.label}</span>
                  {n.count != null && <span className="nav-count">{n.count}</span>}
                </button>
              ))}
            </React.Fragment>
          );
        })}

        <div className="sidebar-footer">
          <div className="plan-card">
            <div className="plan-card-name">Plano Pro</div>
            <div className="plan-card-meta">2.341 / 5.000 pedidos · este mês</div>
            <div className="plan-card-bar"><div style={{ width: '47%' }} /></div>
          </div>
        </div>
      </aside>

      <main className="main">
        {(view !== 'cadastros') && (
        <div className="view-head" data-screen-label={view}>
          <div className="view-head-title-wrap">
            {view !== 'pdv' && <h1 className="view-title">{titles[view]?.title}</h1>}
            {view !== 'pdv' && titles[view]?.sub && (
              <span className="view-sub-inline">{titles[view].sub}</span>
            )}
            {view === 'pdv' && !state.customer && (
              <div className="view-head-cust-search">
                {window.CustomerSearch && (
                  <window.CustomerSearch
                    value={null}
                    onPick={(c) => dispatch({ type: 'customer', customer: c })}
                    onClear={() => {}} />
                )}
              </div>
            )}
            {view === 'pdv' && state.customer && (
              <div className="view-head-cust-mobile">
                {state.customer.points > 0 && (
                  <button className={'view-head-loy-pill ' + (loyaltyApplied.points || loyaltyApplied.coupon ? 'is-applied' : '')}
                    onClick={() => setLoyaltyOpen(true)}
                    title="Fidelidade"
                    aria-label="Abrir fidelidade">
                    <span className="view-head-loy-pill-ico">
                      <svg width="11" height="11" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 2l2.5 7.5h7.5l-6 4.5 2.5 7.5L12 17l-6.5 4.5L8 14l-6-4.5h7.5z"/>
                      </svg>
                    </span>
                    <span className="view-head-loy-pill-body">
                      {loyaltyApplied.points || loyaltyApplied.coupon ? (
                        <>
                          <span className="view-head-loy-pill-line1">−R$ {(
                            (loyaltyApplied.points ? state.customer.points * 0.05 : 0) +
                            (loyaltyApplied.coupon ? (state.customer.coupon?.value || 0) : 0)
                          ).toFixed(2).replace('.', ',')}</span>
                          <span className="view-head-loy-pill-line2">aplicado</span>
                        </>
                      ) : (
                        <>
                          <span className="view-head-loy-pill-line1">{state.customer.points} pts</span>
                          <span className="view-head-loy-pill-line2">R$ {(state.customer.points * 0.05).toFixed(2).replace('.', ',')}</span>
                        </>
                      )}
                    </span>
                  </button>
                )}
                <div className="view-head-cust-info">
                  <div className="view-head-cust-name">{state.customer.name}</div>
                  {state.customer.phone && state.customer.phone !== '—' && (
                    <div className="view-head-cust-meta">{state.customer.phone}</div>
                  )}
                </div>
                <button className="view-head-cust-x"
                  onClick={() => dispatch({ type: 'customer', customer: null })}
                  aria-label="Remover cliente">
                  <Icon name="x" size={12} />
                </button>
              </div>
            )}
          </div>
          <div className="view-actions">
            {view === 'pdv' && (
              <div className="view-actions-bar" role="toolbar" aria-label="Ações do PDV">
                <button className="view-action" onClick={() => setShowReceipt(true)} title="Comanda · imprimir">
                  <Icon name="print" size={15} /><span className="view-action-label">Comanda</span>
                </button>
                <button className="view-action" title="Movimentos do caixa">
                  <Icon name="cash" size={15} /><span className="view-action-label">Caixa</span>
                </button>
                <button className="view-action" title="Status da cozinha">
                  <Icon name="flame" size={15} /><span className="view-action-label">Cozinha</span>
                </button>
                <button className="view-action" title="Histórico de pedidos">
                  <Icon name="clock" size={15} /><span className="view-action-label">Histórico</span>
                </button>
                <button className="view-action overflow-trigger" title="Mais ações" aria-label="Mais ações">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="5" cy="12" r="1.2"/><circle cx="12" cy="12" r="1.2"/><circle cx="19" cy="12" r="1.2"/>
                  </svg>
                </button>
                <span className="view-actions-divider" />
                <button className="btn btn-primary view-action-primary" title="Novo pedido (Ctrl+N)">
                  <Icon name="plus" size={14} /><span>Novo pedido</span>
                </button>
              </div>
            )}
            {view === 'hub' && (
              <>
                <div className="chip"><span className="chip-dot" style={{ background: 'var(--good)' }} />Tempo real</div>
                <button className="btn"><Icon name="sliders" size={14} /> Filtros</button>
              </>
            )}
            {view === 'menu' && (
              <button className="btn"><Icon name="qrcode" size={14} /> Baixar QR</button>
            )}
          </div>
        </div>
        )}

        {view === 'pdv' && <PDVView state={state} dispatch={wrappedDispatch} onOpenPizza={openPizza} cartOpen={cartOpen} setCartOpen={setCartOpen} recent={recent} onOrderSent={onOrderSent} pausedOrders={pausedOrders} onPause={pauseCurrent} onResumePaused={resumePaused} onClosePaused={closePaused} currentTabLabel={currentTabLabel} cartCount={cartCount} cartTotal={cartTotal} onShowHelp={() => setShowHelp(true)} loyaltyApplied={loyaltyApplied} setLoyaltyApplied={setLoyaltyApplied} />}
        {view === 'pizza' && <PizzaFlow
          product={pizzaProduct || PRODUCTS.find(p => p.id === 'pz-g')}
          onCancel={() => { setPizzaProduct(null); setView('pdv'); }}
          onConfirm={confirmPizza} />}
        {view === 'hub' && <HubView />}
        {view === 'menu' && <DigitalMenu />}
        {view === 'waiter' && <WaiterView />}
        {view === 'cadastros' && <CadastrosView />}
        {view === 'admin' && <AdminView />}
      </main>

      {editMode && <TweaksPanel tweaks={tweaks} setTweaks={setTweaks} />}
      {toast && (
        typeof toast === 'object' && toast.kind === 'order' ? (
          <div className="toast toast-order">
            <Icon name="check" size={14} />
            {toast.msg}
            <span className="toast-order-id">#{toast.id}</span>
          </div>
        ) : (
          <div className="toast"><Icon name="check" size={14} />{typeof toast === 'string' ? toast : toast.msg}</div>
        )
      )}

      <HelpModal open={showHelp} onClose={() => setShowHelp(false)} />
      <LoyaltyModal
        open={loyaltyOpen}
        customer={state.customer}
        applied={loyaltyApplied}
        onApply={(next) => setLoyaltyApplied(next)}
        onClose={() => setLoyaltyOpen(false)} />
      <Tour open={showTour} onClose={() => {
        setShowTour(false);
        try { localStorage.setItem('pdv:tour-done', '1'); } catch {}
      }} />
      <ReceiptPreview
        open={showReceipt}
        order={{
          id: orderCounter - 1,
          items: state.cart,
          customer: state.customer,
          mode: state.mode,
          total: state.cart.reduce((s, i) => s + i.price * i.qty, 0) + (state.mode === 'delivery' ? 8 : 0),
          deliveryFee: state.mode === 'delivery' ? 8 : 0,
          discounts: 0,
        }}
        onClose={() => setShowReceipt(false)} />
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
