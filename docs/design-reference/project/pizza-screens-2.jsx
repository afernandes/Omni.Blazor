/* Pizza screens — part 2: Preview + Sheet + Cart + Checkout + Tracking
 */
const { useState: useState2, useMemo: useMemo2, useEffect: useEffect2, Fragment } = React;
const { Ic, AppBar } = window.PizzaScreens;
const PD = window.PizzaData;

// ——— TELA 4 — Preview ———————————————————————————————————————————————
const Preview = ({ go, back, addToCart, cat, size, flavors }) => {
  // per-flavor customization: { [flavorId]: { removed:Set, extras:Set, note } }
  const [custom, setCustom] = useState2({});
  const [borda, setBorda] = useState2(null);
  const [qty, setQty] = useState2(1);
  const [pizzaObs, setPizzaObs] = useState2('');
  const [sheetFor, setSheetFor] = useState2(null); // flavor obj while sheet open

  const flavorObjs = flavors.map(id => PD.FLAVORS.find(f => f.id === id));
  const isHighest = PD.SHOP_PRICE_STRATEGY === 'highest';
  const flavorsCents = PD.combinedFlavorsPrice(flavors, size.id);
  const top = PD.priorityFlavor(flavors, size.id);

  const bordaObj = PD.BORDAS.find(b => b.id === borda);
  const bordaCents = bordaObj ? bordaObj.cents : 0;

  const extrasCents = Object.values(custom).reduce((sum, c) => {
    if (!c?.extras) return sum;
    return sum + Array.from(c.extras).reduce((s, exId) => {
      const ex = PD.EXTRAS.find(e => e.id === exId);
      return s + (ex ? ex.cents : 0);
    }, 0);
  }, 0);

  const unitCents = flavorsCents + bordaCents + extrasCents;
  const totalCents = unitCents * qty;

  const handleAdd = () => {
    const item = {
      id: 'p_' + Date.now(),
      kind: 'pizza',
      name: 'Pizza ' + size.name.replace('Pizza ', ''),
      size,
      flavors: flavorObjs,
      borda: bordaObj,
      custom,
      obs: pizzaObs,
      cents: unitCents,
      qty,
    };
    addToCart(item);
    go({ name: 'cart' });
  };

  const saveSheet = (flavorId, data) => {
    setCustom(prev => ({ ...prev, [flavorId]: data }));
  };

  return (
    <div className="screen">
      <AppBar onBack={back} title="Revise sua pizza" sub={`${size.name} · ${flavors.length} sabor${flavors.length > 1 ? 'es' : ''}`}/>
      <div className="scroll" style={{paddingBottom: 100}}>
        {/* Bloco 1: Sabores */}
        <div className="req-row">
          <span className="label">Seus sabores</span>
          <span className="help">toque para personalizar</span>
        </div>
        <div style={{padding: '4px 14px 8px'}}>
          {flavorObjs.map(f => {
            const c = custom[f.id];
            const customized = c && ((c.extras && c.extras.size) || (c.removed && c.removed.size) || c.note);
            const fracLabel = flavors.length === 2 ? '½' : 'inteira';
            return (
              <button key={f.id} className={'flavor-card ' + (customized ? 'customized' : '')} onClick={() => setSheetFor(f)}>
                <div className="flavor-card-top">
                  <div className={'flavor-card-thumb ' + (f.tag || '')}></div>
                  <div>
                    <div>
                      <span className="flavor-card-frac">{fracLabel}</span>
                      <span className="flavor-card-name">{f.name}</span>
                    </div>
                    <div className="flavor-card-ing">{f.ing}</div>
                    <div className="flavor-card-price">{PD.BRL(PD.flavorPrice(f, size.id))}</div>
                  </div>
                  <span className="flavor-card-chev"><Ic.chev/></span>
                </div>
                {customized ? (
                  <Fragment>
                    <div className="flavor-card-mods">
                      {Array.from(c.extras || []).map(exId => {
                        const ex = PD.EXTRAS.find(e => e.id === exId);
                        return ex && <span key={'a'+exId} className="flavor-card-mod add">+ {ex.name}</span>;
                      })}
                      {Array.from(c.removed || []).map((ing, i) => (
                        <span key={'r'+i} className="flavor-card-mod rem">− {ing}</span>
                      ))}
                    </div>
                    <div className="flavor-card-hint">✏️ Personalizado — toque para editar</div>
                  </Fragment>
                ) : (
                  <div className="flavor-card-hint">👆 Toque aqui para personalizar este sabor</div>
                )}
              </button>
            );
          })}
        </div>

        {/* Bloco 2: Borda */}
        <div className="req-row">
          <span className="label">Escolha a borda</span>
          <span className="req-tag">Obrigatório · 1</span>
        </div>
        <div className="borda-list">
          {PD.BORDAS.map(b => (
            <button key={b.id} className={'borda ' + (borda === b.id ? 'on' : '')} onClick={() => setBorda(b.id)}>
              <div className="radio">{borda === b.id && <span style={{width: 10, height: 10, background: 'var(--accent)', borderRadius: '50%'}}/>}</div>
              <div className="nm">{b.name}</div>
              <div className={'pr ' + (b.cents === 0 ? 'free' : '')}>{b.cents === 0 ? 'incluso' : '+ ' + PD.BRL(b.cents)}</div>
            </button>
          ))}
        </div>

        {/* Bloco 3: Resumo */}
        <div className="req-row" style={{marginTop: 8}}>
          <span className="label">Resumo</span>
        </div>
        <div className="price-summary">
          <div className="row">
            <span>{size.name} ({flavors.length} sabor{flavors.length > 1 ? 'es' : ''})
              {isHighest && flavors.length === 2 && <div className="note">sabor mais caro: {top?.name}</div>}
            </span>
            <span className="v">{PD.BRL(flavorsCents)}</span>
          </div>
          {bordaObj && bordaObj.cents > 0 && (
            <div className="row">
              <span>{bordaObj.name}</span>
              <span className="v add">+ {PD.BRL(bordaObj.cents)}</span>
            </div>
          )}
          {Object.entries(custom).map(([fId, c]) => {
            if (!c.extras || !c.extras.size) return null;
            const f = PD.FLAVORS.find(f => f.id === fId);
            return Array.from(c.extras).map(exId => {
              const ex = PD.EXTRAS.find(e => e.id === exId);
              if (!ex || ex.cents === 0) return null;
              return (
                <div key={fId + exId} className="row">
                  <span>Extra {ex.name} (½ {f.name})</span>
                  <span className="v add">+ {PD.BRL(ex.cents)}</span>
                </div>
              );
            });
          })}
          <div className="total">
            <span>Total desta pizza</span>
            <span className="v">{PD.BRL(unitCents)}</span>
          </div>
          {isHighest && flavors.length === 2 && (
            <div className="info"><Ic.info/> Cobramos o valor do sabor mais caro entre os selecionados.</div>
          )}
        </div>

        <div className="obs">
          <label htmlFor="pobs">Observação geral da pizza (opcional)</label>
          <textarea id="pobs" placeholder="Ex: bem assada, sem orégano em toda a pizza…"
                    value={pizzaObs} onChange={e => setPizzaObs(e.target.value)}/>
        </div>
      </div>

      <div className="bottombar">
        <div className="qty">
          <button onClick={() => setQty(q => Math.max(1, q-1))} disabled={qty <= 1}>−</button>
          <span className="v">{qty}</span>
          <button onClick={() => setQty(q => q+1)}>+</button>
        </div>
        <button className="btn btn-accent" disabled={!borda} onClick={handleAdd}>
          <span>Adicionar</span>
          <span className="total">{PD.BRL(totalCents)}</span>
        </button>
      </div>

      {sheetFor && (
        <CustomizeSheet
          flavor={sheetFor}
          initial={custom[sheetFor.id]}
          fracLabel={flavors.length === 2 ? '½' : ''}
          onClose={() => setSheetFor(null)}
          onSave={(data) => { saveSheet(sheetFor.id, data); setSheetFor(null); }}
        />
      )}
    </div>
  );
};

