// ════════════════════════════════════════════════
// Forneria — Layout híbrido: horizontal + vertical
// Top nav define o módulo · sidebar lista os itens
// ════════════════════════════════════════════════

const { useState, useEffect, useRef, useMemo } = React;
const BRL = (n) => 'R$ ' + n.toFixed(2).replace('.', ',');
const fmt = (n) => n.toLocaleString('pt-BR');

// ─── Modules (horizontal nav) ──────────────────────

const MODULES = [
  { id: 'inicio',    label: 'Início',         icon: 'pos' },
  { id: 'operacao',  label: 'Operação',       icon: 'flame',  count: 28 },
  { id: 'gestao',    label: 'Gestão',         icon: 'sliders' },
  { id: 'cardapio',  label: 'Cardápio',       icon: 'menu' },
  { id: 'analise',   label: 'Análise',        icon: 'hub' },
  { id: 'config',    label: 'Configurações',  icon: 'admin' },
];

// ─── Sidebar (vertical) — items by module ──────────

const SIDEBAR = {
  inicio: [
    { label: 'Geral', items: [
      { id: 'visao',    label: 'Visão geral',    icon: 'pos' },
      { id: 'agenda',   label: 'Agenda do dia',  icon: 'clock' },
      { id: 'metas',    label: 'Metas',          icon: 'star',
        children: [
          { id: 'metas-vendas',  label: 'Vendas',          icon: 'cash',  count: '74%', countKind: 'good' },
          { id: 'metas-ticket',  label: 'Ticket médio',    icon: 'card',  count: '92%', countKind: 'good' },
          { id: 'metas-novos',   label: 'Novos clientes',  icon: 'sparkle', count: '48%', countKind: 'warn' },
        ] },
    ]},
  ],
  operacao: [
    { label: 'Pedidos', items: [
      { id: 'pdv',       label: 'PDV',             icon: 'pos' },
      { id: 'hub',       label: 'HUB de pedidos',  icon: 'hub',    count: 28,
        children: [
          { id: 'hub-aberto',  label: 'Em aberto',     count: 4,  countKind: '' },
          { id: 'hub-cozinha', label: 'Na cozinha',    count: 12, countKind: 'warn',
            children: [
              { id: 'coz-pizza',  label: 'Pizza',           count: 6, countKind: 'warn' },
              { id: 'coz-burger', label: 'Burger / grelha', count: 4 },
              { id: 'coz-fritura',label: 'Fritura',         count: 2 },
            ] },
          { id: 'hub-pronto',  label: 'Prontos',       count: 6,  countKind: 'good' },
          { id: 'hub-entrega', label: 'Em entrega',    count: 5,  countKind: 'accent',
            children: [
              { id: 'ent-propria',label: 'Frota própria', count: 2, countKind: 'accent' },
              { id: 'ent-ifood',  label: 'iFood',         count: 3 },
              { id: 'ent-uber',   label: 'Uber Direct',   count: 0 },
            ] },
        ] },
      { id: 'cozinha',   label: 'Cozinha',         icon: 'flame',  count: 12 },
      { id: 'entrega',   label: 'Entregas',        icon: 'truck',  count: 5 },
    ]},
    { label: 'Atendimento', items: [
      { id: 'garcom',    label: 'Garçom',          icon: 'waiter' },
      { id: 'mesas',     label: 'Mesas',           icon: 'table',  count: 8 },
      { id: 'cardapio-d',label: 'Cardápio digital',icon: 'qrcode' },
    ]},
  ],
  gestao: [
    { label: 'Pessoas', items: [
      { id: 'clientes', label: 'Clientes', icon: 'user', count: 2341,
        children: [
          { id: 'todos',     label: 'Todos',                            icon: 'user',    count: 2341 },
          { id: 'vip',       label: 'VIPs',                             icon: 'star',    count: 86,  countKind: 'accent' },
          { id: 'gold',      label: 'Gold',                             icon: 'star',    count: 214 },
          { id: 'recor',     label: 'Recorrentes',                      icon: 'cart',    count: 612 },
          { id: 'novo',      label: 'Novos',                            icon: 'sparkle', count: 124, countKind: 'good',
            children: [
              { id: 'novo-7d',  label: 'Últimos 7 dias',  icon: 'clock', count: 28, countKind: 'good' },
              { id: 'novo-30d', label: 'Últimos 30 dias', count: 124 },
              { id: 'novo-ifood',label: 'Vindos do iFood', count: 42 },
            ] },
          { id: 'inativo',   label: 'Inativos',                         icon: 'clock',   count: 482 },
          { id: 'risco',     label: 'Em risco de churn',                icon: 'flame',   count: 38,  countKind: 'danger' },
        ] },
      { id: 'fidelidade', label: 'Fidelidade',  icon: 'star',
        children: [
          { id: 'fid-pontos', label: 'Pontos & resgates', icon: 'cash' },
          { id: 'fid-niveis', label: 'Níveis (Bronze/Prata/Ouro)', icon: 'sliders' },
          { id: 'fid-regras', label: 'Regras de acúmulo', icon: 'menu' },
        ] },
      { id: 'equipe',     label: 'Equipe',      icon: 'waiter',
        children: [
          { id: 'eq-operadores', label: 'Operadores' },
          { id: 'eq-garcons',    label: 'Garçons' },
          { id: 'eq-cozinha',    label: 'Cozinha' },
          { id: 'eq-entrega',    label: 'Entregadores' },
        ] },
      { id: 'permissoes', label: 'Permissões',  icon: 'admin' },
    ]},
    { label: 'Operacional', items: [
      { id: 'estoque',     label: 'Estoque',       icon: 'store',   count: '!', countKind: 'danger',
        children: [
          { id: 'est-criticos',  label: 'Insumos críticos',  count: 4,   countKind: 'danger' },
          { id: 'est-todos',     label: 'Todos os insumos',  count: 184 },
          { id: 'est-perdas',    label: 'Perdas e quebras' },
        ] },
      { id: 'fornec',      label: 'Fornecedores',  icon: 'truck' },
      { id: 'movimentos',  label: 'Movimentações', icon: 'cart' },
      { id: 'fichas',      label: 'Fichas técnicas', icon: 'menu' },
    ]},
  ],
  cardapio: [
    { label: 'Catálogo', items: [
      { id: 'produtos',     label: 'Produtos',       icon: 'bag',     count: 47 },
      { id: 'sabores',      label: 'Sabores',        icon: 'pizza',   count: 24 },
      { id: 'categorias',   label: 'Categorias',     icon: 'sliders' },
      { id: 'modificadores',label: 'Modificadores',  icon: 'plus' },
    ]},
    { label: 'Apresentação', items: [
      { id: 'digital', label: 'Cardápio digital', icon: 'qrcode' },
      { id: 'promos',  label: 'Promoções',        icon: 'sparkle', count: 3, countKind: 'accent' },
      { id: 'combos',  label: 'Combos',           icon: 'star' },
    ]},
  ],
  analise: [
    { label: 'Vendas', items: [
      { id: 'receita',   label: 'Receita',         icon: 'cash' },
      { id: 'produtos-r',label: 'Por produto',     icon: 'bag' },
      { id: 'canais',    label: 'Por canal',       icon: 'hub' },
    ]},
    { label: 'Operacional', items: [
      { id: 'tempos',    label: 'Tempos e SLAs',   icon: 'clock' },
      { id: 'cmv',       label: 'CMV e margem',    icon: 'pizza' },
      { id: 'equipe-r',  label: 'Performance',     icon: 'waiter' },
    ]},
  ],
  config: [
    { label: 'Conta', items: [
      { id: 'tenant',  label: 'Empresa',     icon: 'store' },
      { id: 'plano',   label: 'Plano',       icon: 'star' },
      { id: 'cobranca',label: 'Cobrança',    icon: 'card' },
    ]},
    { label: 'Integrações', items: [
      { id: 'integr',  label: 'Apps conectados', icon: 'sparkle' },
      { id: 'api',     label: 'API e webhooks',  icon: 'admin' },
    ]},
  ],
};

