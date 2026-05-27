// ════════════════════════════════════════════════
// Forneria — Dashboard "Visão Geral"
// Layout com menu horizontal seguindo o design system
// ════════════════════════════════════════════════

const { useState, useMemo, useEffect, useRef } = React;
const BRL = (n) => 'R$ ' + n.toFixed(2).replace('.', ',');
const fmt = (n) => n.toLocaleString('pt-BR');

// ─── Mock data ──────────────────────────────────────

const NAV = [
{ id: 'visao', label: 'Visão geral', icon: 'pos' },

{ id: 'pedidos', label: 'Pedidos', icon: 'hub', count: 28,
  submenu: {
    kind: 'list',
    items: [
    { id: 'aberto', label: 'Em aberto', desc: 'Aguardando confirmação', icon: 'cart', meta: '4', metaKind: '' },
    { id: 'cozinha', label: 'Na cozinha', desc: 'Em preparação agora', icon: 'flame', meta: '12', metaKind: 'warn',
      childrenLabel: 'Por estação',
      children: [
        { id: 'coz-pizza',  label: 'Pizza',           icon: 'pizza', meta: '6', metaKind: 'warn',
          childrenLabel: 'Por tamanho',
          children: [
            { id: 'pz-m',  label: 'Média (6 fatias)',   meta: '2', metaKind: '' },
            { id: 'pz-g',  label: 'Grande (8 fatias)',  meta: '3', metaKind: 'warn' },
            { id: 'pz-gg', label: 'Família (12 fatias)',meta: '1', metaKind: '' },
          ] },
        { id: 'coz-burger', label: 'Burger / Grelha', icon: 'flame', meta: '4', metaKind: '' },
        { id: 'coz-fritura',label: 'Fritura',         icon: 'bag',   meta: '2', metaKind: '' },
        { id: 'coz-sobr',   label: 'Sobremesas',      icon: 'star',  meta: '0', metaKind: '' },
      ] },
    { id: 'pronto', label: 'Prontos para retirada', desc: 'Esperando entregador / cliente', icon: 'bag', meta: '6', metaKind: 'good' },
    { id: 'entrega', label: 'Em entrega', desc: 'Saiu para entrega', icon: 'truck', meta: '5', metaKind: 'accent',
      childrenLabel: 'Por canal',
      children: [
        { id: 'ent-propria',label: 'Frota própria',       icon: 'truck', meta: '2', metaKind: 'accent',
          childrenLabel: 'Entregadores',
          children: [
            { id: 'mb-joao',  label: 'João Silva',      icon: 'waiter', meta: 'em rota', metaKind: 'accent' },
            { id: 'mb-pedro', label: 'Pedro Almeida',   icon: 'waiter', meta: 'em rota', metaKind: 'accent' },
            { id: 'mb-rafa',  label: 'Rafa Costa',      icon: 'waiter', meta: 'livre',  metaKind: 'good' },
          ] },
        { id: 'ent-ifood',  label: 'iFood Entrega',       icon: 'bag',   meta: '3', metaKind: '' },
        { id: 'ent-uber',   label: 'Uber Direct',         icon: 'truck', meta: '0', metaKind: '' },
        { id: 'ent-rastr',  label: 'Rastreamento ao vivo', desc: 'Mapa com motoboys', icon: 'qrcode' },
      ] },
    { id: 'concluidos', label: 'Concluídos hoje', desc: '142 finalizados · R$ 11.266,65', icon: 'check', meta: '142', metaKind: '',
      childrenLabel: 'Período',
      children: [
        { id: 'conc-hoje',   label: 'Hoje' },
        { id: 'conc-7d',     label: 'Últimos 7 dias' },
        { id: 'conc-30d',    label: 'Últimos 30 dias' },
        { id: 'conc-custom', label: 'Personalizado…' },
      ] },
    { id: 'cancelados', label: 'Cancelados', desc: '3 cancelados nas últimas 24h', icon: 'x', meta: '3', metaKind: 'danger' }],

    foot: { label: 'Ver todos os pedidos', kbd: 'G P' }
  }
},

{ id: 'cardapio', label: 'Cardápio', icon: 'menu',
  submenu: {
    kind: 'mega',
    columns: [
    { label: 'Catálogo', items: [
      { id: 'produtos', label: 'Produtos', desc: '47 ativos · 4 categorias', icon: 'bag' },
      { id: 'sabores', label: 'Sabores de pizza', desc: '24 sabores · borda recheada', icon: 'pizza' },
      { id: 'categorias', label: 'Categorias', desc: 'Pizzas, burgers, bebidas…', icon: 'sliders' },
      { id: 'modificadores', label: 'Modificadores', desc: 'Adicionais, observações e regras', icon: 'plus' }]
    },
    { label: 'Apresentação', items: [
      { id: 'card-digital', label: 'Cardápio digital', desc: 'Preview self-service via QR', icon: 'qrcode' },
      { id: 'promocoes', label: 'Promoções', desc: '3 ativas · 28% de adesão', icon: 'sparkle', meta: 'NOVO', metaKind: 'accent' },
      { id: 'combos', label: 'Combos', desc: 'Pacotes e cross-sell', icon: 'star' },
      { id: 'horarios', label: 'Horários e dispon.', desc: 'Janelas de venda por canal', icon: 'clock' }]
    }],

    promo: {
      title: 'IA do Cardápio',
      sub: 'Identifique pratos com margem baixa e otimize automaticamente',
      cta: 'Experimentar'
    }
  }
},

{ id: 'clientes', label: 'Clientes', icon: 'user',
  submenu: {
    kind: 'list',
    items: [
    { id: 'lista', label: 'Lista completa', desc: '2.341 cadastrados', icon: 'user',
      childrenLabel: 'Filtros rápidos',
      children: [
        { id: 'lista-todos', label: 'Todos',         icon: 'user',    meta: '2341', metaKind: '' },
        { id: 'lista-vip',   label: 'VIPs',          icon: 'star',    meta: '86',   metaKind: 'accent' },
        { id: 'lista-recor', label: 'Recorrentes',   icon: 'cart',    meta: '612',  metaKind: '' },
        { id: 'lista-novos', label: 'Novos do mês',  icon: 'sparkle', meta: '124',  metaKind: 'good' },
        { id: 'lista-risco', label: 'Em risco',      icon: 'flame',   meta: '38',   metaKind: 'danger' },
      ] },
    { id: 'fidelidade', label: 'Programa fidelidade', desc: '486 ativos · 12.4k pontos hoje', icon: 'star', meta: 'PRO', metaKind: 'accent' },
    { id: 'aniver', label: 'Aniversariantes', desc: '8 esta semana', icon: 'sparkle', meta: '8', metaKind: 'good',
      childrenLabel: 'Período',
      children: [
        { id: 'aniv-hoje', label: 'Hoje',         meta: '1',  metaKind: 'accent' },
        { id: 'aniv-sem',  label: 'Esta semana',  meta: '8',  metaKind: 'good' },
        { id: 'aniv-mes',  label: 'Este mês',     meta: '34', metaKind: '' },
      ] },
    { id: 'segmentos', label: 'Segmentos', desc: 'VIP, recorrentes, inativos…', icon: 'sliders' },
    { id: 'campanhas', label: 'Campanhas', desc: 'WhatsApp e push', icon: 'bell',
      childrenLabel: 'Por canal',
      children: [
        { id: 'camp-wa',    label: 'WhatsApp',   icon: 'phone',  meta: '3', metaKind: 'good' },
        { id: 'camp-push',  label: 'Push (App)', icon: 'bell',   meta: '1', metaKind: '' },
        { id: 'camp-sms',   label: 'SMS',        icon: 'phone',  meta: '0', metaKind: '' },
        { id: 'camp-email', label: 'E-mail mkt', icon: 'menu',   meta: '2', metaKind: '' },
      ] }],

    foot: { label: 'Novo cliente', kbd: 'N' }
  }
},

{ id: 'estoque', label: 'Estoque', icon: 'store',
  submenu: {
    kind: 'list',
    items: [
    { id: 'insumos', label: 'Insumos', desc: '184 itens · 4 críticos', icon: 'bag', meta: '!', metaKind: 'danger' },
    { id: 'movimentos', label: 'Movimentações', desc: 'Entradas, saídas e ajustes', icon: 'truck' },
    { id: 'inventario', label: 'Inventário', desc: 'Última contagem · 18/05', icon: 'check' },
    { id: 'fornec', label: 'Fornecedores', desc: '12 cadastrados', icon: 'store' },
    { id: 'fichas', label: 'Fichas técnicas', desc: 'Custo e CMV por produto', icon: 'menu' }]

  }
},

{ id: 'relatorios', label: 'Relatórios', icon: 'sliders',
  submenu: {
    kind: 'list',
    groupLabel: 'Frequentes',
    items: [
    { id: 'vendas', label: 'Vendas', desc: 'Por período, canal e produto', icon: 'card' },
    { id: 'financeiro', label: 'Financeiro', desc: 'Fluxo de caixa e DRE', icon: 'cash' },
    { id: 'operac', label: 'Operacional', desc: 'Tempos, ocupação e produtividade', icon: 'clock' },
    { id: 'cmv', label: 'CMV e margem', desc: 'Análise por produto', icon: 'pizza' },
    { id: 'equipe', label: 'Equipe', desc: 'Performance por operador', icon: 'waiter' }],

    foot: { label: 'Criar relatório personalizado', kbd: '⌘ R' }
  }
},

{ id: 'integracoes', label: 'Integrações', icon: 'sparkle',
  submenu: {
    kind: 'list',
    items: [
    { id: 'ifood', label: 'iFood', desc: 'Conectado · 18 pedidos hoje', icon: 'bag', meta: 'OK', metaKind: 'good' },
    { id: 'whats', label: 'WhatsApp Business', desc: 'Conectado · +55 11 9…', icon: 'phone', meta: 'OK', metaKind: 'good' },
    { id: 'nfe', label: 'NF-e / SAT', desc: 'Emissão automática', icon: 'print', meta: 'OK', metaKind: 'good' },
    { id: 'maquina', label: 'Maquininhas', desc: 'Stone, Cielo, Rede', icon: 'card' },
    { id: 'api', label: 'API e webhooks', desc: 'Para desenvolvedores', icon: 'admin' },
    { id: 'market', label: 'Marketplace de apps', desc: '42 apps disponíveis', icon: 'store', meta: 'NOVO', metaKind: 'accent' }]

  }
}];


