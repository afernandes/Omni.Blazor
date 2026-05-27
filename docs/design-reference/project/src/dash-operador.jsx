// ——— Dashboard — Visão Operador de Caixa ———
const Icon = window.Icon;
const DASH_BRL = window.DASH_BRL;

function OperadorView() {
  const liveOrders = window.DASH_OP_LIVE_ORDERS;

  const actions = [
    { label:'Novo Pedido', icon:'plus',  cls:'primary', desc:'Iniciar atendimento' },
    { label:'Ver Mesas',   icon:'table', cls:'',        desc:'Salão principal'     },
    { label:'Abrir KDS',   icon:'pos',   cls:'',        desc:'Tela da cozinha'     },
    { label:'Pagamentos',  icon:'card',  cls:'',        desc:'Fechar conta'        },
  ];

  const typeClass = name =>
    name.startsWith('Mesa') ? 'mesa' : name === 'Delivery' ? 'delivery' : 'balcao';

  return (
    <div className="d-main-inner">
      {/* Sub-cabeçalho do caixa */}
      <div className="d-card" style={{padding:'16px 20px',display:'flex',alignItems:'center',justifyContent:'space-between',flexWrap:'wrap',gap:12}}>
        <div>
          <div style={{fontSize:15,fontWeight:700,letterSpacing:'-0.01em'}}>Caixa #1 — Anderson Operador</div>
          <div style={{fontSize:12,color:'var(--fg-muted)',marginTop:3,fontFamily:'var(--font-mono)'}}>
            Turno iniciado às 11:00 · 3h 32min em operação
          </div>
        </div>
        <div style={{display:'flex',gap:10,alignItems:'center'}}>
          <span className="d-store-badge open">
            <span className="d-pulse-dot" />ABERTO desde 11:00
          </span>
          <button className="btn" style={{color:'var(--danger)',borderColor:'color-mix(in oklab,var(--danger) 30%,transparent)'}}>
            Fechar Caixa
          </button>
        </div>
      </div>

      {/* Atalhos rápidos */}
      <div className="d-op-actions">
        {actions.map((a,i) => (
          <button key={i} className={'d-op-btn ' + a.cls}>
            <span style={{fontSize:26}}><Icon name={a.icon} size={26} /></span>
            <span style={{fontWeight:700}}>{a.label}</span>
            <span style={{fontSize:11,opacity:.7,fontWeight:400}}>{a.desc}</span>
          </button>
        ))}
      </div>

      {/* Pedidos em aberto + resumo do caixa */}
      <div className="d-op-row">
        {/* Pedidos ao vivo */}
        <div className="d-card" style={{overflow:'hidden'}}>
          <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 20px',borderBottom:'1px solid var(--line)'}}>
            <span className="d-card-title">Pedidos em aberto</span>
            <span className="badge warn">{liveOrders.length} ativos</span>
          </div>
          <table className="d-table">
            <thead><tr>
              <th>#</th><th>Tipo</th><th>Cliente</th><th>Itens</th><th>Valor</th><th>Aguardando</th>
            </tr></thead>
            <tbody>
              {liveOrders.map(o => (
                <tr key={o.id} style={{cursor:'pointer'}}>
                  <td><span className="d-order-id">#{o.id}</span></td>
                  <td>
                    <span className={'d-type-badge ' + typeClass(o.typeName)}>
                      {typeClass(o.typeName) === 'mesa' ? '🍽' : typeClass(o.typeName) === 'delivery' ? '🛵' : '🏃'} {o.typeName}
                    </span>
                  </td>
                  <td style={{fontWeight:500,fontSize:13}}>{o.customer || '—'}</td>
                  <td><span className="d-items-clip">{o.items}</span></td>
                  <td><span style={{fontFamily:'var(--font-mono)',fontWeight:600}}>{DASH_BRL(o.value)}</span></td>
                  <td>
                    <span style={{fontFamily:'var(--font-mono)',fontSize:12,fontWeight:700,
                      color: o.wait > 20 ? 'var(--danger)' : 'var(--warn)'}}>
                      {o.wait}min
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="d-table-footer">
            <span>Mesas aguardando conta: Mesa 5, Mesa 11</span>
          </div>
        </div>

        {/* Resumo do caixa */}
        <div className="d-card" style={{overflow:'hidden'}}>
          <div style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'14px 18px',background:'var(--bg-sunken)',borderBottom:'1px solid var(--line)'}}>
            <span className="d-card-title">Resumo do caixa</span>
            <span style={{fontSize:12,color:'var(--fg-muted)',fontFamily:'var(--font-mono)'}}>Aberto às 11:00</span>
          </div>
          <div style={{padding:18,display:'flex',flexDirection:'column',gap:14}}>
            <div>
              <div style={{fontSize:10.5,color:'var(--fg-muted)',textTransform:'uppercase',letterSpacing:'0.06em',fontWeight:600,marginBottom:4}}>Vendas no caixa</div>
              <div style={{fontSize:30,fontWeight:800,fontFamily:'var(--font-mono)',letterSpacing:'-0.02em'}}>R$ 1.240,00</div>
            </div>
            <div style={{display:'flex',flexDirection:'column',gap:8}}>
              {[
                { label:'Dinheiro', icon:'cash', val:'R$ 380,00' },
                { label:'Cartão',   icon:'card', val:'R$ 710,00' },
                { label:'Pix',      icon:'pix',  val:'R$ 150,00' },
              ].map(m => (
                <div key={m.label} style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'10px 12px',borderRadius:8,background:'var(--bg-sunken)',border:'1px solid var(--line)',fontSize:13}}>
                  <span style={{display:'flex',alignItems:'center',gap:7,color:'var(--fg-muted)'}}>
                    <Icon name={m.icon} size={14} />{m.label}
                  </span>
                  <span style={{fontFamily:'var(--font-mono)',fontWeight:600}}>{m.val}</span>
                </div>
              ))}
            </div>
            <div style={{paddingTop:12,borderTop:'1px solid var(--line)',display:'flex',justifyContent:'space-between',fontSize:13,color:'var(--fg-muted)'}}>
              <span>Pedidos atendidos</span>
              <span style={{fontFamily:'var(--font-mono)',fontWeight:700,color:'var(--fg)',fontSize:16}}>18</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { OperadorView });