// ─── Customers data ────────────────────────────────

const CUSTOMERS = [
  { id: 1, name: 'Marina Costa Almeida', phone: '+55 11 9·8765-4321', email: 'marina.costa@gmail.com',
    seg: 'vip', spent: 4280.50, orders: 38, avgTicket: 112.65, last: 'hoje, 12:42', channel: 'DIG' },
  { id: 2, name: 'João Pereira da Silva', phone: '+55 11 9·1234-5678', email: 'jp.silva@gmail.com',
    seg: 'gold', spent: 3142.00, orders: 31, avgTicket: 101.35, last: 'ontem', channel: 'PDV' },
  { id: 3, name: 'Carla Mendes Rocha', phone: '+55 11 9·9999-1212', email: 'carla.m@outlook.com',
    seg: 'vip', spent: 2876.30, orders: 27, avgTicket: 106.50, last: 'há 2 dias', channel: 'IFD' },
  { id: 4, name: 'Pedro Henrique Lima', phone: '+55 11 9·5555-7878', email: 'pedro.lima@hotmail.com',
    seg: 'recor', spent: 1842.75, orders: 19, avgTicket: 96.99, last: 'há 4 dias', channel: 'DIG' },
  { id: 5, name: 'Luciana Andrade Santos', phone: '+55 11 9·3434-2121', email: 'lu.andrade@gmail.com',
    seg: 'recor', spent: 1620.40, orders: 17, avgTicket: 95.31, last: 'há 6 dias', channel: 'IFD' },
  { id: 6, name: 'Roberto Tanaka', phone: '+55 11 9·8181-6767', email: 'r.tanaka@yahoo.com',
    seg: 'gold', spent: 1542.00, orders: 14, avgTicket: 110.14, last: 'há 1 sem', channel: 'PDV' },
  { id: 7, name: 'Beatriz Oliveira', phone: '+55 11 9·2727-3939', email: 'bia.oliveira@gmail.com',
    seg: 'risco', spent: 980.00, orders: 8, avgTicket: 122.50, last: 'há 38 dias', channel: 'DIG' },
  { id: 8, name: 'Lucas Andrade Vieira', phone: '+55 11 9·4444-5252', email: 'lucas.av@gmail.com',
    seg: 'novo', spent: 312.50, orders: 4, avgTicket: 78.12, last: 'há 3 dias', channel: 'DIG' },
  { id: 9, name: 'Família Bertolucci', phone: '+55 11 9·6262-1414', email: 'lf.bertolucci@uol.com.br',
    seg: 'vip', spent: 6480.00, orders: 42, avgTicket: 154.28, last: 'hoje, 19:20', channel: 'PDV' },
  { id: 10, name: 'Ana Carolina Souza', phone: '+55 11 9·1717-8989', email: 'ana.cs@gmail.com',
    seg: 'recor', spent: 1240.20, orders: 14, avgTicket: 88.58, last: 'há 8 dias', channel: 'IFD' },
  { id: 11, name: 'Diego Fernandes', phone: '+55 11 9·6464-1919', email: 'diego.f@gmail.com',
    seg: 'inativo', spent: 480.00, orders: 6, avgTicket: 80.00, last: 'há 92 dias', channel: 'PDV' },
  { id: 12, name: 'Mariana Pacheco', phone: '+55 11 9·8888-5454', email: 'm.pacheco@hotmail.com',
    seg: 'novo', spent: 175.00, orders: 2, avgTicket: 87.50, last: 'há 1 dia', channel: 'DIG' },
];

