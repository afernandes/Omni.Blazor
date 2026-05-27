/* Pizza screens — part 3: Cart + Checkout + Tracking + App root
 */
const { useState: useStateA, useMemo: useMemoA, Fragment: FragmentA } = React;
const { Ic: IcA, AppBar: AppBarA, StatusIcons: StatusIconsA, Home, Sizes, Flavors, Preview } = window.PizzaScreens;
const PDA = window.PizzaData;

// ——— Cart ——————————————————————————————————————————————————
const Cart = ({ go, back, cart, setCart }) => {
  const [coupon, setCoupon] = useStateA('');
  const [mode, setMode] = useStateA('delivery');
  const [mesa, setMesa] = useStateA('07');

  const subtotal = cart.reduce((s, i) => s + i.cents * i.qty, 0);
  const delivery = mode === 'delivery' ? 800 : 0;
  const discount = coupon === 'PIZZA10' ? Math.round(subtotal * 0.1) : 0;
  const total = subtotal + delivery - discount;

  const updateQty = (id, d) => {
    setCart(cart.map(i => i.id === id ? { ...i, qty: Math.max(1, i.qty + d) } : i));
  };
  const remove = (id) => setCart(cart.filter(i => i.id !== id));

  const summarizeFlavors = (item) => {
    if (item.kind !== 'pizza') return '';
    return item.flavors.map(f => f.name).join(' + ');
  };
  const modList = (item) => {
    if (item.kind !== 'pizza') return [];
    const out = [];
    if (item.borda && item.borda.cents > 0) out.push('Borda ' + item.borda.name.replace('Recheada com ', ''));
    Object.entries(item.custom || {}).forEach(([fId, c]) => {
      if (c.extras && c.extras.size) {
        const f = PDA.FLAVORS.find(f => f.id === fId);
        Array.from(c.extras).forEach(exId => {
          const ex = PDA.EXTRAS.find(e => e.id === exId);
          if (ex) out.push(`Extra ${ex.name} na ${f.name}`);
        });
      }
    });
    return out;
  };

  return (
    <div className="screen">
      <AppBarA onBack={back} title="Seu pedido" sub={`${cart.length} item${cart.length > 1 ? 's' : ''}`}/>
      <div className="scroll" style={{paddingBottom: 110}}>
        <div className="cart-list">
          {cart.length === 0 && (
            <div style={{padding: 40, textAlign: 'center', color: 'var(--fg-muted)'}}>
              Seu carrinho está vazio. <button onClick={back} style={{color: 'var(--accent)', fontWeight: 600}}>Voltar ao cardápio</button>
            </div>
          )}
          {cart.map(item => (
            <div key={item.id} className="cart-item">
              <div className="cart-item-top">
                <div style={{flex: 1}}>
                  <div className="cart-item-name">🍕 {item.name} · {item.size?.desc?.split('·')[0]?.trim()}</div>
                  <div className="cart-item-meta"><b>{summarizeFlavors(item)}</b></div>
                  {modList(item).length > 0 && (
                    <div className="cart-item-meta">{modList(item).join(' · ')}</div>
                  )}
                  {item.obs && <div className="cart-item-obs">Obs: {item.obs}</div>}
                </div>
                <button className="cart-trash" onClick={() => remove(item.id)}><IcA.trash/></button>
              </div>
              <div className="cart-item-foot">
                <div className="qty">
                  <button onClick={() => updateQty(item.id, -1)} disabled={item.qty <= 1}>−</button>
                  <span className="v">{item.qty}</span>
                  <button onClick={() => updateQty(item.id, +1)}>+</button>
                </div>
                <div className="cart-item-price">{PDA.BRL(item.cents * item.qty)}</div>
              </div>
            </div>
          ))}
        </div>

        {cart.length > 0 && (
          <FragmentA>
            {/* Coupon */}
            <div className="coupon">
              <div className="coupon-ic">🎟️</div>
              <div className="coupon-body">
                <div className="t">{coupon === 'PIZZA10' ? '10% OFF aplicado' : 'Adicionar cupom'}</div>
                <input
                  style={{border: 0, background: 'none', outline: 'none', font: 'inherit', fontSize: 12, color: 'var(--fg-muted)', padding: 0, marginTop: 2, width: '100%'}}
                  placeholder="Tente PIZZA10"
                  value={coupon}
                  onChange={e => setCoupon(e.target.value.toUpperCase())}
                />
              </div>
            </div>

            {/* Modes */}
            <div className="req-row" style={{paddingBottom: 0}}>
              <span className="label">Tipo de pedido</span>
            </div>
            <div className="delivery-modes">
              <button className={'dmode ' + (mode === 'delivery' ? 'on' : '')} onClick={() => setMode('delivery')}>
                <span className="ic">🛵</span>Delivery
              </button>
              <button className={'dmode ' + (mode === 'retirada' ? 'on' : '')} onClick={() => setMode('retirada')}>
                <span className="ic">🏃</span>Retirada
              </button>
              <button className={'dmode ' + (mode === 'mesa' ? 'on' : '')} onClick={() => setMode('mesa')}>
                <span className="ic">🍽️</span>Mesa
              </button>
            </div>
            {mode === 'mesa' && (
              <div style={{margin: '8px 14px'}}>
                <label style={{fontSize: 12, color: 'var(--fg-muted)'}}>Número da mesa</label>
                <input value={mesa} onChange={e => setMesa(e.target.value)}
                  style={{display: 'block', marginTop: 4, width: '100%', padding: '10px 12px', border: '1px solid var(--line)', borderRadius: 8, background: 'var(--bg-elev)', font: 'inherit'}}
                  placeholder="Ex: 07"/>
              </div>
            )}

            {/* Totals */}
            <div className="totals">
              <div className="row"><span>Subtotal</span><span className="v">{PDA.BRL(subtotal)}</span></div>
              {delivery > 0 && <div className="row"><span>Taxa de entrega</span><span className="v">{PDA.BRL(delivery)}</span></div>}
              {discount > 0 && <div className="row"><span>Desconto (PIZZA10)</span><span className="v discount">− {PDA.BRL(discount)}</span></div>}
              <div className="total"><span>Total</span><span className="v">{PDA.BRL(total)}</span></div>
            </div>
          </FragmentA>
        )}
      </div>

      {cart.length > 0 && (
        <div className="bottombar">
          <button className="btn btn-accent" onClick={() => go({ name: 'checkout', mode, mesa, total, subtotal, delivery, discount })}>
            <span>Ir para pagamento</span>
            <span className="total">{PDA.BRL(total)}</span>
          </button>
        </div>
      )}
    </div>
  );
};