const PERIODS = ['Hoje', '7 dias', '30 dias', 'Mês', 'Personalizado'];

// 24 pontos (1 a cada hora) — variação realista para um restaurante
const SALES_BY_HOUR = [
120, 80, 60, 40, 30, 50, 110, 240, 380, 420, 510, 680,
920, 1050, 880, 640, 540, 720, 1180, 1640, 1820, 1490, 980, 420];


const CHANNELS = [
{ id: 'pdv', name: 'PDV Balcão', pct: 38, total: 4280.50, color: '#1a1612' },
{ id: 'digital', name: 'Cardápio digital', pct: 27, total: 3042.00, color: 'var(--accent)' },
{ id: 'ifood', name: 'iFood', pct: 18, total: 2028.40, color: '#ea1d2c' },
{ id: 'mesa', name: 'Mesa / Salão', pct: 12, total: 1352.30, color: '#16a34a' },
{ id: 'garcom', name: 'Garçom', pct: 5, total: 563.45, color: '#7c3aed' }];


const TOP_PRODUCTS = [
{ name: 'Pizza Calabresa G', qty: 142, value: 8236.00, share: 100 },
{ name: 'Cheeseburger Duplo', qty: 118, value: 3304.00, share: 80 },
{ name: 'Pizza Quatro Queijos G', qty: 96, value: 6528.00, share: 64 },
{ name: 'Coca-Cola 2L', qty: 87, value: 1218.00, share: 53 },
{ name: 'Pizza Margherita G', qty: 74, value: 4588.00, share: 44 },
{ name: 'Combo Smash + Fritas', qty: 62, value: 2356.00, share: 36 },
{ name: 'Brownie c/ Sorvete', qty: 58, value: 1044.00, share: 32 }];


