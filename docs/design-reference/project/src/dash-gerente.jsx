// ——— Dashboard — Visão Gerente / Dono ———
const { useEffect: useGE, useRef: useGR } = React;
const GIcon = window.Icon;

function StatusBar() {
  return (
    <div className="d-status-bar">
      <div className="d-status-card">
        <div className="d-status-label"><span style={{width:7,height:7,borderRadius:'50%',background:'var(--good)'}} />Caixa</div>
        <div className="d-status-val" style={{color:'var(--good)'}}>ABERTO</div>
        <div className="d-status-sub">desde 11:00</div>
      </div>
      <div className="d-status-card">
        <div className="d-status-label"><GIcon name="table" size={11} />Mesas</div>
        <div className="d-status-val">8 <span style={{color:'var(--fg-soft)',fontSize:18}}>/ 12</span></div>
        <div className="d-status-bar-wrap"><div className="d-status-bar-fill" style={{width:'66%'}} /></div>
        <div className="d-status-sub">ocupadas</div>
      </div>
      <div className="d-status-card">
        <div className="d-status-label"><GIcon name="cart" size={11} />Pedidos</div>
        <div className="d-status-val">5</div>
        <div className="d-status-sub">em aberto agora</div>
      </div>
      <div className="d-status-card">
        <div className="d-status-label"><GIcon name="pos" size={11} />KDS</div>
        <div className="d-status-val">3</div>
        <div className="d-status-sub">na fila · em preparo</div>
      </div>
    </div>
  );
}

function MetricCards() {
  const cards = [
    { label:'Faturamento',  icon:'card',    val:'R$ 3.847,00', delta:'+8%',      comp:'R$ 3.562 ontem' },
    { label:'Pedidos',      icon:'cart',    val:'47',          delta:'+5',       comp:'vs período anterior' },
    { label:'Ticket Médio', icon:'sparkle', val:'R$ 81,85',    delta:'+R$ 3,20', comp:'vs período anterior' },
    { label:'vs Ontem',     icon:'flame',   val:'+8%',         delta:null,       comp:'R$ 3.562 ontem' },
  ];
  return (
    <div className="d-metric-cards">
      {cards.map((c,i) => (
        <div key={i} className="d-metric-card">
          <div style={{display:'flex',justifyContent:'space-between',alignItems:'flex-start'}}>
            <span className="d-metric-label">{c.label}</span>
            <span className="d-metric-icon"><GIcon name={c.icon} size={16} /></span>
          </div>
          <div className="d-metric-val">{c.val}</div>
          {c.delta
            ? <div style={{display:'flex',gap:8,alignItems:'center'}}>
                <span className="d-metric-delta up">↑ {c.delta}</span>
                <span className="d-metric-comp">{c.comp}</span>
              </div>
            : <span className="d-metric-comp">{c.comp}</span>}
        </div>
      ))}
    </div>
  );
}

function RevenueChart({ dark }) {
  const ref = useGR(null), inst = useGR(null);
  useGE(() => {
    if (!ref.current || !window.Chart) return;
    if (inst.current) inst.current.destroy();
    const ctx = ref.current.getContext('2d');
    const grad = ctx.createLinearGradient(0,0,0,260);
    grad.addColorStop(0,'rgba(217,119,6,0.32)'); grad.addColorStop(1,'rgba(217,119,6,0.02)');
    inst.current = new Chart(ctx, {
      type:'line',
      data:{ labels: HOURS_LBL, datasets:[
        { label:'Hoje',  data: REVENUE_TODAY, borderColor:'#d97706', backgroundColor:grad, fill:true, tension:0.4, borderWidth:2.5, pointRadius:0, pointHoverRadius:5 },
        { label:'Ontem', data: REVENUE_PREV,  borderColor:'#9ca3af', borderDash:[4,4], fill:false, tension:0.4, borderWidth:1.5, pointRadius:0 },
      ]},
      options:{
        responsive:true, maintainAspectRatio:false,
        plugins:{ legend:{display:false}, tooltip:{ backgroundColor:'#1a1612', padding:10, cornerRadius:6 } },
        scales:{
          x:{ grid:{display:false}, ticks:{ color: dark?'#94a3b8':'#9ca3af', font:{size:11, family:'JetBrains Mono'} } },
          y:{ grid:{ color: dark?'rgba(255,255,255,0.05)':'rgba(0,0,0,0.04)' }, ticks:{ color: dark?'#94a3b8':'#9ca3af', font:{size:11, family:'JetBrains Mono'}, callback: v => 'R$' + v } },
        },
      },
    });
    return () => inst.current && inst.current.destroy();
  }, [dark]);
  return <div style={{height:280}}><canvas ref={ref} /></div>;
}