// ——— Checkout ———————————————————————————————————————————————
const Checkout = ({ go, back, cart, ctx }) => {
  const [pay, setPay] = useStateA('pix');
  const [troco, setTroco] = useStateA('');

  return (
    <div className="screen">
      <AppBarA onBack={back} title="Pagamento" sub={`Total ${PDA.BRL(ctx.total)}`}/>
      <div className="scroll" style={{paddingBottom: 110}}>
        {/* Resumo colapsável */}
        <div className="collapse">
          <div className="collapse-head">
            <span className="t">Resumo do pedido · {cart.length} item{cart.length > 1 ? 's' : ''}</span>
            <span className="v">{PDA.BRL(ctx.subtotal)} <span className="chev"><IcA.chevDown/></span></span>
          </div>
        </div>

        {/* Endereço ou mesa */}
        <div className="req-row" style={{paddingBottom: 0}}>
          <span className="label">{ctx.mode === 'mesa' ? 'Mesa' : ctx.mode === 'retirada' ? 'Retirar em' : 'Endereço de entrega'}</span>
        </div>
        <div className="address-card">
          <div className="address-ic">{ctx.mode === 'mesa' ? '🍽️' : ctx.mode === 'retirada' ? '🏪' : <IcA.pin/>}</div>
          <div className="address-body">
            {ctx.mode === 'mesa' ? (
              <FragmentA>
                <div className="t">Mesa {ctx.mesa}</div>
                <div className="s">Salão Principal · pedido cai direto na cozinha.</div>
              </FragmentA>
            ) : ctx.mode === 'retirada' ? (
              <FragmentA>
                <div className="t">Forneria Don Tonhão</div>
                <div className="s">R. Aspicuelta, 582 — Vila Madalena · pronto em 25–35min</div>
                <span className="change">Como chegar</span>
              </FragmentA>
            ) : (
              <FragmentA>
                <div className="t">Casa</div>
                <div className="s">R. Harmonia, 142, apto 41 — Vila Madalena, São Paulo</div>
                <span className="change">Trocar endereço</span>
              </FragmentA>
            )}
          </div>
        </div>

        {/* Pagamento */}
        <div className="req-row" style={{paddingBottom: 0}}>
          <span className="label">Forma de pagamento</span>
          <span className="req-tag">Obrigatório</span>
        </div>
        <div className="pay-list">
          <button className={'pay ' + (pay === 'pix' ? 'on' : '')} onClick={() => setPay('pix')}>
            <div className="pay-ic">📱</div>
            <div className="pay-body">
              <div className="t">PIX</div>
              <div className="s">5% OFF · pagamento na hora do pedido</div>
            </div>
            <div className="radio">{pay === 'pix' && <span style={{width: 10, height: 10, background: 'var(--accent)', borderRadius: '50%'}}/>}</div>
          </button>
          <button className={'pay ' + (pay === 'card' ? 'on' : '')} onClick={() => setPay('card')}>
            <div className="pay-ic">💳</div>
            <div className="pay-body">
              <div className="t">Cartão {ctx.mode === 'delivery' ? 'na entrega' : 'na maquininha'}</div>
              <div className="s">Crédito ou débito · todas as bandeiras</div>
            </div>
            <div className="radio">{pay === 'card' && <span style={{width: 10, height: 10, background: 'var(--accent)', borderRadius: '50%'}}/>}</div>
          </button>
          <button className={'pay ' + (pay === 'cash' ? 'on' : '')} onClick={() => setPay('cash')}>
            <div className="pay-ic">💵</div>
            <div className="pay-body">
              <div className="t">Dinheiro</div>
              <div className="s">Informe o troco abaixo se precisar</div>
            </div>
            <div className="radio">{pay === 'cash' && <span style={{width: 10, height: 10, background: 'var(--accent)', borderRadius: '50%'}}/>}</div>
          </button>
        </div>
        {pay === 'cash' && (
          <div style={{margin: '8px 14px'}}>
            <label style={{fontSize: 12, color: 'var(--fg-muted)'}}>Troco para</label>
            <input value={troco} onChange={e => setTroco(e.target.value)}
              style={{display: 'block', marginTop: 4, width: '100%', padding: '10px 12px', border: '1px solid var(--line)', borderRadius: 8, background: 'var(--bg-elev)', font: 'inherit'}}
              placeholder="Ex: R$ 100,00 (deixe em branco se exato)"/>
          </div>
        )}

        {/* Resumo de valores */}
        <div className="totals" style={{marginTop: 14}}>
          <div className="row"><span>Subtotal</span><span className="v">{PDA.BRL(ctx.subtotal)}</span></div>
          {ctx.delivery > 0 && <div className="row"><span>Entrega</span><span className="v">{PDA.BRL(ctx.delivery)}</span></div>}
          {ctx.discount > 0 && <div className="row"><span>Desconto</span><span className="v discount">− {PDA.BRL(ctx.discount)}</span></div>}
          <div className="total"><span>Total</span><span className="v">{PDA.BRL(ctx.total)}</span></div>
        </div>
      </div>

      <div className="bottombar">
        <button className="btn btn-accent" onClick={() => go({ name: 'tracking', ctx })}>
          <span>Confirmar pedido</span>
          <span className="total">{PDA.BRL(ctx.total)}</span>
        </button>
      </div>
    </div>
  );
};