const SEGMENTS = [
  { id: 'todos',    label: 'Todos',         count: 2341 },
  { id: 'vip',      label: 'VIPs',          count: 86 },
  { id: 'gold',     label: 'Gold',          count: 214 },
  { id: 'recor',    label: 'Recorrentes',   count: 612 },
  { id: 'novo',     label: 'Novos',         count: 124 },
  { id: 'inativo',  label: 'Inativos',      count: 482 },
  { id: 'risco',    label: 'Em risco',      count: 38 },
];

const SEG_NAMES = {
  vip: 'VIP', gold: 'Gold', recor: 'Recorrente',
  novo: 'Novo', inativo: 'Inativo', risco: 'Em risco',
};

// ─── Nav Module with submenu dropdown ───────────────

function NavModule({ module: m, isActive, onActivate, openMenu, setOpenMenu, onPickSub }) {
  const isOpen = openMenu === m.id;
  const hostRef = useRef(null);
  const [anchor, setAnchor] = useState(null);
  const sections = SIDEBAR[m.id] || [];
  const hasSubmenu = sections.length > 0;

  useEffect(() => {
    if (!isOpen || !hostRef.current) return;
    const measure = () => {
      const r = hostRef.current.getBoundingClientRect();
      setAnchor({ left: r.left, bottom: r.bottom });
    };
    measure();
    window.addEventListener('resize', measure);
    window.addEventListener('scroll', measure, true);
    return () => {
      window.removeEventListener('resize', measure);
      window.removeEventListener('scroll', measure, true);
    };
  }, [isOpen]);

  // Hover peek when another menu is already open
  const onEnter = () => {
    if (hasSubmenu && openMenu && openMenu !== m.id) setOpenMenu(m.id);
  };

  return (
    <div className="menu-nav-host" ref={hostRef} onMouseEnter={onEnter}>
      <button
        className={'hv-nav-item ' + (isActive ? 'active ' : '') + (isOpen ? 'open' : '')}
        onClick={() => {
          if (hasSubmenu) {
            setOpenMenu(isOpen ? null : m.id);
          } else {
            onActivate();
            setOpenMenu(null);
          }
        }}>
        <Icon name={m.icon} size={15} />
        <span>{m.label}</span>
        {m.count != null && <span className="count">{m.count}</span>}
        {hasSubmenu && (
          <svg className="caret" width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        )}
      </button>

      {isOpen && anchor && (
        <ModuleDropdown sections={sections} anchor={anchor}
          activeSide={isActive ? null : null}
          onPick={(id) => { onPickSub(m.id, id); setOpenMenu(null); }} />
      )}
    </div>
  );
}

