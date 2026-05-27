/* Pizza screens — components for each screen.
 * Exposes window.PizzaScreens = { Home, Sizes, Flavors, Preview, Cart, Checkout, Tracking, Sheet }
 */
const { useState, useMemo, useEffect } = React;
const { BRL, SIZES, FLAVOR_GROUPS, FLAVORS, BORDAS, EXTRAS, CATEGORIES, DRINKS,
        SHOP_PRICE_STRATEGY, flavorPrice, combinedFlavorsPrice, priorityFlavor } = window.PizzaData;

// ——— Tiny icon set (inline SVGs) ———————————————————————————————————
const Ic = {
  back: (p) => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" {...p}><path d="M15 18l-6-6 6-6"/></svg>,
  search: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" {...p}><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>,
  share: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="M8.59 13.51l6.83 3.98M15.41 6.51l-6.82 3.98"/></svg>,
  chev: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" {...p}><path d="M9 18l6-6-6-6"/></svg>,
  chevDown: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" {...p}><path d="M6 9l6 6 6-6"/></svg>,
  x: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" {...p}><path d="M18 6L6 18M6 6l12 12"/></svg>,
  check: (p) => <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M20 6L9 17l-5-5"/></svg>,
  cart: (p) => <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="9" cy="20" r="1.5"/><circle cx="18" cy="20" r="1.5"/><path d="M2 3h3l2.5 13.5A2 2 0 0 0 9.5 18H18a2 2 0 0 0 2-1.5L22 7H6"/></svg>,
  trash: (p) => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/></svg>,
  info: (p) => <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="12" cy="12" r="9"/><path d="M12 8h.01M11 12h1v4h1"/></svg>,
  pin: (p) => <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M12 22s-8-7-8-13a8 8 0 0 1 16 0c0 6-8 13-8 13z"/><circle cx="12" cy="9" r="3"/></svg>,
  ticket: (p) => <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 8a3 3 0 0 1 3-3h12a3 3 0 0 1 3 3v2a2 2 0 0 0 0 4v2a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3v-2a2 2 0 0 0 0-4z"/><path d="M14 5v14"/></svg>,
};
const StatusIcons = () => (
  <span className="icons">
    <svg width="14" height="10" viewBox="0 0 14 10" fill="currentColor"><rect x="0" y="6" width="2" height="4" rx="0.5"/><rect x="3.5" y="4" width="2" height="6" rx="0.5"/><rect x="7" y="2" width="2" height="8" rx="0.5"/><rect x="10.5" y="0" width="2" height="10" rx="0.5"/></svg>
    <svg width="14" height="10" viewBox="0 0 14 10" fill="currentColor"><path d="M1.5 4 A 6 6 0 0 1 12.5 4 M3.5 6 A 4 4 0 0 1 10.5 6 M5.5 8 A 2 2 0 0 1 8.5 8" stroke="currentColor" strokeWidth="1" fill="none"/></svg>
    <svg width="22" height="11" viewBox="0 0 22 11" fill="none" stroke="currentColor" strokeWidth="1"><rect x="0.5" y="0.5" width="18" height="10" rx="2"/><rect x="2" y="2" width="15" height="7" rx="1" fill="currentColor"/><rect x="20" y="3.5" width="1.5" height="4" rx="0.5" fill="currentColor"/></svg>
  </span>
);

// ——— Appbar ——————————————————————————————————————————————————————————
const AppBar = ({ onBack, title, sub, right }) => (
  <div className="appbar">
    {onBack && <button className="appbar-back" onClick={onBack} aria-label="Voltar"><Ic.back/></button>}
    <div className="appbar-title">
      <h1>{title}</h1>
      {sub && <div className="sub">{sub}</div>}
    </div>
    {right || <div style={{width: 40}}/>}
  </div>
);