// ——— Tracking ———————————————————————————————————————————————
const Tracking = ({ go, ctx }) => {
  const num = '#4721';
  const eta = ctx.mode === 'mesa' ? 'em 12min' : ctx.mode === 'retirada' ? 'em 25min' : '~40 minutos';
  const steps = [
    { id: 'r', t: 'Pedido recebido', s: 'Confirmamos seu pedido às 21:42', icon: '📥', state: 'done' },
    { id: 'p', t: 'Em preparo', s: 'Massa esticada, indo pro forno a lenha', icon: '🔥', state: 'current' },
    { id: 's', t: ctx.mode === 'delivery' ? 'Saiu para entrega' : ctx.mode === 'retirada' ? 'Pronto para retirada' : 'A caminho da mesa', s: 'Em breve', icon: ctx.mode === 'delivery' ? '🛵' : '✅', state: '' },
    { id: 'd', t: ctx.mode === 'delivery' ? 'Entregue' : 'Servido', s: '—', icon: '🏠', state: '' },
  ];
  return (
    <div className="screen">
      <AppBarA onBack={() => go({ name: 'home' })} title="Acompanhe seu pedido" sub={'Pedido ' + num}/>
      <div className="scroll" style={{paddingBottom: 110}}>
        <div className="track-hero">
          <div className="num">CHEGADA ESTIMADA</div>
          <div className="eta">{eta}</div>
          <div className="sub">Pedido {num} · Forneria Don Tonhão</div>
        </div>

        <div className="timeline">
          {steps.map(s => (
            <div key={s.id} className={'timeline-step ' + s.state}>
              <div className="timeline-icon">{s.icon}</div>
              <div className="timeline-body">
                <div className="t">{s.t}</div>
                <div className="s">{s.s}</div>
              </div>
            </div>
          ))}
        </div>

        <div style={{padding: '14px 14px 8px', fontSize: 12, color: 'var(--fg-muted)'}}>Resumo do pedido</div>
        <div className="totals" style={{margin: '0 14px 14px'}}>
          <div className="row"><span>Total pago</span><span className="v" style={{fontWeight: 700}}>{PDA.BRL(ctx.total)}</span></div>
          <div className="row"><span>Forma de pagamento</span><span className="v" style={{fontFamily: 'var(--font-ui)'}}>PIX</span></div>
          <div className="row"><span>{ctx.mode === 'delivery' ? 'Entregar em' : ctx.mode === 'retirada' ? 'Retirar em' : 'Mesa'}</span>
            <span className="v" style={{fontFamily: 'var(--font-ui)'}}>{ctx.mode === 'mesa' ? 'Mesa ' + ctx.mesa : 'Vila Madalena'}</span>
          </div>
        </div>
      </div>

      <div className="bottombar">
        <button className="btn" onClick={() => go({ name: 'home', resetCart: true })} style={{flex: 1}}>
          Novo pedido
        </button>
        <button className="btn btn-accent" style={{flex: 1, background: 'var(--good)'}} onClick={() => alert('Abrindo WhatsApp…')}>
          WhatsApp
        </button>
      </div>
    </div>
  );
};

