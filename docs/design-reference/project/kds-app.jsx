// ════════════════════════════════════════════════
// Forneria — KDS (Kitchen Display System)
// Mesmo design system do PDV + garçom, em dark mode.
// ════════════════════════════════════════════════

const { useState, useEffect, useMemo } = React;

function useClock() {
  const [t, setT] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setT(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  const pad = n => String(n).padStart(2, '0');
  return `${pad(t.getHours())}:${pad(t.getMinutes())}:${pad(t.getSeconds())}`;
}

// ─── Mock data ──────────────────────────────────

// Order types: 'table', 'delivery', 'counter', 'ifood'
// Status: 'new' (just arrived), 'prep' (in progress), 'delay', 'urgent', 'ready'
const SECTOR = { icon: 'pizza', label: 'Setor Pizza' };

const TICKETS = [
  {
    id: '#0247', type: 'table', who: 'Mesa 07', meta: '3 pessoas · Ricardo M.',
    status: 'urgent', timer: '18:42', target: '15:00',
    items: [
      { qty: 2, name: 'Pizza Calabresa Grande', mods: [{ k: 'add', t: 'Borda de catupiry' }, { k: 'rm', t: 'Sem azeitona' }], done: false },
      { qty: 1, name: 'Pizza Margherita Média', mods: [], done: true },
    ],
  },
  {
    id: '#0248', type: 'ifood', who: 'Ana Carolina S.', meta: 'iFood · Entrega · 1.2km',
    status: 'delay', timer: '14:08', target: '12:00',
    items: [
      { qty: 1, name: 'Pizza Pepperoni Grande', mods: [{ k: 'add', t: '+catupiry' }, { k: 'note', t: 'massa fina' }], done: false },
      { qty: 1, name: 'Coca-Cola 2L', mods: [], done: true },
    ],
  },
  {
    id: '#0249', type: 'table', who: 'Mesa 12', meta: '4 pessoas · Beatriz O.',
    status: 'prep', timer: '08:14', target: '15:00',
    items: [
      { qty: 1, name: 'Família 4 sabores ½ Margherita ½ Pepperoni', mods: [{ k: 'add', t: 'Borda cheddar' }, { k: 'note', t: '½ sem cebola' }], done: false },
      { qty: 2, name: 'Cerveja Long Neck', mods: [], done: true },
    ],
  },
  {
    id: '#0250', type: 'counter', who: 'João Pereira', meta: 'PDV-02 · Retirada balcão',
    status: 'prep', timer: '06:32', target: '12:00',
    items: [
      { qty: 1, name: 'Pizza Quatro Queijos Grande', mods: [], done: false },
      { qty: 1, name: 'Suco de Laranja 500ml', mods: [], done: true },
    ],
  },
  {
    id: '#0251', type: 'delivery', who: 'Família Bertolucci', meta: 'Própria · 2.4km · Pedro M.',
    status: 'prep', timer: '04:48', target: '15:00',
    items: [
      { qty: 1, name: 'Pizza Família Portuguesa', mods: [{ k: 'rm', t: 'sem ovo' }], done: false },
      { qty: 1, name: 'Pizza Família Pepperoni', mods: [{ k: 'add', t: 'borda catupiry' }], done: false },
    ],
  },
  {
    id: '#0252', type: 'table', who: 'Mesa 03', meta: '2 pessoas · Carla R.',
    status: 'new', timer: '00:42', target: '15:00',
    items: [
      { qty: 1, name: 'Pizza Margherita Média', mods: [], done: false },
      { qty: 1, name: 'Pizza Frango c/ Catupiry Média', mods: [{ k: 'add', t: '+milho' }], done: false },
    ],
  },
  {
    id: '#0253', type: 'ifood', who: 'Lucas Andrade', meta: 'iFood · Entrega · 3.1km',
    status: 'new', timer: '00:18', target: '12:00',
    items: [
      { qty: 1, name: 'Pizza Carbonara Grande', mods: [{ k: 'note', t: 'bem assada' }], done: false },
    ],
  },
  {
    id: '#0254', type: 'table', who: 'Mesa 09', meta: '6 pessoas · Marina C.',
    status: 'ready', timer: '12:08', target: '15:00',
    items: [
      { qty: 2, name: 'Pizza Calabresa Grande', mods: [], done: true },
      { qty: 1, name: 'Pizza Búfala Grande', mods: [], done: true },
      { qty: 1, name: 'Pizza Vegetariana Grande', mods: [], done: true },
    ],
  },
];

const FILTERS = [
  { id: 'all',    label: 'Todos',      kind: '' },
  { id: 'new',    label: 'Novos',      kind: 'info' },
  { id: 'prep',   label: 'Em preparo', kind: 'warn' },
  { id: 'delay',  label: 'Atrasados',  kind: 'warn' },
  { id: 'urgent', label: 'Urgente',    kind: 'danger' },
  { id: 'ready',  label: 'Prontos',    kind: '' },
];

// ─── Components ──────────────────────────────────

function TypeBadge({ type }) {
  const labels = { table: 'Mesa', delivery: 'Entrega', counter: 'Balcão', ifood: 'iFood' };
  return <span className={'t-type ' + type}>{labels[type]}</span>;
}

function Ticket({ ticket, onBump, onToggleItem }) {
  const done = ticket.items.filter(i => i.done).length;
  const pct = ticket.items.length ? (done / ticket.items.length) * 100 : 0;
  const allDone = done === ticket.items.length && ticket.items.length > 0;
  const cls = 'ticket is-' + ticket.status;

  return (
    <div className={cls}>
      <div className="t-head">
        <div className="t-id">{ticket.id}</div>
        <TypeBadge type={ticket.type} />
        <div className="t-timer">
          <span className="v">{ticket.timer}</span>
          <span className="l">meta {ticket.target}</span>
        </div>
        <div className="t-meta">
          <span className="who">{ticket.who}</span>
          <span className="sep">·</span>
          <span>{ticket.meta}</span>
        </div>
        <div className="t-progress">
          <div style={{ width: pct + '%' }} />
        </div>
      </div>

      <div className="t-items">
        {ticket.items.map((it, i) => (
          <div key={i} className={'t-item ' + (it.done ? 'done' : '')}>
            <span className="t-qty">{it.qty}×</span>
            <div className="t-item-body">
              <div className="t-name">{it.name}</div>
              {it.mods && it.mods.length > 0 && (
                <div className="t-mods">
                  {it.mods.map((m, j) => (
                    <span key={j} className={'t-mod ' + m.k}>
                      {m.k === 'add'  ? '+ ' : ''}
                      {m.k === 'rm'   ? '− ' : ''}
                      {m.k === 'note' ? '✎ ' : ''}
                      {m.t}
                    </span>
                  ))}
                </div>
              )}
            </div>
            <button className="t-item-check"
              onClick={() => onToggleItem(ticket.id, i)}
              aria-label={it.done ? 'Desmarcar' : 'Marcar pronto'}>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </button>
          </div>
        ))}
      </div>

      <div className="t-foot">
        <button className={'t-bump ' + (allDone ? 'ready' : '')} onClick={() => onBump(ticket.id)}>
          {allDone ? 'Marcar pronto · enviar à expedição' : 'Bater pedido'}
          <kbd>↵</kbd>
        </button>
        <button className="t-mini" aria-label="Mais opções">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
               strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="5"  r="1.5" />
            <circle cx="12" cy="12" r="1.5" />
            <circle cx="12" cy="19" r="1.5" />
          </svg>
        </button>
      </div>
    </div>
  );
}

// ─── App ─────────────────────────────────────────

function KDSApp() {
  const clock = useClock();
  const [filter, setFilter] = useState('all');
  const [tickets, setTickets] = useState(TICKETS);
  const [sort, setSort] = useState('Hora de chegada');

  const counts = useMemo(() => {
    const c = { all: tickets.length, new: 0, prep: 0, delay: 0, urgent: 0, ready: 0 };
    for (const t of tickets) if (c[t.status] != null) c[t.status]++;
    return c;
  }, [tickets]);

  const visible = useMemo(
    () => filter === 'all' ? tickets : tickets.filter(t => t.status === filter),
    [tickets, filter]
  );

  const toggleItem = (id, idx) => {
    setTickets(ts => ts.map(t => {
      if (t.id !== id) return t;
      const items = t.items.map((it, i) => i === idx ? { ...it, done: !it.done } : it);
      return { ...t, items };
    }));
  };

  const bump = (id) => {
    setTickets(ts => ts.map(t => {
      if (t.id !== id) return t;
      // Cycle status forward: new → prep → ready (done)
      const next = t.status === 'new'    ? 'prep'
                 : t.status === 'prep'   ? 'ready'
                 : t.status === 'delay'  ? 'prep'
                 : t.status === 'urgent' ? 'prep'
                 : 'ready';
      return { ...t, status: next };
    }));
  };

  return (
    <div className="k">
      {/* Top bar */}
      <header className="k-top">
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <div className="k-brand">
            <div className="k-brand-mark">F</div>
            <div>
              <div className="k-brand-name">Forneria</div>
              <div className="k-brand-meta">KDS · v2.4</div>
            </div>
          </div>
          <button className="k-sector">
            <span className="sec-dot" />
            {SECTOR.label}
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="6 9 12 15 18 9" />
            </svg>
          </button>
        </div>

        <div className="k-context">
          <div className="k-clock">{clock}</div>
          <div className="k-counter">
            <b>{counts.all}</b>
            pedidos em aberto
          </div>
        </div>

        <div className="k-actions">
          <a className="k-navlink on">Principal</a>
          <a className="k-navlink">Expedição
            <span className="ct">·{counts.ready}</span>
          </a>
          <a className="k-navlink">Histórico</a>
          <span className="k-online">
            <span className="dot" />
            Online
          </span>
        </div>
      </header>

      {/* Filter bar */}
      <div className="k-filters">
        <div className="k-pills">
          {FILTERS.map(f => (
            <button key={f.id}
              className={'k-pill ' + (filter === f.id ? 'on' : f.kind)}
              onClick={() => setFilter(f.id)}>
              {f.label}
              <span className="ct">{counts[f.id]}</span>
            </button>
          ))}
        </div>
        <button className="k-sort">
          <span className="lbl">Ordenar:</span>
          <span>{sort}</span>
          <svg width="10" height="10" viewBox="0 0 24 24" fill="none"
               stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </button>
      </div>

      {/* Tickets grid */}
      <div className="k-stage">
        <div className="k-grid">
          {visible.map(t => (
            <Ticket key={t.id} ticket={t}
              onBump={bump} onToggleItem={toggleItem} />
          ))}
          {visible.length === 0 && (
            <div style={{
              gridColumn: '1 / -1',
              textAlign: 'center', padding: '80px 20px',
              color: 'var(--fg-soft)', fontSize: 14,
            }}>
              Nenhum pedido no filtro selecionado.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<KDSApp />);