const RECENT_ORDERS = [
{ id: '#4287', cust: 'Marina Costa', items: '1× Pizza Calabresa G, 1× Coca 2L', total: 87.80, channel: 'digital', status: 'cozinha', time: 'há 2 min' },
{ id: '#4286', cust: 'João Pereira', items: '2× Cheeseburger Duplo, 1× Brownie', total: 78.00, channel: 'pdv', status: 'pronto', time: 'há 4 min' },
{ id: '#4285', cust: 'Mesa 12', items: '1× Família 4Q ½ Margherita, 2× Cerveja LN', total: 96.00, channel: 'mesa', status: 'aberto', time: 'há 6 min' },
{ id: '#4284', cust: 'Pedro Lima', items: '1× Combo Smash + Fritas, 1× Guaraná 2L', total: 52.00, channel: 'ifood', status: 'entrega', time: 'há 12 min' },
{ id: '#4283', cust: 'Carla Mendes', items: '1× Veggie Burger, 1× Suco Laranja', total: 35.00, channel: 'digital', status: 'entrega', time: 'há 18 min' },
{ id: '#4282', cust: 'Mesa 03', items: '1× Pizza Pepperoni G, 1× Petit Gâteau', total: 86.00, channel: 'garcom', status: 'cozinha', time: 'há 22 min' },
{ id: '#4281', cust: 'Lucas Andrade', items: '1× Frango c/ Catupiry G, 1× Cerveja LN', total: 74.00, channel: 'pdv', status: 'pronto', time: 'há 24 min' }];


const EVENTS = [
{ kind: 'alert', text: <><b>Estoque crítico</b> — Muçarela 4kg restantes (cobertura 1,8h)</>, time: '11:42', icon: 'flame' },
{ kind: 'warn', text: <><b>iFood</b> — 2 pedidos com tempo de preparo acima da meta</>, time: '11:30', icon: 'clock' },
{ kind: 'ok', text: <><b>Promoção "Calabresa + Coca"</b> ativada · 28% de adesão hoje</>, time: '10:15', icon: 'sparkle' },
{ kind: 'acc', text: <><b>Caixa PDV-02</b> aberto por Carla Rocha · R$ 200,00 fundo</>, time: '08:12', icon: 'cash' }];


// ─── Components ──────────────────────────────────────

function Sparkline({ data, color = 'var(--accent)', width = 100, height = 32 }) {
  const max = Math.max(...data);
  const min = Math.min(...data);
  const range = max - min || 1;
  const step = width / (data.length - 1);
  const pts = data.map((v, i) => [i * step, height - (v - min) / range * height]);
  const path = pts.map((p, i) => (i ? 'L' : 'M') + p[0].toFixed(1) + ',' + p[1].toFixed(1)).join(' ');
  const areaPath = path + ` L${width},${height} L0,${height} Z`;
  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
      <path d={areaPath} fill={color} opacity="0.12" />
      <path d={path} fill="none" stroke={color} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>);

}

function Kpi({ label, icon, value, unit, delta, deltaDir = 'up', vs = 'vs ontem', spark, color }) {
  return (
    <div className="h-kpi">
      <div className="h-kpi-label">
        <span className="ico"><Icon name={icon} size={13} /></span>
        {label}
      </div>
      <div className="h-kpi-value">
        {unit && <span className="unit">{unit}</span>}
        {value}
      </div>
      <div>
        <span className={'h-kpi-delta ' + deltaDir}>
          {deltaDir === 'up' ? '▲' : '▼'} {delta}
        </span>
        <span className="h-kpi-delta-vs" style={{ marginLeft: 6, fontSize: 11.5 }}>{vs}</span>
      </div>
      {spark && <div className="h-kpi-spark"><Sparkline data={spark} color={color || 'var(--accent)'} /></div>}
    </div>);

}