// ——— TELA 1 — Home ———————————————————————————————————————————————————
const Home = ({ go, cart }) => {
  const [activeCat, setActiveCat] = useState('destaques');
  const cartCount = cart.reduce((s, i) => s + i.qty, 0);
  const cartTotal = cart.reduce((s, i) => s + i.cents * i.qty, 0);

  return (
    <div className="screen">
      <div className="scroll" style={{paddingBottom: cartCount ? 80 : 12}}>
        <div className="home-cover">
          <div className="home-cover-top">
            <button className="home-back"><Ic.back/></button>
            <button className="home-share"><Ic.share/></button>
          </div>
          <div className="home-cover-bottom">
            <div className="home-logo">D</div>
            <div className="home-cover-info">
            <div className="home-name">Forneria<br/>Don Tonhão</div>
            <div className="home-cover-meta">
              <span className="open">Aberto agora</span>
              <span className="sep">·</span>
              <span>Entrega 35–50min</span>
              <span className="sep">·</span>
              <span>★ 4,8 (1.247)</span>
            </div>
            </div>
          </div>
        </div>

        <label className="home-search">
          <Ic.search/>
          <input placeholder="Buscar pizza, bebida…"/>
        </label>

        <nav className="home-cats">
          {CATEGORIES.map(c => (
            <button key={c.id} className={'home-cat ' + (activeCat === c.id ? 'on' : '')} onClick={() => setActiveCat(c.id)}>
              <span>{c.emoji}</span>{c.name}
            </button>
          ))}
        </nav>

        {/* Destaques */}
        <section className="home-section">
          <div className="label">Mais pedidas</div>
          <h2>Destaques da casa</h2>
          <p className="desc">As campeãs da semana em pedidos</p>
          <button className="feat-card" onClick={() => go({ name: 'sizes', cat: { id: 'salgadas', name: 'Pizzas Salgadas', desc: 'até 2 sabores salgados' } })}>
            <div className="th"><span className="badge">+ pedida</span></div>
            <div className="bd">
              <div className="nm">Calabresa Premium</div>
              <div className="ds">Calabresa artesanal, mussarela, cebola roxa, azeitona preta.</div>
              <div className="pr">a partir de R$ 59,00</div>
            </div>
          </button>
          <button className="feat-card" onClick={() => go({ name: 'sizes', cat: { id: 'salgadas', name: 'Pizzas Salgadas', desc: 'até 2 sabores salgados' } })}>
            <div className="th"><span className="badge">Novidade</span></div>
            <div className="bd">
              <div className="nm">3 Queijos Scala</div>
              <div className="ds">Muçarela, gorgonzola, parmesão gratinado finalizado no forno a lenha.</div>
              <div className="pr">a partir de R$ 79,00</div>
            </div>
          </button>
        </section>

        {/* Pizzas Salgadas — categoria que abre fluxo */}
        <section className="home-section">
          <h2>Pizzas Salgadas</h2>
          <p className="desc">Monte sua pizza · escolha o tamanho e até 2 sabores</p>
          <button className="itemrow" onClick={() => go({ name: 'sizes', cat: { id: 'salgadas', name: 'Pizzas Salgadas', desc: 'Monte sua pizza escolhendo o tamanho e até 2 sabores salgados.' } })}>
            <div className="itemrow-thumb"></div>
            <div className="itemrow-body">
              <div className="itemrow-name">Pizza · até 2 sabores</div>
              <div className="itemrow-desc">12 sabores salgados. Massa artesanal, fermentação 48h, forno a lenha.</div>
              <div className="itemrow-price"><span className="from">a partir de</span>R$ 39,00</div>
            </div>
          </button>
        </section>

        {/* Pizzas Doces */}
        <section className="home-section">
          <h2>Pizzas Doces</h2>
          <p className="desc">Para fechar a noite com chave de ouro</p>
          <button className="itemrow" onClick={() => go({ name: 'sizes', cat: { id: 'doces', name: 'Pizzas Doces', desc: 'Sabores doces para fechar a refeição.' } })}>
            <div className="itemrow-thumb sweet"></div>
            <div className="itemrow-body">
              <div className="itemrow-name">Pizza Doce · até 2 sabores</div>
              <div className="itemrow-desc">Brigadeiro, romeu e julieta, banana c/ canela, prestígio.</div>
              <div className="itemrow-price"><span className="from">a partir de</span>R$ 44,00</div>
            </div>
          </button>
        </section>

        {/* Bebidas */}
        <section className="home-section">
          <h2>Bebidas</h2>
          <p className="desc">Geladas, prontas para chegar com a pizza</p>
          {DRINKS.map(d => (
            <button key={d.id} className="itemrow">
              <div className="itemrow-thumb drink"></div>
              <div className="itemrow-body">
                <div className="itemrow-name">{d.name}</div>
                <div className="itemrow-desc">{d.desc}</div>
                <div className="itemrow-price">{BRL(d.cents)}</div>
              </div>
            </button>
          ))}
        </section>
      </div>

      {cartCount > 0 && (
        <div className="cart-bar" onClick={() => go({ name: 'cart' })}>
          <span className="lhs">
            <span className="count">{cartCount}</span>
            <Ic.cart/> Ver pedido
          </span>
          <span className="total">{BRL(cartTotal)}</span>
        </div>
      )}
    </div>
  );
};

// ——— TELA 2 — Sizes ——————————————————————————————————————————————————
const Sizes = ({ go, back, cat }) => {
  const [sel, setSel] = useState(null);
  return (
    <div className="screen">
      <AppBar onBack={back} title={cat.name} sub="Passo 1 de 3 · escolha o tamanho"/>
      <div className="scroll" style={{paddingBottom: 100}}>
        <div className="section-pad">
          <h2>{cat.name}</h2>
          <div className="sub">{cat.desc}</div>
        </div>

        <div className="req-row">
          <span className="label">Escolha o tamanho</span>
          <span className="req-tag">Obrigatório · 1</span>
        </div>

        <div className="size-list">
          {SIZES.map(s => (
            <button key={s.id} className={'size-card ' + (sel === s.id ? 'on' : '')} onClick={() => setSel(s.id)}>
              <div>
                <div className="nm">{s.name}</div>
                <div className="ds">{s.desc} · até {s.maxFlavors} sabores</div>
              </div>
              <div className="pr">{BRL(s.fromCents)}<small>a partir de</small></div>
              <div className="radio"></div>
            </button>
          ))}
        </div>
      </div>

      <div className="bottombar">
        <button className="btn btn-accent" disabled={!sel} onClick={() => sel && go({ name: 'flavors', cat, size: SIZES.find(s => s.id === sel) })}>
          <span>Escolher Sabores</span>
          <Ic.chev/>
        </button>
      </div>
    </div>
  );
};