function ModuleDropdown({ sections, anchor, onPick, activeSide }) {
  // Use mega layout when there's more than one section
  const isMega = sections.length > 1;
  const minW = isMega ? 620 : 280;
  const maxLeft = Math.max(8, window.innerWidth - minW - 8);
  const style = { left: Math.min(anchor.left, maxLeft), top: anchor.bottom + 1 };

  const renderItem = (it) => (
    <button key={it.id} className={'menu-dd-item ' + (activeSide === it.id ? 'is-active' : '')}
            role="menuitem" onClick={() => onPick(it.id)}>
      <span className="menu-dd-ico"><Icon name={it.icon} size={14} /></span>
      <span className="menu-dd-body">
        <div className="menu-dd-name">{it.label}</div>
        {it.children && it.children.length > 0 && (
          <div className="menu-dd-desc">
            {it.children.slice(0, 3).map(c => c.label).join(' · ')}
            {it.children.length > 3 ? '…' : ''}
          </div>
        )}
      </span>
      {it.count != null && (
        <span className={'menu-dd-meta ' + (it.countKind === 'danger' ? 'danger' : '')}>
          {it.count}
        </span>
      )}
    </button>
  );

  if (isMega) {
    return (
      <div className="menu-dropdown mega" role="menu" style={style}>
        {sections.map((sec, si) => (
          <div key={si} className="menu-dd-col">
            <div className="menu-dd-group-label">{sec.label}</div>
            {sec.items.map(renderItem)}
          </div>
        ))}
      </div>
    );
  }
  // Single-section
  return (
    <div className="menu-dropdown" role="menu" style={style}>
      {sections[0]?.label && <div className="menu-dd-group-label">{sections[0].label}</div>}
      {sections[0]?.items?.map(renderItem)}
    </div>
  );
}

// ─── Side Nav Item — recursive (supports N-level nesting) ───