function SalesChart({ data }) {
  const W = 720,H = 220,padL = 36,padR = 8,padT = 12,padB = 28;
  const innerW = W - padL - padR;
  const innerH = H - padT - padB;
  const max = Math.max(...data);
  const step = innerW / (data.length - 1);

  const pts = data.map((v, i) => [padL + i * step, padT + innerH - v / max * innerH]);
  const linePath = pts.map((p, i) => (i ? 'L' : 'M') + p[0].toFixed(1) + ',' + p[1].toFixed(1)).join(' ');
  const areaPath = linePath + ` L${padL + innerW},${padT + innerH} L${padL},${padT + innerH} Z`;

  // Grid lines at 4 horizontal levels
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((t) => ({
    y: padT + innerH * (1 - t),
    label: Math.round(max * t)
  }));

  // X labels every 4 hours
  const xLabels = [0, 4, 8, 12, 16, 20].map((h) => ({
    x: padL + h * step,
    label: String(h).padStart(2, '0') + 'h'
  }));

  const [hover, setHover] = useState(null);

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" height="100%"
    onMouseLeave={() => setHover(null)}
    onMouseMove={(e) => {
      const rect = e.currentTarget.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width * W;
      const i = Math.max(0, Math.min(data.length - 1, Math.round((x - padL) / step)));
      setHover(i);
    }}>
      {/* Grid */}
      <g className="h-chart-grid">
        {yTicks.map((t, i) =>
        <line key={i} x1={padL} x2={W - padR} y1={t.y} y2={t.y} />
        )}
      </g>
      {/* Y axis labels */}
      <g className="h-chart-axis">
        {yTicks.map((t, i) =>
        <text key={i} x={padL - 6} y={t.y + 3} textAnchor="end">
            {t.label >= 1000 ? (t.label / 1000).toFixed(1) + 'k' : t.label}
          </text>
        )}
        {xLabels.map((l, i) =>
        <text key={i} x={l.x} y={H - 10} textAnchor="middle">{l.label}</text>
        )}
      </g>
      {/* Area + Line */}
      <path d={areaPath} className="h-chart-area" />
      <path d={linePath} className="h-chart-line" />
      {/* Hover */}
      {hover != null &&
      <g>
          <line x1={pts[hover][0]} x2={pts[hover][0]} y1={padT} y2={padT + innerH}
        stroke="var(--line-strong)" strokeDasharray="2 3" />
          <circle cx={pts[hover][0]} cy={pts[hover][1]} r="5" className="h-chart-dot" />
          {/* Tooltip */}
          <g transform={`translate(${Math.min(pts[hover][0] + 10, W - 100)}, ${Math.max(pts[hover][1] - 36, padT)})`}>
            <rect width="92" height="32" rx="6" className="h-chart-tooltip" />
            <text x="8" y="14" fontSize="10" fontFamily="var(--font-mono)" fill="var(--bg)" opacity="0.7">
              {String(hover).padStart(2, '0')}:00 — {String(hover + 1).padStart(2, '0')}:00
            </text>
            <text x="8" y="26" fontSize="11" fontWeight="700" fontFamily="var(--font-mono)" fill="var(--bg)">
              R$ {fmt(data[hover])}
            </text>
          </g>
        </g>
      }
    </svg>);

}

// ─── User menu dropdown ─────────────────────────────