function TypeDonut({ dark }) {
  const ref = useGR(null), inst = useGR(null);
  const total = ORDER_TYPES.reduce((a,b) => a + b.value, 0);
  useGE(() => {
    if (!ref.current || !window.Chart) return;
    if (inst.current) inst.current.destroy();
    inst.current = new Chart(ref.current.getContext('2d'), {
      type:'doughnut',
      data:{ labels: ORDER_TYPES.map(t => t.name), datasets:[{ data: ORDER_TYPES.map(t => t.value), backgroundColor: ORDER_TYPES.map(t => t.color), borderColor: dark?'#2a241d':'#fff', borderWidth:3 }] },
      options:{ responsive:true, maintainAspectRatio:false, cutout:'68%', plugins:{ legend:{display:false}, tooltip:{ callbacks:{ label: c => c.label + ': ' + BRL(c.parsed) } } } },
    });
    return () => inst.current && inst.current.destroy();
  }, [dark]);
  return (
    <div>
      <div style={{position:'relative', height:200}}>
        <canvas ref={ref} />
        <div className="d-donut-center"><div className="d-donut-val">R$ 3,8k</div><div className="d-donut-lbl">total</div></div>
      </div>
      <div style={{display:'flex',flexDirection:'column',gap:7,marginTop:12}}>
        {ORDER_TYPES.map(t => (
          <div key={t.name} style={{display:'flex',justifyContent:'space-between',alignItems:'center',fontSize:13}}>
            <span style={{display:'flex',alignItems:'center',gap:8}}>
              <span style={{width:9,height:9,borderRadius:'50%',background:t.color}} />
              <span style={{color:'var(--fg-muted)'}}>{t.name}</span>
            </span>
            <span style={{display:'flex',gap:14,alignItems:'center'}}>
              <span style={{fontWeight:700,fontFamily:'var(--font-mono)'}}>{Math.round(t.value/total*100)}%</span>
              <span style={{fontFamily:'var(--font-mono)',color:'var(--fg-muted)',fontSize:12,minWidth:70,textAlign:'right'}}>{BRL(t.value)}</span>
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function OrdersTable({ orders, newOrderId, onOpenModal }) {
  const sb = (s) => s === 'entregue' ? <span className="d-status-badge entregue">✓ Entregue</span>
                  : s === 'preparo'  ? <span className="d-status-badge preparo">⏱ Preparo</span>
                                     : <span className="d-status-badge cancelado">✕ Cancelado</span>;
  return (
    <div className="d-card" style={{overflow:'hidden'}}>
      <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
        <div>
          <div className="d-card-title">Pedidos recentes</div>
          <div className="d-card-sub">Tempo real · clique para detalhes</div>
        </div>
        <button className="d-link-btn">Ver todos →</button>
      </div>
      <table className="d-table">
        <thead><tr>
          <th>#</th><th>Tipo</th><th>Cliente</th><th>Itens</th>
          <th style={{textAlign:'right'}}>Valor</th><th>Status</th><th>Hora</th>
        </tr></thead>
        <tbody>
          {orders.map(o => (
            <tr key={o.id} className={newOrderId === o.id ? 'new-row' : ''} onClick={() => onOpenModal(o)}>
              <td><span className="d-order-id">#{o.id}</span></td>
              <td><span className={'d-type-badge ' + o.type}>{o.type === 'mesa' ? '🍽' : o.type === 'delivery' ? '🛵' : '🏃'} {o.typeName}</span></td>
              <td style={{fontSize:13,fontWeight:500}}>{o.customer}</td>
              <td><span className="d-items-clip">{o.items}</span></td>
              <td style={{textAlign:'right'}}><span style={{fontFamily:'var(--font-mono)',fontWeight:600}}>{BRL(o.value)}</span></td>
              <td>{sb(o.status)}</td>
              <td><span style={{fontFamily:'var(--font-mono)',color:'var(--fg-soft)',fontSize:12}}>{o.time}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="d-table-footer">
        <span>Mostrando {orders.length} de 47 pedidos hoje</span>
        <span>Atualizado em tempo real</span>
      </div>
    </div>
  );
}

function TopProducts() {
  const max = TOP_PRODUCTS[0].value;
  return (
    <div className="d-card">
      <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
        <span className="d-card-title">Mais vendidos</span>
        <span style={{fontSize:12,color:'var(--fg-muted)'}}>Hoje</span>
      </div>
      <div style={{padding:'4px 18px 14px'}}>
        {TOP_PRODUCTS.map(p => {
          const cls = p.rank === 1 ? 'gold' : p.rank === 2 ? 'silver' : p.rank === 3 ? 'bronze' : '';
          return (
            <div key={p.rank} className="d-product-row">
              <span className={'d-rank ' + cls}>{p.rank}</span>
              <div style={{flex:1, minWidth:0}}>
                <div style={{fontWeight:600,fontSize:13.5,whiteSpace:'nowrap',overflow:'hidden',textOverflow:'ellipsis'}}>{p.name}</div>
                <div style={{display:'flex',alignItems:'center',gap:8,marginTop:4}}>
                  <span style={{fontSize:11.5,color:'var(--fg-muted)',fontFamily:'var(--font-mono)'}}>{p.qty} vendidos</span>
                  <div style={{flex:1,height:3,background:'var(--line)',borderRadius:2,overflow:'hidden'}}>
                    <div style={{width:(p.value/max*100)+'%',height:'100%',background:'var(--accent)'}} />
                  </div>
                </div>
              </div>
              <div style={{fontFamily:'var(--font-mono)',fontWeight:700,fontSize:13}}>{BRL(p.value)}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function TableMap() {
  return (
    <div className="d-card">
      <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
        <span className="d-card-title">Mapa de mesas</span>
        <div style={{display:'flex',gap:12,fontSize:11,color:'var(--fg-muted)'}}>
          <span style={{display:'flex',alignItems:'center',gap:5}}><span style={{width:8,height:8,borderRadius:2,background:'var(--good)',opacity:0.4}} />Livre</span>
          <span style={{display:'flex',alignItems:'center',gap:5}}><span style={{width:8,height:8,borderRadius:2,background:'var(--danger)',opacity:0.4}} />Ocupada</span>
          <span style={{display:'flex',alignItems:'center',gap:5}}><span style={{width:8,height:8,borderRadius:2,background:'var(--warn)',opacity:0.4}} />Conta</span>
        </div>
      </div>
      <div style={{padding:18}}>
        <div className="d-table-map">
          {TABLES_STATE.map(t => (
            <div key={t.n} className={'d-table-cell ' + t.status}>
              <span>{t.n}</span>
              <span className="d-table-cell-lbl">{t.status}</span>
            </div>
          ))}
        </div>
        <div style={{display:'flex',justifyContent:'space-between',alignItems:'center',marginTop:14,fontSize:12,color:'var(--fg-muted)'}}>
          <span>8 ocupadas · 4 livres</span>
          <span style={{color:'var(--accent)',fontWeight:600,cursor:'pointer'}}>Abrir salão →</span>
        </div>
      </div>
    </div>
  );
}

function OrderModal({ order, onClose }) {
  if (!order) return null;
  return (
    <div className="d-modal-overlay" onClick={onClose}>
      <div className="d-modal" onClick={e => e.stopPropagation()}>
        <div className="d-modal-head">
          <div>
            <div className="d-modal-title">Pedido #{order.id} · {order.typeName}</div>
            <div style={{fontSize:12,color:'var(--fg-muted)',marginTop:3,fontFamily:'var(--font-mono)'}}>{order.time} · {order.customer}</div>
          </div>
          <button className="btn-ghost" onClick={onClose}><GIcon name="x" size={16} /></button>
        </div>
        <div className="d-modal-body">
          <div>
            <div className="d-modal-section-title">Itens</div>
            <div style={{fontSize:13}}>{order.items}</div>
          </div>
          <div>
            <div className="d-modal-section-title">Pagamento</div>
            <div style={{display:'flex',justifyContent:'space-between',fontSize:13}}>
              <span style={{color:'var(--fg-muted)'}}>Total</span>
              <span style={{fontFamily:'var(--font-mono)',fontWeight:700,fontSize:18}}>{BRL(order.value)}</span>
            </div>
          </div>
        </div>
        <div className="d-modal-foot">
          <button className="btn" onClick={onClose}>Fechar</button>
          <button className="btn btn-primary"><GIcon name="print" size={13} />Imprimir</button>
        </div>
      </div>
    </div>
  );
}

function GerenteView({ orders, newOrderId, dark, onOpenModal }) {
  return (
    <div className="d-main-inner">
      <StatusBar />
      <MetricCards />
      <div className="d-charts-row">
        <div className="d-card">
          <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
            <div>
              <div className="d-card-title">Faturamento do período</div>
              <div className="d-card-sub">Comparativo com período anterior</div>
            </div>
            <div style={{display:'flex',gap:14,fontSize:12,color:'var(--fg-muted)'}}>
              <span style={{display:'flex',alignItems:'center',gap:6}}><span style={{width:10,height:10,borderRadius:'50%',background:'#d97706'}} />Hoje</span>
              <span style={{display:'flex',alignItems:'center',gap:6}}><span style={{width:14,height:2,background:'#9ca3af'}} />Ontem</span>
            </div>
          </div>
          <div style={{padding:'14px 18px 18px'}}><RevenueChart dark={dark} /></div>
        </div>
        <div className="d-card">
          <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
            <div>
              <div className="d-card-title">Por tipo de pedido</div>
              <div className="d-card-sub">Distribuição do faturamento</div>
            </div>
          </div>
          <div style={{padding:'14px 18px 18px'}}><TypeDonut dark={dark} /></div>
        </div>
      </div>
      <OrdersTable orders={orders} newOrderId={newOrderId} onOpenModal={onOpenModal} />
      <div className="d-bottom-row">
        <TopProducts />
        <TableMap />
      </div>
    </div>
  );
}

Object.assign(window, { GerenteView, OrderModal });