function SideNavItem({ item, activeId, onSelect, collapsed, inFlyout = false, depth = 0, iconMode = 'all' }) {
  const hasChildren = item.children && item.children.length > 0;
  // Auto-expand if a descendant is active
  const containsActive = useMemo(() => {
    const walk = (it) => {
      if (it.id === activeId) return true;
      return (it.children || []).some(walk);
    };
    return hasChildren && (item.children || []).some(walk);
  }, [item, activeId, hasChildren]);

  // Determine icon mode for THIS item's children list.
  //   'all'   — every child has its own icon → render icons normally
  //   'mixed' — some children have icons OR have their own children → show dot
  //             fallback for icon-less children to keep alignment
  //   'none'  — pure flat list of simple labels (no icons, no nested children)
  //             → drop the icon column entirely to reclaim horizontal space
  const childrenIconMode = useMemo(() => {
    if (!hasChildren) return 'all';
    const withIcon = item.children.filter(c => !!c.icon).length;
    const withNested = item.children.filter(c => c.children && c.children.length > 0).length;
    if (withIcon === 0 && withNested === 0) return 'none';
    if (withIcon === item.children.length) return 'all';
    return 'mixed';
  }, [item, hasChildren]);

  const [expanded, setExpanded] = useState(containsActive);
  useEffect(() => { if (containsActive) setExpanded(true); }, [containsActive]);

  // Flyout state — used both for collapsed-sidebar trigger AND nested-in-flyout
  const [flyoutOpen, setFlyoutOpen] = useState(false);
  const [flyoutAnchor, setFlyoutAnchor] = useState(null);
  const buttonRef = useRef(null);
  const closeTimer = useRef(null);

  // Flyout behavior applies when: top-level + collapsed sidebar, OR rendered inside another flyout.
  // Both cases: hover/click opens a popout to the right; no inline expansion.
  const useFlyout = (collapsed && depth === 0) || inFlyout;
  const isFlyoutTrigger = useFlyout && hasChildren;

  const openFlyout = () => {
    clearTimeout(closeTimer.current);
    if (!buttonRef.current) return;
    const r = buttonRef.current.getBoundingClientRect();
    setFlyoutAnchor({ left: r.right + 8, top: r.top });
    setFlyoutOpen(true);
  };
  const closeFlyoutSoon = () => {
    closeTimer.current = setTimeout(() => setFlyoutOpen(false), 220);
  };
  const cancelClose = () => clearTimeout(closeTimer.current);

  // Close any open flyout when leaving collapsed mode
  useEffect(() => { if (!collapsed && !inFlyout) setFlyoutOpen(false); }, [collapsed, inFlyout]);

  const isActive = activeId === item.id;
  const isInTrail = containsActive && !isActive;

  const classes = [
    'side-nav-item',
    hasChildren ? 'has-children' : '',
    isActive ? 'is-active' : '',
    isInTrail ? 'in-trail' : '',
    expanded && !isFlyoutTrigger ? 'is-expanded' : '',
    flyoutOpen ? 'is-flyout-open' : '',
  ].filter(Boolean).join(' ');

  return (
    <>
      <button ref={buttonRef} className={classes}
        data-label={item.label}
        data-badge={!collapsed || item.count == null ? undefined :
                    (typeof item.count === 'number' ? (item.count > 99 ? '99+' : item.count) : item.count)}
        onMouseEnter={isFlyoutTrigger ? openFlyout : undefined}
        onMouseLeave={isFlyoutTrigger ? closeFlyoutSoon : undefined}
        onClick={() => {
          if (isFlyoutTrigger) {
            if (flyoutOpen) setFlyoutOpen(false);
            else openFlyout();
            onSelect(item);
          } else if (hasChildren && !collapsed) {
            setExpanded(e => !e);
            onSelect(item);
          } else {
            onSelect(item);
          }
        }}>
        <span className="side-nav-ico">
          {item.icon ? <Icon name={item.icon} size={16} /> :
           iconMode === 'mixed' ? <span className="side-nav-ico-dot" /> :
           null}
        </span>
        <span className="side-nav-label-text">{item.label}</span>
        {item.count != null && !collapsed && (
          <span className={'side-nav-meta ' + (item.countKind || '')}>{item.count}</span>
        )}
        {hasChildren && !collapsed && (
          <svg className="side-nav-caret" width="12" height="12" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" strokeWidth="2.2"
               strokeLinecap="round" strokeLinejoin="round">
            <polyline points="9 6 15 12 9 18" />
          </svg>
        )}
      </button>

      {/* Inline children (only when NOT a flyout trigger) */}
      {hasChildren && expanded && !isFlyoutTrigger && !collapsed && (
        <div className={'side-nav-children ' + (childrenIconMode === 'none' ? 'no-icons' : '')}>
          {item.children.map(ch => (
            <SideNavItem key={ch.id} item={ch}
              activeId={activeId} onSelect={onSelect}
              collapsed={collapsed} inFlyout={inFlyout}
              depth={depth + 1}
              iconMode={childrenIconMode} />
          ))}
        </div>
      )}

      {/* Flyout panel — opens to the right of the trigger */}
      {isFlyoutTrigger && flyoutOpen && flyoutAnchor && (
        <div className="side-nav-flyout"
             style={{ left: flyoutAnchor.left, top: flyoutAnchor.top }}
             onMouseEnter={cancelClose}
             onMouseLeave={closeFlyoutSoon}>
          {!inFlyout && (
            <div className="side-nav-flyout-head">
              <span className="ico"><Icon name={item.icon} size={14} /></span>
              {item.label}
            </div>
          )}
          <div className={'side-nav-flyout-body ' + (childrenIconMode === 'none' ? 'no-icons' : '')}>
            {item.children.map(ch => (
              <SideNavItem key={ch.id} item={ch}
                activeId={activeId}
                onSelect={(picked) => { onSelect(picked); setFlyoutOpen(false); }}
                collapsed={false}
                inFlyout={true}
                depth={depth + 1}
                iconMode={childrenIconMode} />
            ))}
          </div>
        </div>
      )}
    </>
  );
}