// ——— App root ———————————————————————————————————————————————
const App = () => {
  const [stack, setStack] = useStateA([{ name: 'home' }]);
  const [cart, setCart] = useStateA([]);
  const cur = stack[stack.length - 1];

  const go = (next) => {
    if (next.name === 'home' && next.resetCart) setCart([]);
    setStack(prev => {
      // tracking + new pedido resets back to root
      if (next.name === 'home') return [{ name: 'home' }];
      return [...prev, next];
    });
  };
  const back = () => setStack(prev => prev.length > 1 ? prev.slice(0, -1) : prev);

  const addToCart = (item) => setCart(c => [...c, item]);

  const screen = () => {
    switch (cur.name) {
      case 'home':     return <Home go={go} cart={cart}/>;
      case 'sizes':    return <Sizes go={go} back={back} cat={cur.cat}/>;
      case 'flavors':  return <Flavors go={go} back={back} cat={cur.cat} size={cur.size}/>;
      case 'preview':  return <Preview go={go} back={back} addToCart={addToCart} cat={cur.cat} size={cur.size} flavors={cur.flavors}/>;
      case 'cart':     return <Cart go={go} back={back} cart={cart} setCart={setCart}/>;
      case 'checkout': return <Checkout go={go} back={back} cart={cart} ctx={cur}/>;
      case 'tracking': return <Tracking go={go} ctx={cur.ctx}/>;
      default:         return <Home go={go} cart={cart}/>;
    }
  };

  return (
    <div className="frame">
      <div className="statusbar">
        <span>21:42</span>
        <StatusIconsA/>
      </div>
      {screen()}
    </div>
  );
};

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(<App/>);