function UserMenu({ user, theme, onTheme, onAction }) {
  const triggerRef = useRef(null);
  const [open, setOpen] = useState(false);
  const [anchor, setAnchor] = useState(null);

  useEffect(() => {
    if (!open || !triggerRef.current) return;
    const measure = () => {
      const r = triggerRef.current.getBoundingClientRect();
      setAnchor({ right: window.innerWidth - r.right, top: r.bottom });
    };
    measure();
    window.addEventListener('resize', measure);
    window.addEventListener('scroll', measure, true);
    const onKey = (e) => { if (e.key === 'Escape') setOpen(false); };
    window.addEventListener('keydown', onKey);
    return () => {
      window.removeEventListener('resize', measure);
      window.removeEventListener('scroll', measure, true);
      window.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const pick = (id) => {
    setOpen(false);
    onAction && onAction(id);
  };

  return (
    <>
      <div ref={triggerRef}
        className={'h-user-trigger ' + (open ? 'is-open' : '')}
        onClick={() => setOpen(o => !o)}
        role="button" tabIndex={0}>
        <div className="avatar">{user.initials}</div>
        <div className="h-user-trigger-text">
          <div className="topbar-user-name">{user.name}</div>
          <div className="topbar-user-role">{user.role}</div>
        </div>
        <svg className="caret" width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </div>

      {open && anchor && (
        <>
          <div className="menu-dd-backdrop" onClick={() => setOpen(false)} />
          <div className="h-user-menu" role="menu"
               style={{ right: Math.max(8, anchor.right), top: anchor.top + 6 }}>

            {/* User card header */}
            <div className="h-user-card">
              <div className="avatar">{user.initials}</div>
              <div className="h-user-card-info">
                <div className="h-user-card-name">{user.name}</div>
                <div className="h-user-card-mail">{user.email}</div>
                <span className="h-user-card-meta">
                  <span className="dot" />
                  {user.role}
                </span>
              </div>
            </div>

            {/* Primary actions */}
            <div className="h-user-section">
              <button className="h-user-item" onClick={() => pick('perfil')}>
                <span className="ico"><Icon name="user" size={15} /></span>
                <span>Meu perfil</span>
                <span className="h-user-item-trail" />
              </button>
              <button className="h-user-item" onClick={() => pick('caixa')}>
                <span className="ico"><Icon name="cash" size={15} /></span>
                <span>Meu caixa <span style={{ color: 'var(--fg-soft)', fontFamily: 'var(--font-mono)', fontSize: 11, marginLeft: 4 }}>· 04:32</span></span>
                <span className="h-user-item-trail">
                  <span style={{ color: 'var(--good)' }}>R$ 4.280</span>
                </span>
              </button>
              <button className="h-user-item" onClick={() => pick('turno')}>
                <span className="ico"><Icon name="clock" size={15} /></span>
                <span>Fechar turno</span>
                <span className="h-user-kbd">⇧ X</span>
              </button>
            </div>

            {/* Preferences */}
            <div className="h-user-section">
              <div className="h-user-label">Preferências</div>

              <div className="h-user-theme">
                <span className="ico" style={{ color: 'var(--fg-muted)' }}>
                  <Icon name={theme === 'dark' ? 'moon' : 'sun'} size={15} />
                </span>
                <span>Aparência</span>
                <div className="h-user-theme-seg">
                  <button className={theme === 'light' ? 'active' : ''} title="Claro"
                          onClick={() => onTheme('light')}>
                    <Icon name="sun" size={13} />
                  </button>
                  <button className={theme === 'dark' ? 'active' : ''} title="Escuro"
                          onClick={() => onTheme('dark')}>
                    <Icon name="moon" size={13} />
                  </button>
                </div>
              </div>

              <button className="h-user-item" onClick={() => pick('notif')}>
                <span className="ico"><Icon name="bell" size={15} /></span>
                <span>Notificações</span>
                <span className="h-user-item-trail">3 novas</span>
              </button>
              <button className="h-user-item" onClick={() => pick('atalhos')}>
                <span className="ico"><Icon name="sliders" size={15} /></span>
                <span>Atalhos do teclado</span>
                <span className="h-user-kbd">⌘ /</span>
              </button>
            </div>

            {/* Admin / Support */}
            <div className="h-user-section">
              <button className="h-user-item" onClick={() => pick('config')}>
                <span className="ico"><Icon name="admin" size={15} /></span>
                <span>Configurações</span>
              </button>
              <button className="h-user-item" onClick={() => pick('ajuda')}>
                <span className="ico"><Icon name="qrcode" size={15} /></span>
                <span>Ajuda &amp; Suporte</span>
                <span className="h-user-item-trail" style={{ color: 'var(--accent)' }}>
                  Chat 24/7
                </span>
              </button>
            </div>

            {/* Tenant switcher */}
            <div className="h-user-tenant">
              <div className="h-user-tenant-mark">FB</div>
              <div className="h-user-tenant-info">
                <div className="h-user-tenant-name">Forno do Bairro</div>
                <div className="h-user-tenant-meta">Vila Madalena · 3 unidades</div>
              </div>
              <button className="h-user-tenant-switch" onClick={() => pick('trocar-tenant')}>
                Trocar
              </button>
            </div>

            {/* Logout */}
            <div className="h-user-section">
              <button className="h-user-item is-danger" onClick={() => pick('sair')}>
                <span className="ico"><Icon name="x" size={15} /></span>
                <span>Sair</span>
                <span className="h-user-kbd">⇧ ⌘ Q</span>
              </button>
            </div>
          </div>
        </>
      )}
    </>
  );
}

// ─── Nav Item with dropdown ─────────────────────────

function NavWithSubmenu({ item, active, openMenu, setOpenMenu, onSelect, onSubSelect }) {
  const isActive = active === item.id;
  const isOpen = openMenu === item.id;
  const hostRef = useRef(null);
  const [anchorRect, setAnchorRect] = useState(null);

  // Measure anchor when open (for fixed-positioned dropdown that can escape
  // any scrollable / clipped ancestor on medium screens).
  useEffect(() => {
    if (!isOpen || !hostRef.current) return;
    const measure = () => {
      const r = hostRef.current.getBoundingClientRect();
      setAnchorRect({ left: r.left, bottom: r.bottom });
    };
    measure();
    window.addEventListener('resize', measure);
    window.addEventListener('scroll', measure, true);
    return () => {
      window.removeEventListener('resize', measure);
      window.removeEventListener('scroll', measure, true);
    };
  }, [isOpen]);

  // Hover to peek on items that have submenus, but click stays sticky
  const handleEnter = () => {
    if (item.submenu && openMenu && openMenu !== item.id) {
      setOpenMenu(item.id);
    }
  };

  return (
    <div className="h-nav-host" ref={hostRef} onMouseEnter={handleEnter}>
      <button
        className={'h-nav-item ' + (isActive ? 'active ' : '') + (isOpen ? 'open' : '')}
        onClick={() => {
          if (item.submenu) {
            setOpenMenu(isOpen ? null : item.id);
          } else {
            onSelect(item.id);
            setOpenMenu(null);
          }
        }}>
        <Icon name={item.icon} size={15} />
        <span>{item.label}</span>
        {item.count != null && <span className="count">{item.count}</span>}
        {item.submenu &&
        <svg className="caret" width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        }
      </button>

      {isOpen && item.submenu && anchorRect &&
      <Submenu submenu={item.submenu} parentId={item.id} anchorRect={anchorRect}
      onPick={(subId) => {onSubSelect(item.id, subId);setOpenMenu(null);}} />
      }
    </div>);

}

function MenuItem({ item, onPick, depth = 0 }) {
  const ref = useRef(null);
  const [open, setOpen] = useState(false);
  const [anchor, setAnchor] = useState(null);
  const closeTimer = useRef(null);
  const hasChildren = item.children && item.children.length > 0;

  const measure = () => {
    if (!ref.current) return;
    const r = ref.current.getBoundingClientRect();
    setAnchor({ right: r.right, top: r.top, bottom: r.bottom });
  };

  const handleEnter = () => {
    if (!hasChildren) return;
    clearTimeout(closeTimer.current);
    measure();
    setOpen(true);
  };
  const handleLeave = () => {
    if (!hasChildren) return;
    // Generous delay so users can move into the nested submenu
    closeTimer.current = setTimeout(() => setOpen(false), 300);
  };
  const cancelClose = () => { clearTimeout(closeTimer.current); };

  return (
    <>
      <button ref={ref}
        className={'menu-dd-item ' + (hasChildren ? 'has-children ' : '') + (open ? 'is-expanded' : '')}
        role="menuitem"
        onMouseEnter={handleEnter}
        onMouseLeave={handleLeave}
        onClick={(e) => {
          e.stopPropagation();
          if (hasChildren) {
            measure();
            setOpen(o => !o);
          } else {
            onPick(item.id);
          }
        }}>
        <span className="menu-dd-ico"><Icon name={item.icon} size={14} /></span>
        <span className="menu-dd-body">
          <div className="menu-dd-name">{item.label}</div>
          {item.desc && <div className="menu-dd-desc">{item.desc}</div>}
        </span>
        {item.meta && <span className={'menu-dd-meta ' + (item.metaKind || '')}>{item.meta}</span>}
        {hasChildren && (
          <svg className="menu-dd-caret" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="9 6 15 12 9 18" />
          </svg>
        )}
      </button>

      {open && anchor && hasChildren && (
        <NestedDropdown anchor={anchor} items={item.children}
                        groupLabel={item.childrenLabel}
                        onPick={onPick}
                        onMouseEnter={cancelClose}
                        onMouseLeave={handleLeave}
                        depth={depth + 1} />
      )}
    </>
  );
}

function NestedDropdown({ anchor, items, groupLabel, onPick, onMouseEnter, onMouseLeave, depth }) {
  const minW = 260;
  const gap = 6;
  const onMobile = window.innerWidth <= 720;
  let style;
  if (onMobile) {
    // Anchor below the parent item, full-width
    style = { left: 8, right: 8, top: anchor.bottom + 4, minWidth: 'auto' };
  } else {
    // Open to the right of the parent; flip to left if doesn't fit
    const wouldOverflow = anchor.right + gap + minW > window.innerWidth - 8;
    if (wouldOverflow) {
      style = { left: 'auto', right: window.innerWidth - anchor.right + minW + gap, top: anchor.top - 6 };
    } else {
      style = { left: anchor.right + gap, top: anchor.top - 6 };
    }
  }
  return (
    <div className="menu-dropdown is-nested" role="menu" style={style}
         onMouseEnter={onMouseEnter} onMouseLeave={onMouseLeave}>
      {groupLabel && <div className="menu-dd-group-label">{groupLabel}</div>}
      {items.map(it => <MenuItem key={it.id} item={it} onPick={onPick} depth={depth} />)}
    </div>
  );
}

function Submenu({ submenu, parentId, anchorRect, onPick }) {
  // Clamp the dropdown inside the viewport.
  const isMega = submenu.kind === 'mega';
  const minW = isMega ? 620 : 280;
  const maxLeft = Math.max(8, window.innerWidth - minW - 8);
  const left = Math.min(anchorRect.left, maxLeft);
  const ddStyle = { left, top: anchorRect.bottom + 1 };

  if (isMega) {
    return (
      <div className="menu-dropdown mega" role="menu" style={ddStyle}>
        {submenu.columns.map((col, ci) =>
        <div key={ci} className="menu-dd-col">
            <div className="menu-dd-group-label">{col.label}</div>
            {col.items.map(it => <MenuItem key={it.id} item={it} onPick={onPick} />)}
          </div>
        )}
        {submenu.promo &&
        <div className="menu-dd-promo">
            <div className="menu-dd-promo-ico"><Icon name="sparkle" size={18} /></div>
            <div>
              <div className="menu-dd-promo-title">{submenu.promo.title}</div>
              <div className="menu-dd-promo-sub">{submenu.promo.sub}</div>
            </div>
            <button className="btn btn-primary menu-dd-promo-cta" style={{ padding: '7px 14px', fontSize: 12 }}>
              {submenu.promo.cta}
            </button>
          </div>
        }
      </div>);

  }

  // Default: list submenu
  return (
    <div className="menu-dropdown" role="menu" style={ddStyle}>
      {submenu.groupLabel && <div className="menu-dd-group-label">{submenu.groupLabel}</div>}
      {submenu.items.map(it => <MenuItem key={it.id} item={it} onPick={onPick} />)}
      {submenu.foot &&
      <div className="menu-dd-foot">
          <button className="linklike" onClick={() => onPick('__foot')}>
            {submenu.foot.label} →
          </button>
          {submenu.foot.kbd && <span className="menu-dd-kbd">{submenu.foot.kbd}</span>}
        </div>
      }
    </div>);

}

// ─── App ────────────────────────────────────────────

function HorizontalDashboard() {
  const [active, setActive] = useState('visao');
  const [activeSub, setActiveSub] = useState(null);
  const [openMenu, setOpenMenu] = useState(null);
  const [period, setPeriod] = useState('Hoje');
  const [chartMode, setChartMode] = useState('Receita');
  const [theme, setTheme] = useState('light');
  const [toast, setToast] = useState(null);

  // Apply theme
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

  // Toast helper
  const flashToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), 1800);
  };

  const handleUserAction = (id) => {
    const labels = {
      perfil: 'Meu perfil',
      caixa: 'Detalhes do caixa',
      turno: 'Fechando turno…',
      notif: 'Preferências de notificações',
      atalhos: 'Atalhos do teclado',
      config: 'Configurações',
      ajuda: 'Abrindo chat de suporte…',
      'trocar-tenant': 'Trocar de unidade',
      sair: 'Sessão encerrada',
    };
    flashToast(labels[id] || id);
  };

  // Esc + click-outside to close
  useEffect(() => {
    const onKey = (e) => { if (e.key === 'Escape') setOpenMenu(null); };
    const onDocClick = (e) => {
      if (!openMenu) return;
      // Keep open if click landed inside a dropdown or on the trigger nav item
      if (e.target.closest('.menu-dropdown')) return;
      if (e.target.closest('.h-nav-item')) return;
      setOpenMenu(null);
    };
    window.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onDocClick);
    return () => {
      window.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onDocClick);
    };
  }, [openMenu]);

  const pickSub = (parentId, subId) => {
    if (subId === '__foot') {
      setActive(parentId);
      setActiveSub(null);
    } else {
      setActive(parentId);
      setActiveSub(subId);
    }
  };

  // Sub-tab definitions per parent — keeps the second-level nav visible
  const getSubtabs = () => {
    if (active === 'visao') {
      return { kind: 'periods', items: PERIODS, value: period, onChange: setPeriod };
    }
    const parent = NAV.find((n) => n.id === active);
    if (!parent || !parent.submenu) return null;
    const items = parent.submenu.kind === 'mega' ?
    parent.submenu.columns.flatMap((c) => c.items) :
    parent.submenu.items;
    return { kind: 'sub', items: items.map((i) => ({ id: i.id, label: i.label, meta: i.meta, metaKind: i.metaKind })), value: activeSub || items[0].id, onChange: setActiveSub };
  };

  const subtabs = getSubtabs();

  const totalRev = useMemo(() => SALES_BY_HOUR.reduce((a, b) => a + b, 0), []);

  return (
    <div className="h-app">

      {/* ─── Brand bar ─── */}
      <div className="h-bar">
        <div className="brand">
          <div className="brand-mark">F</div>
          <span className="brand-name">Forneria</span>
          <span className="brand-slash">/</span>
          <span className="brand-tenant">Forno do Bairro — Vila Madalena</span>
        </div>

        <div className="topbar-spacer" style={{ flex: 1 }} />

        <div className="h-bar-search input-with-ico">
          <span className="ico"><Icon name="search" size={15} /></span>
          <input className="input" placeholder="Buscar pedido, cliente, produto…" />
        </div>

        <div className="topbar-spacer" style={{ flex: 1 }} />

        <div className="h-bar-actions">
          <button className="topbar-icon-btn" title="Notificações">
            <Icon name="bell" size={16} />
            <span className="has-dot" />
          </button>
          <button className="topbar-icon-btn" title="Imprimir">
            <Icon name="print" size={16} />
          </button>
          <div className="divider-v" />
          <UserMenu
            user={{ initials: 'CR', name: 'Carla Rocha', role: 'Gerente · PDV-02', email: 'carla.rocha@forneria.app' }}
            theme={theme}
            onTheme={setTheme}
            onAction={handleUserAction} />
        </div>
      </div>

      {/* ─── Horizontal nav ─── */}
      <div className="h-nav">
        {NAV.map((n) =>
        <NavWithSubmenu key={n.id}
        item={n}
        active={active}
        openMenu={openMenu}
        setOpenMenu={setOpenMenu}
        onSelect={(id) => {setActive(id);setActiveSub(null);}}
        onSubSelect={pickSub} />
        )}
        <div className="h-nav-spacer" />
        <div className="h-nav-aux">
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--good)', boxShadow: '0 0 0 3px color-mix(in oklab, var(--good) 20%, transparent)' }} />
            Caixa aberto · 04:32:18
          </span>
        </div>
      </div>

      {/* Backdrop removed — click-outside handled by document listener
          (avoided stacking-context conflict where the sticky nav trapped the dropdown). */}

      {/* ─── Sub-tabs row (context-aware) ─── */}
      {subtabs && subtabs.kind === 'periods' &&
      <div className="h-subtabs">
          {subtabs.items.map((p) =>
        <button key={p}
        className={'h-subtab ' + (subtabs.value === p ? 'active' : '')}
        onClick={() => subtabs.onChange(p)}>
              {p}
            </button>
        )}
        </div>
      }
      {subtabs && subtabs.kind === 'sub' &&
      <div className="h-subtabs">
          {subtabs.items.map((s) =>
        <button key={s.id}
        className={'h-subtab ' + (subtabs.value === s.id ? 'active' : '')}
        onClick={() => subtabs.onChange(s.id)}>
              {s.label}
              {s.meta && <span className={'menu-dd-meta ' + (s.metaKind || '')} style={{ marginLeft: 8 }}>{s.meta}</span>}
            </button>
        )}
        </div>
      }

      {/* ─── Page content ─── */}
      <div className="h-page">

        <div className="h-page-head">
          <div>
            <h1 className="h-page-title">
              {active === 'visao' ? 'Visão geral' : NAV.find((n) => n.id === active)?.label}
              {activeSub && (() => {
                const parent = NAV.find((n) => n.id === active);
                const items = parent?.submenu?.kind === 'mega' ?
                parent.submenu.columns.flatMap((c) => c.items) :
                parent?.submenu?.items || [];
                const sub = items.find((i) => i.id === activeSub);
                return sub ?
                <span style={{ color: 'var(--fg-soft)', fontWeight: 400 }}>
                    {' / '}{sub.label}
                  </span> :
                null;
              })()}
            </h1>
            <p className="h-page-sub">
              {period === 'Hoje' ? '22 de maio · ' + new Date().toLocaleDateString('pt-BR', { weekday: 'long' }) : period}
              {' · '}atualizado há 12s
            </p>
          </div>
          <div className="h-page-actions">
            <button className="btn btn-ghost">
              <Icon name="sliders" size={13} /> Personalizar
            </button>
            <button className="btn">
              <Icon name="print" size={13} /> Exportar
            </button>
            <button className="btn btn-primary">
              <Icon name="plus" size={13} /> Novo pedido
            </button>
          </div>
        </div>

        {/* KPIs */}
        <div className="h-kpis">
          <Kpi label="Receita" icon="cash"
          unit="R$" value="11.266,65"
          delta="+18,4%" deltaDir="up"
          spark={SALES_BY_HOUR.slice(6)} />
          <Kpi label="Pedidos" icon="cart"
          value="142"
          delta="+9,2%" deltaDir="up"
          spark={[18, 22, 28, 24, 32, 30, 40, 38, 46, 52, 58, 62]} />
          <Kpi label="Ticket médio" icon="card"
          unit="R$" value="79,34"
          delta="+8,4%" deltaDir="up"
          spark={[62, 64, 66, 68, 70, 72, 74, 76, 78, 79, 80, 79]} color="#7c3aed" />
          <Kpi label="Tempo médio" icon="clock"
          value="24'18"
          delta="−2'04" deltaDir="down"
          vs="meta 25'00"
          spark={[28, 26, 29, 27, 26, 25, 24, 23, 25, 24, 23, 24]} color="#16a34a" />
        </div>

        {/* Chart + Channels */}
        <div className="h-row">
          <div className="h-card">
            <div className="h-card-head">
              <div>
                <h3>Vendas por hora</h3>
                <div className="sub">
                  Pico previsto às 20:00 — {BRL(1820)} ·
                  total parcial <b style={{ color: 'var(--fg)' }}>{BRL(totalRev)}</b>
                </div>
              </div>
              <div className="h-card-head-actions">
                <div className="h-mini-seg">
                  {['Receita', 'Pedidos', 'Itens'].map((m) =>
                  <button key={m}
                  className={chartMode === m ? 'active' : ''}
                  onClick={() => setChartMode(m)}>{m}</button>
                  )}
                </div>
              </div>
            </div>
            <div className="h-chart">
              <SalesChart data={SALES_BY_HOUR} />
            </div>
          </div>

          <div className="h-card">
            <div className="h-card-head">
              <div>
                <h3>Canais</h3>
                <div className="sub">Distribuição da receita</div>
              </div>
              <button className="btn-ghost" style={{ padding: '4px 8px', fontSize: 11 }}>
                Ver detalhes <Icon name="chevron-right" size={12} />
              </button>
            </div>
            <div className="h-channels">
              {CHANNELS.map((c) =>
              <div key={c.id} className="h-channel">
                  <div className="h-channel-name">
                    <span className="dot" style={{ background: c.color }} />
                    {c.name}
                  </div>
                  <div className="h-channel-bar">
                    <div style={{ width: c.pct + '%', background: c.color }} />
                  </div>
                  <div className="h-channel-val">
                    {BRL(c.total)}
                    <div style={{ fontSize: 10.5, color: 'var(--fg-soft)', fontWeight: 500 }}>{c.pct}%</div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Recent orders + Top products */}
        <div className="h-row">
          <div className="h-card">
            <div className="h-card-head">
              <div>
                <h3>Pedidos recentes</h3>
                <div className="sub">28 ativos · 4 canais — atualização em tempo real</div>
              </div>
              <div className="h-card-head-actions">
                <button className="btn-ghost" style={{ padding: '4px 8px', fontSize: 11 }}>
                  Ver todos <Icon name="chevron-right" size={12} />
                </button>
              </div>
            </div>
            <table className="h-table">
              <thead>
                <tr>
                  <th>Pedido</th>
                  <th>Cliente / Mesa</th>
                  <th>Canal</th>
                  <th>Status</th>
                  <th className="right">Total</th>
                </tr>
              </thead>
              <tbody>
                {RECENT_ORDERS.map((o) =>
                <tr key={o.id}>
                    <td>
                      <div className="h-order-id">{o.id}</div>
                      <div className="h-order-meta">{o.time}</div>
                    </td>
                    <td>
                      <div className="h-order-cust">{o.cust}</div>
                      <div className="h-order-meta" style={{ fontFamily: 'var(--font-ui)', color: 'var(--fg-muted)' }}>{o.items}</div>
                    </td>
                    <td>
                      <span className={'pill-channel ' + o.channel}>{o.channel}</span>
                    </td>
                    <td>
                      <span className={'status ' + o.status}>
                        <span className="dot" />
                        {o.status === 'cozinha' ? 'Na cozinha' :
                      o.status === 'entrega' ? 'Em entrega' :
                      o.status === 'pronto' ? 'Pronto' :
                      'Aberto'}
                      </span>
                    </td>
                    <td className="right h-order-total">{BRL(o.total)}</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div className="h-card">
              <div className="h-card-head">
                <div>
                  <h3>Mais vendidos</h3>
                  <div className="sub">Top 7 · por receita</div>
                </div>
                <div className="h-card-head-actions">
                  <button className="btn-ghost" style={{ padding: '4px 8px', fontSize: 11 }}>
                    <Icon name="sliders" size={12} />
                  </button>
                </div>
              </div>
              <div className="h-top">
                {TOP_PRODUCTS.map((p, i) =>
                <div key={p.name} className="h-top-row">
                    <div className="h-top-rank">{String(i + 1).padStart(2, '0')}</div>
                    <div className="h-top-info">
                      <div className="h-top-name">{p.name}</div>
                      <div className="h-top-bar"><div style={{ width: p.share + '%' }} /></div>
                    </div>
                    <div className="h-top-value">
                      {BRL(p.value)}
                      <div className="h-top-units">{p.qty} un.</div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            <div className="h-card">
              <div className="h-card-head">
                <div>
                  <h3>Eventos do turno</h3>
                  <div className="sub">Alertas e marcos operacionais</div>
                </div>
              </div>
              <div className="h-events">
                {EVENTS.map((e, i) =>
                <div key={i} className="h-event">
                    <div className={'h-event-ico ' + e.kind}>
                      <Icon name={e.icon} size={13} />
                    </div>
                    <div className="h-event-text">{e.text}</div>
                    <div className="h-event-time">{e.time}</div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

      </div>

      {toast && (
        <div className="toast" style={{
          position: 'fixed', bottom: 24, left: '50%', transform: 'translateX(-50%)',
          background: 'var(--fg)', color: 'var(--bg)',
          padding: '10px 18px', borderRadius: 999,
          fontSize: 13, fontWeight: 600,
          boxShadow: 'var(--shadow-lg)', zIndex: 100,
          display: 'inline-flex', alignItems: 'center', gap: 8,
          animation: 'dd-in 180ms cubic-bezier(.2,.8,.3,1)',
        }}>
          <Icon name="check" size={14} />
          {toast}
        </div>
      )}
    </div>);

}

ReactDOM.createRoot(document.getElementById('root')).render(<HorizontalDashboard />);