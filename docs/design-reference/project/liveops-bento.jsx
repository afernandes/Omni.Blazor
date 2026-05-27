// ════════════════════════════════════════════════
// Forneria — Live Operations (Bento/Lego layout)
// Demonstra os 3 padrões: panel (topbar), bento (grid), tile (cards)
// ════════════════════════════════════════════════

const { useState, useEffect, useMemo } = React;
const BRL = (n) => 'R$ ' + n.toFixed(2).replace('.', ',');
const fmt = (n) => n.toLocaleString('pt-BR');

// ─── Live clock ──
function useClock() {
  const [t, setT] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setT(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  const pad = (n) => String(n).padStart(2, '0');
  return `${pad(t.getHours())}:${pad(t.getMinutes())}:${pad(t.getSeconds())}`;
}

// ─── Mock data ─────────────────────────────────────

const HOURS_DATA = [
  3, 2, 1, 1, 1, 2, 4, 8, 14, 18, 22, 28,
  42, 48, 38, 26, 22, 32, 56, 78, 92, 64, 38, 14,
];
const CURRENT_HOUR = new Date().getHours();

const STATIONS = [
  { name: 'Pizza',    val: 6,  sub: 'na fila',     icon: 'pizza',   alert: false },
  { name: 'Burger',   val: 4,  sub: 'na fila',     icon: 'flame',   alert: false },
  { name: 'Fritura',  val: 2,  sub: 'na fila',     icon: 'bag',     alert: false },
  { name: 'Bebidas',  val: 8,  sub: 'expedição',   icon: 'cart',    alert: false },
];

const CHANNELS = [
  { name: 'PDV',     pct: 38, val: 'R$ 4.280', color: '#1a1612' },
  { name: 'Digital', pct: 27, val: 'R$ 3.042', color: '#dc2626' },
  { name: 'iFood',   pct: 18, val: 'R$ 2.028', color: '#ea1d2c' },
  { name: 'Mesa',    pct: 12, val: 'R$ 1.352', color: '#16a34a' },
  { name: 'Garçom',  pct: 5,  val: 'R$ 563',   color: '#7c3aed' },
];

const QUEUE = [
  { id: '4287', name: 'Pizza Calabresa G',     meta: 'Mesa 12 · 2 itens',         time: '12:34', status: 'late' },
  { id: '4286', name: 'Combo Smash + Fritas',  meta: 'João Pereira · PDV',        time: '08:12', status: 'warn' },
  { id: '4285', name: 'Família 4Q ½ Margherita', meta: 'Mesa 03 · 4 itens',       time: '05:48', status: '' },
  { id: '4284', name: 'Pizza Pepperoni G',     meta: 'iFood · Pedro Lima',        time: '03:21', status: '' },
  { id: '4283', name: 'Cheeseburger Duplo',    meta: 'Digital · Carla Mendes',    time: '02:14', status: '' },
  { id: '4282', name: 'Veggie Burger',         meta: 'Digital · Lucas Andrade',   time: '01:08', status: '' },
];

const TICKER = [
  { id: '4290', cust: 'Marina Costa',  items: '1× Pizza Calabresa G, 1× Coca 2L',       total: 87.80, channel: '#dc2626', time: 'há 12s' },
  { id: '4289', cust: 'Mesa 08',       items: '1× Pizza Margherita G, 1× Suco',         total: 67.00, channel: '#16a34a', time: 'há 1min' },
  { id: '4288', cust: 'João Pereira',  items: '2× Cheeseburger Duplo, 1× Cerveja',       total: 78.00, channel: '#1a1612', time: 'há 2min' },
  { id: '4287', cust: 'Família L.',    items: '1× Família 4 sabores, 2× Cerveja LN',    total: 96.00, channel: '#16a34a', time: 'há 4min' },
  { id: '4286', cust: 'Pedro Lima',    items: '1× Combo Smash + Fritas',                 total: 52.00, channel: '#ea1d2c', time: 'há 6min' },
  { id: '4285', cust: 'Carla Mendes',  items: '1× Veggie Burger, 1× Suco Laranja',       total: 35.00, channel: '#dc2626', time: 'há 9min' },
];

const PIZZAS_NOW = [
  { id: '#4287/A', name: 'Calabresa G',         mods: 'sem cebola',           time: '04:12', status: 'late' },
  { id: '#4285/B', name: '½ Quatro Q. / ½ Marg.', mods: 'borda catupiry',       time: '02:48', status: 'warn' },
  { id: '#4282/A', name: 'Pepperoni G',         mods: '+catupiry',             time: '01:30', status: '' },
  { id: '#4290/A', name: 'Frango c/ Catupiry G',mods: '',                       time: '00:54', status: '' },
  { id: '#4291/A', name: 'Margherita M',        mods: 'massa fina',             time: '00:22', status: '' },
  { id: '#4292/A', name: 'Família 4 sabores',   mods: 'borda cheddar',          time: '00:08', status: '' },
];

// ─── App ──────────────────────────────────────────

function LiveOps() {
  const clock = useClock();
  const [view, setView] = useState('Operação');

  // Compute current totals (a tiny derived value for the hero KPI)
  const total = useMemo(() => HOURS_DATA.reduce((s, x) => s + x, 0), []);

  return (
    <div className="lo-app">
      {/* ─── Brand bar (uses .surface-panel pattern via .lo-bar) ─── */}
      <div className="lo-bar">
        <div className="lo-title">
          <span className="live-dot" />
          Live Operations
        </div>
        <div className="lo-segments">
          {['Operação', 'Cozinha', 'Entrega', 'Salão'].map(v => (
            <button key={v} className={'lo-segment ' + (view === v ? 'active' : '')}
                    onClick={() => setView(v)}>{v}</button>
          ))}
        </div>
        <div className="lo-clock">{clock}</div>
      </div>

      {/* ─── Stage ─── */}
      <div className="lo-stage">

        {/* Row 1: KPI bento (4 columns, edge-to-edge) */}
        <div className="page-bento cols-4 rounded">
          <div className="surface-tile kpi-tile is-accent">
            <div className="kpi-label">
              <span className="ico"><Icon name="cash" size={13} /></span>
              Receita do turno
            </div>
            <div className="kpi-value">
              <span className="unit">R$</span>11.266
            </div>
            <div className="kpi-delta">▲ +18,4% <span className="vs">vs ontem</span></div>
          </div>

          <div className="surface-tile kpi-tile">
            <div className="kpi-label">
              <span className="ico"><Icon name="cart" size={13} /></span>
              Pedidos
            </div>
            <div className="kpi-value">142</div>
            <div className="kpi-delta up">▲ +9,2% <span className="vs">vs ontem</span></div>
          </div>

          <div className="surface-tile kpi-tile">
            <div className="kpi-label">
              <span className="ico"><Icon name="clock" size={13} /></span>
              Tempo médio
            </div>
            <div className="kpi-value">24'18</div>
            <div className="kpi-delta down">▼ −2'04 <span className="vs">meta 25'00</span></div>
          </div>

          <div className="surface-tile kpi-tile">
            <div className="kpi-label">
              <span className="ico"><Icon name="card" size={13} /></span>
              Ticket médio
            </div>
            <div className="kpi-value">
              <span className="unit">R$</span>79,34
            </div>
            <div className="kpi-delta up">▲ +8,4% <span className="vs">vs ontem</span></div>
          </div>
        </div>

        {/* Row 2: 3-col bento — Fila/Canais/Hora */}
        <div className="page-bento cols-3 rounded" style={{ gridAutoRows: 'minmax(220px, auto)' }}>
          {/* Fila da cozinha — spans 2 columns */}
          <div className="surface-tile span-c2">
            <div className="surface-tile-head">
              <span className="label">Fila da cozinha · {QUEUE.length} ativos</span>
              <span className="meta">ordem por tempo de prep.</span>
            </div>
            <div className="queue">
              {QUEUE.map((q, i) => (
                <div key={q.id} className="queue-row">
                  <span className="queue-num">#{q.id}</span>
                  <div className="queue-info">
                    <div className="queue-name">{q.name}</div>
                    <div className="queue-meta">{q.meta}</div>
                  </div>
                  <span className={'queue-time ' + (q.status === 'late' ? 'late' : '')}>{q.time}</span>
                  <span className={'queue-status ' + q.status} />
                </div>
              ))}
            </div>
          </div>

          {/* Canais */}
          <div className="surface-tile">
            <div className="surface-tile-head">
              <span className="label">Canais</span>
              <span className="meta">% receita</span>
            </div>
            <div className="channels">
              {CHANNELS.map(c => (
                <div key={c.name} className="channel-row">
                  <div className="channel-name">
                    <span className="channel-dot" style={{ background: c.color }} />
                    {c.name}
                  </div>
                  <div className="channel-bar">
                    <div style={{ width: c.pct + '%', background: c.color }} />
                  </div>
                  <div className="channel-val">{c.pct}%</div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Row 3: Hourly sparkbars (full width) */}
        <div className="page-bento cols-1 rounded" style={{ gridTemplateColumns: '1fr' }}>
          <div className="surface-tile">
            <div className="surface-tile-head">
              <span className="label">Receita por hora · pico previsto 20:00</span>
              <span className="meta">R$ {fmt(total)} acumulado</span>
            </div>
            <div className="sparkbars">
              {HOURS_DATA.map((v, h) => {
                const max = Math.max(...HOURS_DATA);
                return (
                  <div key={h}
                       className={h === CURRENT_HOUR ? 'now' : ''}
                       title={`${String(h).padStart(2,'0')}h — R$ ${v * 100}`}
                       style={{ height: `${Math.max(4, (v / max) * 100)}%` }} />
                );
              })}
            </div>
          </div>
        </div>

        {/* Row 4: Stations bento — 4 mini-tiles in a single rounded card */}
        <div className="page-bento rounded" style={{ gridTemplateColumns: '1fr', gridAutoRows: 'minmax(0, auto)' }}>
          <div className="surface-tile">
            <div className="surface-tile-head">
              <span className="label">Estações</span>
              <span className="meta">tempo médio por estação</span>
            </div>
            <div className="stations">
              {STATIONS.map(s => (
                <div key={s.name} className={'station ' + (s.alert ? 'alert' : '')}>
                  <div className="station-name">
                    <Icon name={s.icon} size={12} />
                    {s.name}
                  </div>
                  <div className="station-val">{s.val}</div>
                  <div className="station-sub">{s.sub}</div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Row 5: Big asymmetric bento — Pizza station + Fleet + Ticker */}
        <div className="page-bento cols-3 rounded" style={{ gridAutoRows: 'minmax(280px, auto)' }}>
          {/* Pizza station — spans 2 cols */}
          <div className="surface-tile pizza-station span-c2">
            <div className="ps-head">
              <div className="title">
                <Icon name="pizza" size={14} />
                Estação Pizza
              </div>
              <div className="meta">{PIZZAS_NOW.length} em forno · forno 280°C</div>
            </div>
            <div className="ps-grid">
              {PIZZAS_NOW.map(p => (
                <div key={p.id} className={'ps-pizza ' + (p.status === 'late' ? 'is-late' : p.status === 'warn' ? 'is-warn' : '')}>
                  <div className="ps-pizza-id">{p.id}</div>
                  <div className="ps-pizza-name">{p.name}</div>
                  {p.mods && <div className="ps-pizza-mods">{p.mods}</div>}
                  <div className={'ps-pizza-time ' + p.status}>{p.time}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Fleet map */}
          <div className="surface-tile fleet">
            <div className="surface-tile-head">
              <span className="label">Frota · 3 em rota</span>
              <span className="meta">ao vivo</span>
            </div>
            <div className="fleet-map">
              <div className="fleet-pin store" style={{ left: '50%', top: '52%' }} />
              <div className="fleet-pin moto" style={{ left: '32%', top: '38%' }} />
              <div className="fleet-pin moto" style={{ left: '68%', top: '70%' }} />
              <div className="fleet-pin late" style={{ left: '20%', top: '74%' }} />
              <div className="fleet-pin idle" style={{ left: '78%', top: '28%' }} />
            </div>
          </div>
        </div>

        {/* Row 6: Order ticker */}
        <div className="page-bento rounded" style={{ gridTemplateColumns: '1fr' }}>
          <div className="surface-tile">
            <div className="surface-tile-head">
              <span className="label">Stream de pedidos · últimos 10 minutos</span>
              <span className="meta">tempo real</span>
            </div>
            <div className="ticker">
              {TICKER.map(t => (
                <div key={t.id} className="ticker-row">
                  <div>
                    <div className="ticker-top">
                      <span className="ticker-channel" style={{ background: t.channel }} />
                      <span className="ticker-id">#{t.id}</span>
                      <span className="ticker-cust">{t.cust}</span>
                    </div>
                    <div className="ticker-items">{t.items}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div className="ticker-total">{BRL(t.total)}</div>
                    <div className="ticker-time">{t.time}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<LiveOps />);
