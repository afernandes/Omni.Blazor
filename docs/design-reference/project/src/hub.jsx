// Order HUB — Kanban of incoming orders
function HubView() {
  const [orders, setOrders] = React.useState(() => HUB_ORDERS.map(o => ({ ...o, col: o.mins < 5 ? 'new' : o.mins < 12 ? 'prep' : o.mins < 20 ? 'ready' : 'out' })));

  const cols = [
    { id: 'new',   label: 'Novos',       sub: 'Aguardando aceite' },
    { id: 'prep',  label: 'Em preparo',  sub: 'Na cozinha' },
    { id: 'ready', label: 'Prontos',     sub: 'Aguardando entregador/balcão' },
    { id: 'out',   label: 'Saiu',        sub: 'A caminho ou retirado' },
  ];

  const move = (id, to) => setOrders(os => os.map(o => o.id === id ? { ...o, col: to } : o));

  return (
    <div className="hub">
      <div className="hub-cols">
        {cols.map(c => {
          const inCol = orders.filter(o => o.col === c.id);
          return (
            <div key={c.id} className="hub-col">
              <div className="hub-col-head">
                <div className="hub-col-title">
                  <span className={'chip-dot'} style={{ background: c.id === 'new' ? 'var(--accent)' : c.id === 'prep' ? 'var(--warn)' : c.id === 'ready' ? 'var(--good)' : 'var(--fg-soft)' }} />
                  {c.label}
                  <span className="badge">{inCol.length}</span>
                </div>
                <button className="btn btn-ghost btn-icon"><Icon name="sliders" size={14} /></button>
              </div>
              <div className="hub-col-body">
                {inCol.map(o => {
                  const late = o.mins > 15 && c.id !== 'out';
                  return (
                    <div key={o.id} className={'hub-card ' + (late ? 'urgent' : o.mins < 4 ? 'fresh' : '')}>
                      <div className="hub-card-top">
                        <span className="hub-card-id">{o.id}</span>
                        <span className={'hub-card-chan ' + o.channel}>{o.channel === 'pdv' ? 'Balcão' : o.channel === 'digital' ? 'Cardápio' : o.channel === 'ifood' ? 'iFood' : o.channel === 'garcom' ? 'Garçom' : o.channel}</span>
                      </div>
                      <div>
                        <div className="hub-card-cust">{o.customer}</div>
                        <div className="hub-card-items">
                          {o.items.join(' · ')}
                        </div>
                      </div>
                      <div className="hub-card-foot">
                        <span className={'hub-card-time ' + (late ? 'late' : '')}>
                          <Icon name="clock" size={11} /> {o.mins}min · {o.mode}
                        </span>
                        <span className="hub-card-total">{BRL(o.total)}</span>
                      </div>
                      <div style={{ display: 'flex', gap: 6 }}>
                        {c.id === 'new'   && <button className="btn btn-primary" style={{ flex: 1, padding: '6px 10px', fontSize: 12, justifyContent: 'center' }} onClick={() => move(o.id, 'prep')}>Aceitar</button>}
                        {c.id === 'prep'  && <button className="btn btn-primary" style={{ flex: 1, padding: '6px 10px', fontSize: 12, justifyContent: 'center' }} onClick={() => move(o.id, 'ready')}>Pronto</button>}
                        {c.id === 'ready' && <button className="btn btn-primary" style={{ flex: 1, padding: '6px 10px', fontSize: 12, justifyContent: 'center' }} onClick={() => move(o.id, 'out')}>Despachar</button>}
                        {c.id === 'out'   && <button className="btn" style={{ flex: 1, padding: '6px 10px', fontSize: 12, justifyContent: 'center' }}>Detalhes</button>}
                      </div>
                    </div>
                  );
                })}
                {inCol.length === 0 && (
                  <div style={{ padding: '30px 10px', textAlign: 'center', color: 'var(--fg-soft)', fontSize: 12 }}>Nenhum pedido aqui</div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

window.HubView = HubView;