// ——— TELA 3 — Flavors ————————————————————————————————————————————————
const Flavors = ({ go, back, cat, size }) => {
  const max = size.maxFlavors;
  const [selected, setSelected] = useState([]);
  const [filter, setFilter] = useState('Todos');
  const [query, setQuery] = useState('');

  const isFull = selected.length === max;

  const visible = useMemo(() => {
    return FLAVORS.filter(f => {
      if (cat.id === 'doces' && f.group !== 'Doces') return false;
      if (cat.id === 'doces' ? false : f.group === 'Doces') return false; // hide doces from salgadas
      if (filter !== 'Todos' && f.group !== filter && !(filter === 'Veganos' && f.tag === 'veg')) return false;
      if (query && !(f.name + ' ' + f.ing).toLowerCase().includes(query.toLowerCase())) return false;
      return true;
    });
  }, [filter, query, cat.id]);

  const toggle = (f) => {
    setSelected(s => {
      if (s.includes(f.id)) return s.filter(x => x !== f.id);
      if (s.length >= max) return s;
      return [...s, f.id];
    });
  };

  const previewPriceCents = combinedFlavorsPrice(selected, size.id);
  const top = priorityFlavor(selected, size.id);
  const showHighest = selected.length === 2 && SHOP_PRICE_STRATEGY === 'highest';

  return (
    <div className="screen">
      <AppBar onBack={back} title={size.name + ' · até ' + max + ' sabores'} sub={`Passo 2 de 3 · escolha pelo menos 1 e no máximo ${max}`}/>
      <div className="scroll" style={{paddingBottom: 100}}>
        <div style={{padding: '12px 14px 0'}}>
          <label className="home-search" style={{margin: 0}}>
            <Ic.search/>
            <input placeholder="Buscar sabor…" value={query} onChange={e => setQuery(e.target.value)}/>
          </label>
        </div>

        <div className="flavor-filters">
          {['Todos', 'Tradicionais', 'Especiais', 'Veganos'].map(f => (
            <button key={f} className={'flavor-filter ' + (filter === f ? 'on' : '')} onClick={() => setFilter(f)}>{f}</button>
          ))}
        </div>

        <div className={'flavors-counter ' + (isFull ? 'complete' : '')} style={{marginTop: 12}}>
          <span className="cnt">
            {isFull ? `${max} sabores selecionados — pronto para continuar!` : <><b>{selected.length}</b> de <b>{max}</b> sabores selecionados</>}
          </span>
          <span className="progress">
            {Array.from({length: max}, (_, i) => <span key={i} className={'dot ' + (i < selected.length ? 'on' : '')}/>)}
          </span>
        </div>

        <div className="flavor-list">
          {visible.map(f => {
            const on = selected.includes(f.id);
            const disabled = !on && isFull;
            return (
              <button key={f.id} className={'flavor ' + (on ? 'on ' : '') + (disabled ? 'disabled' : '')} onClick={() => toggle(f)}>
                <div className={'flavor-thumb ' + (f.tag || '')}></div>
                <div className="flavor-body">
                  <div className="flavor-name">{f.name}</div>
                  <div className="flavor-ing">{f.ing}</div>
                  <div className="flavor-price">{BRL(flavorPrice(f, size.id))}</div>
                </div>
                <div className="checkbox">{on && <Ic.check/>}</div>
              </button>
            );
          })}
        </div>
      </div>

      <div className="bottombar" style={{flexDirection: 'column', alignItems: 'stretch', gap: 8}}>
        {showHighest && (
          <div style={{fontSize: 11, color: 'var(--fg-muted)', textAlign: 'center', display: 'flex', justifyContent: 'center', gap: 6, alignItems: 'center'}}>
            <Ic.info/> Cobramos o valor do sabor mais caro: <b style={{color:'var(--fg)'}}>&nbsp;{top?.name}</b>
          </div>
        )}
        <button className="btn btn-accent" disabled={selected.length === 0} onClick={() => go({ name: 'preview', cat, size, flavors: selected })}>
          <span>Continuar</span>
          <span className="total">{BRL(previewPriceCents)}</span>
        </button>
      </div>
    </div>
  );
};

window.PizzaScreens = window.PizzaScreens || {};
Object.assign(window.PizzaScreens, { Home, Sizes, Flavors, AppBar, Ic, StatusIcons });
