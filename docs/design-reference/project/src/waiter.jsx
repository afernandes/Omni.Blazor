// Waiter (tablet) module
function WaiterView() {
  const [selected, setSelected] = React.useState(7);
  const tables = Array.from({ length: 20 }, (_, i) => ({
    num: i + 1,
    state: [3, 5, 7, 12, 14].includes(i + 1) ? 'open' : 'free',
    items: i + 1 === 7 ? 3 : i + 1 === 12 ? 5 : 0,
    total: i + 1 === 7 ? 84 : i + 1 === 12 ? 142 : 0,
  }));

  const t = tables.find(t => t.num === selected);

  return (
    <div className="waiter-frame-wrap">
      <div className="tablet">
        <div className="tablet-screen">
          <div className="tablet-side">
            <div className="tablet-head">
              <div>
                <div style={{ fontWeight: 700, fontSize: 15 }}>Salão</div>
                <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>20 mesas · 5 ocupadas</div>
              </div>
              <button className="btn btn-icon"><Icon name="bell" size={14} /></button>
            </div>
            <div className="tables-grid">
              {tables.map(tb => (
                <button key={tb.num}
                  className={'table-btn ' + (tb.state === 'open' ? 'open ' : '') + (selected === tb.num ? 'selected' : '')}
                  onClick={() => setSelected(tb.num)}>
                  <span className="tnum">{tb.num}</span>
                  <span>{tb.state === 'open' ? 'ocupada' : 'livre'}</span>
                </button>
              ))}
            </div>
          </div>
          <div className="tablet-main">
            <div className="tablet-head" style={{ borderBottom: '1px solid var(--line)' }}>
              <div>
                <div style={{ fontWeight: 700, fontSize: 16 }}>Mesa {selected}</div>
                <div style={{ fontSize: 12, color: 'var(--fg-muted)' }}>{t.state === 'open' ? `${t.items} itens · aberta há 32min` : 'Livre — toque para abrir'}</div>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn"><Icon name="user-plus" size={14} /> Cliente</button>
                <button className="btn btn-primary"><Icon name="plus" size={14} /> Item</button>
              </div>
            </div>
            <div style={{ flex: 1, overflowY: 'auto', padding: 18 }}>
              {t.state === 'open' ? (
                <>
                  <div className="pz-sum-section-label">Pedidos em andamento</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
                    {[
                      { name: 'Pizza Grande · Calabresa / Margherita', qty: 1, price: 62, notes: ['sem cebola no 2º sabor', '+ borda catupiry'] },
                      { name: 'Coca-Cola 2L', qty: 1, price: 14, notes: [] },
                      { name: 'Petit Gâteau', qty: 1, price: 22, notes: ['sorvete à parte'] },
                    ].map((it, i) => (
                      <div key={i} className="card" style={{ padding: 12 }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                          <div>
                            <div style={{ fontWeight: 600, fontSize: 14 }}>{it.qty}× {it.name}</div>
                            {it.notes.length > 0 && (
                              <ul className="cart-item-notes" style={{ margin: '4px 0 0' }}>
                                {it.notes.map((n, j) => <li key={j}>{n}</li>)}
                              </ul>
                            )}
                          </div>
                          <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(it.price)}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <div style={{ display: 'grid', placeItems: 'center', height: '100%', color: 'var(--fg-soft)' }}>
                  <div style={{ textAlign: 'center' }}>
                    <Icon name="table" size={32} />
                    <div style={{ marginTop: 8 }}>Mesa livre</div>
                    <button className="btn btn-primary" style={{ marginTop: 12 }}>Abrir comanda</button>
                  </div>
                </div>
              )}
            </div>
            {t.state === 'open' && (
              <div style={{ padding: 16, borderTop: '1px solid var(--line)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'var(--bg-elev)' }}>
                <div>
                  <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--fg-soft)', fontWeight: 600 }}>Total</div>
                  <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, fontSize: 22 }}>{BRL(t.total + 14)}</div>
                </div>
                <div style={{ display: 'flex', gap: 8 }}>
                  <button className="btn"><Icon name="print" size={14} /> Dividir conta</button>
                  <button className="btn btn-primary btn-lg">Fechar mesa</button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

window.WaiterView = WaiterView;