// ——— Bottom Sheet ——————————————————————————————————————————————————
const CustomizeSheet = ({ flavor, initial, fracLabel, onClose, onSave }) => {
  const initialIngs = flavor.ing.split(',').map(s => s.trim());
  const [removed, setRemoved] = useState2(new Set(initial?.removed || []));
  const [extras, setExtras] = useState2(new Set(initial?.extras || []));
  const [note, setNote] = useState2(initial?.note || '');

  const toggleIng = (ing) => {
    setRemoved(prev => {
      const next = new Set(prev);
      if (next.has(ing)) next.delete(ing); else next.add(ing);
      return next;
    });
  };
  const toggleExtra = (exId) => {
    setExtras(prev => {
      const next = new Set(prev);
      if (next.has(exId)) next.delete(exId); else next.add(exId);
      return next;
    });
  };

  return (
    <Fragment>
      <div className="sheet-overlay" onClick={onClose}/>
      <div className="sheet">
        <div className="sheet-grab"/>
        <div className="sheet-head">
          <h2>Personalizando {fracLabel} {flavor.name}</h2>
          <button className="sheet-close" onClick={onClose}><Ic.x/></button>
        </div>
        <div className="sheet-body">
          <div className="sheet-section">
            <h3>Remover ingredientes <small>sem custo</small></h3>
            <div className="ingredient-toggles">
              {initialIngs.map(ing => {
                const off = removed.has(ing);
                return (
                  <button key={ing} className={'ing ' + (off ? 'off' : '')} onClick={() => toggleIng(ing)}>
                    <span className="v">{!off && <Ic.check/>}</span>
                    {ing}
                  </button>
                );
              })}
            </div>
          </div>

          <div className="sheet-section">
            <h3>Adicionar extras <small>turbine seu sabor</small></h3>
            <div className="extras-list">
              {PD.EXTRAS.map(ex => {
                const on = extras.has(ex.id);
                return (
                  <button key={ex.id} className={'extra ' + (on ? 'on' : '')} onClick={() => toggleExtra(ex.id)}>
                    <div className="extra-thumb"/>
                    <div className="extra-name">{ex.name}</div>
                    <div className={'extra-pr ' + (ex.cents === 0 ? 'free' : '')}>{ex.cents === 0 ? 'Grátis' : '+ ' + PD.BRL(ex.cents)}</div>
                    <span className="extra-btn">{on ? '✓' : '+'}</span>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="sheet-section">
            <h3>Observação <small>para este sabor</small></h3>
            <textarea
              style={{width: '100%', minHeight: 60, padding: '10px 12px', border: '1px solid var(--line)', borderRadius: 8, background: 'var(--bg-elev)', font: 'inherit', fontSize: 13, outline: 'none'}}
              placeholder="Ex: bem temperado, capricha no recheio"
              value={note} onChange={e => setNote(e.target.value)}
            />
          </div>
        </div>
        <div className="sheet-foot">
          <button className="btn btn-accent" onClick={() => onSave({ removed, extras, note })}>
            <span>Salvar personalização</span>
          </button>
        </div>
      </div>
    </Fragment>
  );
};

window.PizzaScreens = window.PizzaScreens || {};
Object.assign(window.PizzaScreens, { Preview, CustomizeSheet });