function HybridDashboard() {
  const [module, setModule] = useState('gestao');
  const [side, setSide] = useState('vip');           // active sidebar leaf
  const [segment, setSegment] = useState('vip');
  const [selected, setSelected] = useState(new Set());
  const [query, setQuery] = useState('');
  const [sideOpen, setSideOpen] = useState(false);     // mobile drawer
  const [collapsed, setCollapsed] = useState(false);   // desktop collapse
  const [openMenu, setOpenMenu] = useState(null);      // horizontal nav submenu

  // The active id for the recursive sidebar component
  const activeId = side;

  // Close mobile drawer on resize back to desktop / Esc / click outside menu
  useEffect(() => {
    const onResize = () => { if (window.innerWidth > 760) setSideOpen(false); };
    const onKey = (e) => {
      if (e.key === 'Escape') {
        setSideOpen(false);
        setOpenMenu(null);
      }
    };
    const onDocClick = (e) => {
      // Close horizontal menu submenu on outside click
      if (!openMenu) return;
      if (e.target.closest('.menu-dropdown')) return;
      if (e.target.closest('.hv-nav-item')) return;
      setOpenMenu(null);
    };
    window.addEventListener('resize', onResize);
    window.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onDocClick);
    return () => {
      window.removeEventListener('resize', onResize);
      window.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onDocClick);
    };
  }, [openMenu]);

  const sidebarSections = SIDEBAR[module] || [];

  // Filter customers
  const filtered = useMemo(() => {
    let list = CUSTOMERS;
    if (segment !== 'todos') list = list.filter(c => c.seg === segment);
    if (query) {
      const q = query.toLowerCase();
      list = list.filter(c =>
        c.name.toLowerCase().includes(q) ||
        c.email.toLowerCase().includes(q) ||
        c.phone.includes(q)
      );
    }
    return list;
  }, [segment, query]);

  const toggleRow = (id) => {
    setSelected(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };
  const toggleAll = () => {
    if (selected.size === filtered.length) setSelected(new Set());
    else setSelected(new Set(filtered.map(c => c.id)));
  };
  const allChecked = selected.size > 0 && selected.size === filtered.length;

  // Stats (derived from current segment)
  const stats = useMemo(() => ({
    total: filtered.length,
    spent: filtered.reduce((s, c) => s + c.spent, 0),
    avgTicket: filtered.length ? filtered.reduce((s, c) => s + c.avgTicket, 0) / filtered.length : 0,
    orders: filtered.reduce((s, c) => s + c.orders, 0),
  }), [filtered]);

  // Reset selection when filters change
  useEffect(() => setSelected(new Set()), [segment, query]);

  return (
    <div className={'hv-app ' + (collapsed ? 'is-collapsed' : '')}>

      {/* ─── Top brand bar ─── */}
      <div className="hv-bar">
        <button className="hv-hamburger" onClick={() => setSideOpen(v => !v)} aria-label="Menu">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <path d="M3 6h18M3 12h18M3 18h18" />
          </svg>
        </button>
        <div className="brand">
          <div className="brand-mark">F</div>
          <span className="brand-name">Forneria</span>
          <span className="brand-slash">/</span>
          <span className="brand-tenant">Forno do Bairro</span>
        </div>

        <div className="hv-bar-search input-with-ico">
          <span className="ico"><Icon name="search" size={15} /></span>
          <input className="input" placeholder="Buscar cliente, pedido, produto…" />
        </div>

        <div className="hv-bar-actions">
          <button className="topbar-icon-btn" title="Notificações">
            <Icon name="bell" size={16} />
            <span className="has-dot" />
          </button>
          <button className="topbar-icon-btn" title="Atalhos">
            <Icon name="sliders" size={16} />
          </button>
          <div className="divider-v" />
          <div className="topbar-user" style={{ paddingLeft: 0, borderLeft: 0 }}>
            <div className="avatar">CR</div>
            <div>
              <div className="topbar-user-name">Carla Rocha</div>
              <div className="topbar-user-role">Gerente · PDV-02</div>
            </div>
          </div>
        </div>
      </div>

      {/* ─── Horizontal modules nav ─── */}
      <div className="hv-nav">
        {MODULES.map(m => (
          <NavModule key={m.id}
            module={m}
            isActive={module === m.id}
            openMenu={openMenu}
            setOpenMenu={setOpenMenu}
            onActivate={() => {
              setModule(m.id);
              const first = SIDEBAR[m.id]?.[0]?.items?.[0];
              if (first) setSide(first.id);
            }}
            onPickSub={(moduleId, itemId) => {
              setModule(moduleId);
              setSide(itemId);
            }} />
        ))}
        <div className="hv-nav-spacer" />
        <div className="hv-nav-trail">
          <span className="status-pill is-open">
            <span className="status-pill-dot" />
            Caixa aberto · 04:32
          </span>
        </div>
      </div>

      {/* Backdrop removed — click-outside handled by document listener
          (avoided stacking-context conflict where the sticky nav trapped the dropdown). */}

      {/* ─── Vertical sidebar (contextual) ─── */}
      {sideOpen && <div className="hv-side-backdrop" onClick={() => setSideOpen(false)} />}
      <aside className={'hv-side ' + (sideOpen ? 'is-open' : '')}>
        <div className="hv-side-head">
          <h2>{MODULES.find(m => m.id === module)?.label}</h2>
          <button className="hv-side-collapse"
                  onClick={() => setCollapsed(c => !c)}
                  title={collapsed ? 'Expandir menu' : 'Colapsar menu'}
                  aria-label={collapsed ? 'Expandir menu' : 'Colapsar menu'}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>
        </div>

        <div className="hv-side-body">
          {sidebarSections.map((sec, si) => (
            <div key={si} className="side-nav-section">
              {sec.label && <div className="side-nav-label">{sec.label}</div>}
              {sec.items.map(it => (
                <SideNavItem key={it.id} item={it}
                  activeId={activeId}
                  collapsed={collapsed}
                  onSelect={(picked) => {
                    setSide(picked.id);
                    setSideOpen(false);
                    // Map specific children to the segment filter
                    if (['todos','vip','gold','recor','novo','inativo','risco'].includes(picked.id)) {
                      setSegment(picked.id);
                    }
                  }} />
              ))}
            </div>
          ))}
        </div>

        <div className="hv-side-foot">
          <div className="hv-promo">
            <div className="hv-promo-title">
              IA do Forneria <span className="badge">BETA</span>
            </div>
            <div className="hv-promo-sub">
              Identifique clientes em risco de churn antes que parem de pedir.
            </div>
            <button className="hv-promo-cta">
              Experimentar
              <Icon name="chevron-right" size={12} />
            </button>
          </div>
        </div>
      </aside>

      {/* ─── Main content ─── */}
      <main className="hv-main">
        {/* Breadcrumbs */}
        <div className="hv-crumbs">
          <a>Forneria</a>
          <span className="sep">/</span>
          <a onClick={() => setModule('gestao')}>{MODULES.find(m => m.id === module)?.label}</a>
          <span className="sep">/</span>
          <span className="current">Clientes</span>
          {segment !== 'todos' && (
            <>
              <span className="sep">/</span>
              <span className="current">{SEGMENTS.find(s => s.id === segment)?.label}</span>
            </>
          )}
        </div>

        {/* Page head */}
        <div className="hv-page-head">
          <div>
            <h1 className="hv-page-title">
              <span className="icon-wrap"><Icon name="user" size={17} /></span>
              Clientes
            </h1>
            <p className="hv-page-sub">
              Base completa de clientes · sincronizada com PDV, cardápio digital e iFood
            </p>
          </div>
          <div className="hv-page-actions">
            <button className="btn btn-ghost">
              <Icon name="print" size={13} /> Importar
            </button>
            <button className="btn">
              <Icon name="qrcode" size={13} /> Exportar CSV
            </button>
            <button className="btn btn-primary">
              <Icon name="user-plus" size={13} /> Novo cliente
            </button>
          </div>
        </div>

        {/* Toolbar */}
        <div className="hv-toolbar">
          <div className="hv-toolbar-search input-with-ico">
            <span className="ico"><Icon name="search" size={14} /></span>
            <input className="input" placeholder="Buscar por nome, telefone, e-mail…"
                   value={query} onChange={e => setQuery(e.target.value)} />
          </div>
          <div className="hv-segments">
            {SEGMENTS.map(s => (
              <button key={s.id}
                className={'hv-segment ' + (segment === s.id ? 'active' : '')}
                onClick={() => setSegment(s.id)}>
                {s.label} <span className="count">{fmt(s.count)}</span>
              </button>
            ))}
          </div>
          <div className="hv-toolbar-spacer" />
          <button className="btn btn-ghost">
            <Icon name="sliders" size={13} /> Filtros
          </button>
        </div>

        {/* Stats row */}
        <div className="hv-stats">
          <div className="hv-stat">
            <div className="hv-stat-label">Clientes no segmento</div>
            <div className="hv-stat-value">{fmt(stats.total)}</div>
            <div className="hv-stat-delta up">▲ +12 este mês <span className="hv-stat-delta-vs">vs mês anterior</span></div>
          </div>
          <div className="hv-stat">
            <div className="hv-stat-label">Receita acumulada</div>
            <div className="hv-stat-value">{BRL(stats.spent)}</div>
            <div className="hv-stat-delta up">▲ +18,4% <span className="hv-stat-delta-vs">vs período anterior</span></div>
          </div>
          <div className="hv-stat">
            <div className="hv-stat-label">Ticket médio</div>
            <div className="hv-stat-value">{BRL(stats.avgTicket)}</div>
            <div className="hv-stat-delta up">▲ +8,2% <span className="hv-stat-delta-vs">vs período anterior</span></div>
          </div>
          <div className="hv-stat">
            <div className="hv-stat-label">Pedidos totais</div>
            <div className="hv-stat-value">{fmt(stats.orders)}</div>
            <div className="hv-stat-delta down">▼ −4,1% <span className="hv-stat-delta-vs">vs período anterior</span></div>
          </div>
        </div>

        {/* Table card */}
        <div className="hv-card">
          <div className="hv-card-head">
            <div>
              <h3>
                {SEGMENTS.find(s => s.id === segment)?.label}
                <span style={{ color: 'var(--fg-soft)', fontWeight: 500, marginLeft: 8 }}>
                  ({filtered.length})
                </span>
              </h3>
              <div className="sub">
                Ordenado por valor gasto · atualizado há 2 min
              </div>
            </div>
            <div className="hv-card-head-actions">
              <button className="btn btn-ghost" style={{ padding: '5px 10px', fontSize: 12 }}>
                <Icon name="sliders" size={12} /> Colunas
              </button>
              <button className="btn btn-ghost" style={{ padding: '5px 10px', fontSize: 12 }}>
                Ordenar <Icon name="chevron-right" size={12} />
              </button>
            </div>
          </div>

          {/* Selection action bar */}
          {selected.size > 0 && (
            <div className="hv-selection">
              <span className="count">{selected.size}</span> selecionado{selected.size > 1 ? 's' : ''}
              <button className="hv-sel-btn"><Icon name="phone" size={12} /> Enviar campanha</button>
              <button className="hv-sel-btn"><Icon name="star" size={12} /> Marcar como VIP</button>
              <button className="hv-sel-btn"><Icon name="bag" size={12} /> Adicionar pontos</button>
              <div className="hv-sel-spacer" />
              <span className="hv-sel-clear" onClick={() => setSelected(new Set())}>Limpar seleção</span>
            </div>
          )}

          <div className="hv-table-wrap">
            <table className="hv-table">
              <thead>
                <tr>
                  <th className="checkcell">
                    <div className={'hv-check ' + (allChecked ? 'checked' : '')} onClick={toggleAll}>
                      <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
                    </div>
                  </th>
                  <th>Cliente</th>
                  <th>Contato</th>
                  <th>Segmento</th>
                  <th className="right">Pedidos</th>
                  <th className="right">Ticket médio</th>
                  <th className="right">Total gasto</th>
                  <th>Último pedido</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(c => {
                  const initials = c.name.split(' ').slice(0, 2).map(s => s[0]).join('');
                  const isSel = selected.has(c.id);
                  return (
                    <tr key={c.id} className={isSel ? 'selected' : ''}
                        onClick={(e) => { if (e.target.closest('.hv-check')) return; toggleRow(c.id); }}>
                      <td className="checkcell">
                        <div className={'hv-check ' + (isSel ? 'checked' : '')}
                             onClick={(e) => { e.stopPropagation(); toggleRow(c.id); }}>
                          <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
                        </div>
                      </td>
                      <td>
                        <div className="hv-cust">
                          <div className={'hv-cust-av ' + (c.seg === 'vip' ? 'vip' : c.seg === 'gold' ? 'gold' : '')}>
                            {initials}
                          </div>
                          <div>
                            <div className="hv-cust-name">{c.name}</div>
                            <div className="hv-cust-meta">#{c.id.toString().padStart(4, '0')}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="hv-mono" style={{ fontSize: 12 }}>{c.phone}</div>
                        <div className="hv-cust-meta" style={{ fontFamily: 'var(--font-ui)', color: 'var(--fg-muted)' }}>{c.email}</div>
                      </td>
                      <td>
                        <span className={'seg ' + c.seg}>{SEG_NAMES[c.seg]}</span>
                      </td>
                      <td className="hv-num">{c.orders}</td>
                      <td className="hv-num">{BRL(c.avgTicket)}</td>
                      <td className="hv-num">{BRL(c.spent)}</td>
                      <td>
                        <div style={{ fontSize: 12 }}>{c.last}</div>
                        <span className="pill-channel compact">{c.channel}</span>
                      </td>
                      <td>
                        <button className="topbar-icon-btn" style={{ width: 28, height: 28 }} onClick={(e) => e.stopPropagation()}>
                          <Icon name="chevron-right" size={14} />
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan="9" style={{ padding: 40, textAlign: 'center', color: 'var(--fg-soft)' }}>
                      Nenhum cliente encontrado com esses filtros.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="hv-pagination">
            <div>
              Mostrando <b style={{ color: 'var(--fg)' }}>1 — {filtered.length}</b> de <b style={{ color: 'var(--fg)' }}>{SEGMENTS.find(s => s.id === segment)?.count || filtered.length}</b>
            </div>
            <div className="hv-pages">
              <button className="hv-page"><Icon name="chevron-left" size={13} /></button>
              <button className="hv-page active">1</button>
              <button className="hv-page">2</button>
              <button className="hv-page">3</button>
              <button className="hv-page dots">…</button>
              <button className="hv-page">8</button>
              <button className="hv-page"><Icon name="chevron-right" size={13} /></button>
            </div>
          </div>
        </div>

      </main>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<HybridDashboard />);
