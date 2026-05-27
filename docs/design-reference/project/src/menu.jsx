// Customer-facing Digital Menu (phone frame)
function DigitalMenu() {
  const [mode, setMode] = React.useState('delivery');
  const [cat, setCat] = React.useState('pizza');
  const [cartCount, setCartCount] = React.useState(2);
  const [cartTotal, setCartTotal] = React.useState(66.50);

  const SECTIONS = [
    { id: 'pizza', name: 'Pizzas' },
    { id: 'burger', name: 'Burgers' },
    { id: 'drink', name: 'Bebidas' },
    { id: 'dessert', name: 'Sobremesas' },
    { id: 'entry', name: 'Entradas' },
  ];

  const items = PRODUCTS.filter(p => p.cat === cat);

  const add = (p) => {
    setCartCount(c => c + 1);
    setCartTotal(t => t + p.price);
  };

  // Fixed QR pattern (just decorative)
  const qrCells = React.useMemo(() => {
    const seed = 31;
    const cells = [];
    for (let i = 0; i < 21 * 21; i++) {
      cells.push(((i * seed + (i % 7) * 13) % 17) < 8 ? 1 : 0);
    }
    // finder patterns (3 corners)
    const setSquare = (cx, cy, size, val) => {
      for (let y = cy; y < cy + size; y++) {
        for (let x = cx; x < cx + size; x++) {
          cells[y * 21 + x] = val;
        }
      }
    };
    const finder = (cx, cy) => {
      setSquare(cx, cy, 7, 1);
      setSquare(cx + 1, cy + 1, 5, 0);
      setSquare(cx + 2, cy + 2, 3, 1);
    };
    finder(0, 0); finder(14, 0); finder(0, 14);
    return cells;
  }, []);

  return (
    <div style={{ display: 'flex', padding: '24px', flex: 1, overflow: 'auto' }} className="dm-frame-wrap">
      <div className="dm-phone">
        <div className="dm-notch"></div>
        <div className="dm-phone-screen">
          <div className="dm-status">
            <span>09:42</span>
            <span style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              <Icon name="wifi" size={13} />
              <Icon name="battery" size={13} />
            </span>
          </div>

          <div className="dm-hero">
            <h1>Forno do Bairro</h1>
            <div className="meta">
              <b>● Aberto</b>
              <span>· 30–45 min</span>
              <span>· 4.8★</span>
            </div>
            <div className="dm-mode-tabs">
              {[
                { id: 'delivery', label: 'Delivery', icon: 'truck' },
                { id: 'retirada', label: 'Retirar',  icon: 'bag' },
                { id: 'mesa',     label: 'Mesa',     icon: 'table' },
              ].map(m => (
                <button key={m.id}
                  className={'dm-mode-tab ' + (mode === m.id ? 'active' : '')}
                  onClick={() => setMode(m.id)}>
                  <Icon name={m.icon} size={13} />
                  {m.label}
                </button>
              ))}
            </div>
          </div>

          <div className="dm-cats">
            {SECTIONS.map(s => (
              <button key={s.id}
                className={'chip ' + (cat === s.id ? 'active' : '')}
                onClick={() => setCat(s.id)}
                style={{ flexShrink: 0 }}>
                {s.name}
              </button>
            ))}
          </div>

          <div className="dm-list">
            <div className="dm-section-title">{SECTIONS.find(s => s.id === cat).name}</div>
            {items.map(p => (
              <div key={p.id} className="dm-item" onClick={() => add(p)}>
                <div className="dm-item-thumb"></div>
                <div className="dm-item-body">
                  <div className="dm-item-name">{p.name}</div>
                  <div className="dm-item-desc">
                    {p.cat === 'pizza' ? 'Massa artesanal, molho de tomate italiano. Escolha até ' + (p.size === 'GG' ? '4' : '2') + ' sabores.' :
                     p.cat === 'burger' ? 'Blend 180g, pão brioche, acompanha fritas crocantes.' :
                     p.cat === 'drink' ? 'Gelada, servida com canudinho biodegradável.' :
                     'Preparado na hora com ingredientes selecionados.'}
                  </div>
                  <div className="dm-item-price">{BRL(p.price)}</div>
                </div>
              </div>
            ))}
          </div>

          {cartCount > 0 && (
            <div className="dm-cart-bar">
              <span style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                <span className="count">{cartCount}</span>
                Ver sacola
              </span>
              <span style={{ fontFamily: 'var(--font-mono)' }}>{BRL(cartTotal)}</span>
            </div>
          )}
        </div>
      </div>

      <div className="dm-qr-panel">
        <div>
          <div style={{ fontSize: 11, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--fg-soft)', fontWeight: 600 }}>Link do cardápio</div>
          <div style={{ fontFamily: 'var(--font-mono)', fontSize: 13, marginTop: 4, wordBreak: 'break-all' }}>
            pedidos.fornodobairro.com.br
          </div>
        </div>
        <div className="dm-qr">
          {qrCells.map((v, i) => <div key={i} className={v ? '' : 'off'} />)}
        </div>
        <div style={{ fontSize: 12, color: 'var(--fg-muted)', textAlign: 'center' }}>
          QR fixo na mesa ou no balcão
        </div>
        <div className="card" style={{ padding: 12, background: 'var(--bg-sunken)' }}>
          <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--fg-soft)', fontWeight: 600, marginBottom: 6 }}>Taxa de entrega</div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, padding: '3px 0' }}>
            <span>Centro</span><span style={{ fontFamily: 'var(--font-mono)' }}>R$ 5,00</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, padding: '3px 0' }}>
            <span>Vila Madalena</span><span style={{ fontFamily: 'var(--font-mono)' }}>R$ 8,00</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, padding: '3px 0' }}>
            <span>Pinheiros</span><span style={{ fontFamily: 'var(--font-mono)' }}>R$ 9,00</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, padding: '3px 0', color: 'var(--fg-muted)' }}>
            <span>Outras · Google Maps</span><span style={{ fontFamily: 'var(--font-mono)' }}>dinâmico</span>
          </div>
        </div>
      </div>
    </div>
  );
}

window.DigitalMenu = DigitalMenu;
