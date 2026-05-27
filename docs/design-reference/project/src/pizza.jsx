// Pizza configurator — multi-step, inline (not a modal)
const { useState: usePzState, useMemo: usePzMemo } = React;

function PizzaFlow({ product, onCancel, onConfirm }) {
  const size = PIZZA_SIZES.find(s => s.size === product.size) || PIZZA_SIZES[1];

  const [step, setStep] = usePzState(1);
  const [partition, setPartition] = usePzState(1); // 1, 2 or size.maxFlavors
  const [slots, setSlots] = usePzState(Array(size.maxFlavors).fill(null));
  const [activeSlot, setActiveSlot] = usePzState(0);
  const [ingredients, setIngredients] = usePzState(() =>
    INGREDIENTS.reduce((acc, ing) => { acc[ing.id] = ing.default ? 'in' : 'out'; return acc; }, {})
  );
  const [notes, setNotes] = usePzState('');
  const [flavorQuery, setFlavorQuery] = usePzState('');

  const effSlots = slots.slice(0, partition);

  const basePrice = usePzMemo(() => {
    // price rule = max of selected flavors
    let best = 0;
    for (const sid of effSlots) {
      if (sid) {
        const f = FLAVORS.find(f => f.id === sid);
        if (f && f.prices[size.size] > best) best = f.prices[size.size];
      }
    }
    return best || product.price;
  }, [slots, partition, size.size, product.price]);

  const extras = usePzMemo(() => {
    let sum = 0;
    for (const [id, st] of Object.entries(ingredients)) {
      const ing = INGREDIENTS.find(i => i.id === id);
      if (!ing) continue;
      if (st === 'in' && !ing.default) sum += ing.price;
    }
    return sum;
  }, [ingredients]);

  const total = basePrice + extras;

  const allFilled = effSlots.every(s => s != null);
  const canAdvance = step === 1 || (step === 2 && allFilled) || step === 3 || step === 4;

  const pickFlavor = (fid) => {
    setSlots(prev => {
      const next = [...prev];
      next[activeSlot] = fid;
      return next;
    });
    // auto-advance to next empty slot
    const nextEmpty = effSlots.findIndex((s, i) => i !== activeSlot && s == null);
    if (nextEmpty >= 0) setActiveSlot(nextEmpty);
  };

  const confirm = () => {
    const flavorNames = effSlots.map(sid => FLAVORS.find(f => f.id === sid)?.name).filter(Boolean);
    const ingNotes = [];
    for (const [id, st] of Object.entries(ingredients)) {
      const ing = INGREDIENTS.find(i => i.id === id);
      if (!ing) continue;
      if (st === 'in' && !ing.default) ingNotes.push('+ ' + ing.name);
      if (st === 'out' && ing.default) ingNotes.push('− sem ' + ing.name.toLowerCase());
    }
    const notesArr = [
      flavorNames.length > 1 ? flavorNames.join(' / ') : flavorNames[0],
      ...ingNotes,
      notes ? 'obs: ' + notes : null,
    ].filter(Boolean);
    onConfirm({
      id: 'pz-' + Date.now(),
      pid: product.id,
      name: `Pizza ${size.name} · ${flavorNames.join(' / ')}`,
      price: total,
      qty: 1,
      notes: notesArr.slice(1), // first is already in name
    });
  };

  return (
    <div className="pz">
      <div className="pz-main">
        <div className="pz-stepper">
          {[
            { n: 1, label: 'Partição', cond: true },
            { n: 2, label: 'Sabores', cond: true },
            { n: 3, label: 'Adicionais', cond: true },
            { n: 4, label: 'Observações', cond: true },
          ].map((s, i) => (
            <button key={s.n}
              className={'pz-step clickable ' + (step === s.n ? 'active' : (step > s.n ? 'done' : ''))}
              onClick={() => setStep(s.n)}>
              <span className="pz-step-num">{step > s.n ? '✓' : s.n}</span>
              {s.label}
            </button>
          ))}
        </div>

        <div className="pz-body">
          {step === 1 && (
            <>
              <h2>Quantos sabores?</h2>
              <p className="sub">Pizza {size.name.toLowerCase()} · {size.slices} fatias · preço pelo sabor mais caro</p>
              <div className="pz-partition">
                {[1, 2, size.maxFlavors > 2 ? size.maxFlavors : null].filter(Boolean).filter((v, i, a) => a.indexOf(v) === i).map(n => (
                  <button key={n}
                    className={'pz-part-card ' + (partition === n ? 'active' : '')}
                    onClick={() => { setPartition(n); setSlots(prev => { const next = [...prev]; for (let i = n; i < next.length; i++) next[i] = null; return next; }); setTimeout(() => setStep(2), 120); }}>
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 15 }}>
                        {n === 1 ? 'Inteira' : n === 2 ? 'Meio-a-meio' : `${n} sabores`}
                      </div>
                      <div style={{ color: 'var(--fg-muted)', fontSize: 12, marginTop: 2 }}>
                        {n === 1 ? '1 sabor' : `${n} sabores diferentes`}
                      </div>
                    </div>
                    <div className="pz-part-slots" style={{ width: 60 }}>
                      {Array(n).fill(0).map((_, i) => <div key={i} className="pz-part-slot" />)}
                    </div>
                  </button>
                ))}
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <h2>Escolha os sabores</h2>
              <p className="sub">{partition === 1 ? 'Pizza inteira — 1 sabor' : `${partition} sabores — clique em cada slot para escolher`}</p>

              <div className="pz-slots">
                {effSlots.map((sid, i) => {
                  const flavor = sid ? FLAVORS.find(f => f.id === sid) : null;
                  return (
                    <button key={i}
                      className={'pz-slot ' + (flavor ? 'filled ' : '') + (activeSlot === i ? 'active' : '')}
                      onClick={() => setActiveSlot(i)}>
                      <div className="pz-slot-num">{i + 1}</div>
                      <div className="pz-slot-info">
                        <div className="pz-slot-label">
                          {partition === 1 ? 'Sabor' : `${i + 1}º Sabor`}
                        </div>
                        <div className={'pz-slot-value ' + (flavor ? '' : 'empty')}>
                          {flavor ? flavor.name : 'Toque para escolher…'}
                        </div>
                      </div>
                      {flavor && (
                        <span className="badge">{BRL(flavor.prices[size.size])}</span>
                      )}
                    </button>
                  );
                })}
              </div>

              <div style={{ marginTop: 16, marginBottom: 4, display: 'flex', alignItems: 'center', gap: 10 }}>
                <div className="input-with-ico" style={{ flex: 1 }}>
                  <span className="ico"><Icon name="search" size={16} /></span>
                  <input className="input" autoFocus
                    placeholder="Filtrar sabores (nome ou ingrediente)…"
                    value={flavorQuery}
                    onChange={(e) => setFlavorQuery(e.target.value)} />
                </div>
                {flavorQuery && (
                  <button className="btn btn-ghost" onClick={() => setFlavorQuery('')}>
                    <Icon name="x" size={13} /> Limpar
                  </button>
                )}
              </div>

              <div className="pz-flavors">
                {(() => {
                  const q = flavorQuery.trim().toLowerCase();
                  const list = q
                    ? FLAVORS.filter(f => f.name.toLowerCase().includes(q) || f.desc.toLowerCase().includes(q))
                    : FLAVORS;
                  if (list.length === 0) {
                    return (
                      <div style={{ gridColumn: '1 / -1', padding: '30px 10px', textAlign: 'center', color: 'var(--fg-soft)', fontSize: 13 }}>
                        Nenhum sabor encontrado para "{flavorQuery}".
                      </div>
                    );
                  }
                  return list.map(f => (
                    <button key={f.id}
                      className={'pz-flavor ' + (effSlots.includes(f.id) ? 'selected' : '')}
                      onClick={() => pickFlavor(f.id)}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                        <span className="pz-flavor-name">{f.name}</span>
                        <span className="pz-flavor-price">{BRL(f.prices[size.size])}</span>
                      </div>
                      <div className="pz-flavor-desc">{f.desc}</div>
                    </button>
                  ));
                })()}
              </div>
            </>
          )}

          {step === 3 && (
            <>
              <h2>Adicionais e personalização</h2>
              <p className="sub">Ingredientes padrão podem ser removidos. Adicionais são cobrados à parte.</p>

              <div>
                {INGREDIENTS.map(ing => {
                  const st = ingredients[ing.id];
                  return (
                    <div key={ing.id} className="pz-ing-row">
                      <div>
                        <div className="pz-ing-name">{ing.name}</div>
                        <div className="pz-ing-meta">
                          {ing.default ? 'padrão · ' : 'adicional · '}
                          {ing.price === 0 ? 'sem custo' : '+' + BRL(ing.price)}
                        </div>
                      </div>
                      <div className="pz-ing-ctl">
                        {ing.default ? (
                          <div className="tweak-segmented" style={{ width: 160 }}>
                            <button className={st === 'in' ? 'active' : ''} onClick={() => setIngredients(p => ({ ...p, [ing.id]: 'in' }))}>Com</button>
                            <button className={st === 'out' ? 'active' : ''} onClick={() => setIngredients(p => ({ ...p, [ing.id]: 'out' }))}>Sem</button>
                          </div>
                        ) : (
                          st === 'in'
                            ? <button className="btn" onClick={() => setIngredients(p => ({ ...p, [ing.id]: 'out' }))}><Icon name="check" size={12} /> Adicionado</button>
                            : <button className="btn pz-ing-add" onClick={() => setIngredients(p => ({ ...p, [ing.id]: 'in' }))}><Icon name="plus" size={12} /> Adicionar</button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </>
          )}

          {step === 4 && (
            <>
              <h2>Observações</h2>
              <p className="sub">Instruções especiais para a cozinha (opcional)</p>
              <textarea className="input" rows={5}
                style={{ resize: 'vertical', fontFamily: 'var(--font-ui)' }}
                placeholder="Ex: bem passada, cortar em 12 pedaços, sem orégano nos sabores doces…"
                value={notes}
                onChange={(e) => setNotes(e.target.value)} />
            </>
          )}
        </div>

        <div className="pz-foot">
          <button className="btn" onClick={onCancel}><Icon name="chevron-left" size={14} /> Voltar ao PDV</button>
          <div className="pz-foot-price">
            <span className="pz-foot-price-label">Total do item</span>
            <span className="pz-foot-price-val">{BRL(total)}</span>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            {step > 1 && <button className="btn" onClick={() => setStep(s => s - 1)}>Anterior</button>}
            {step < 4 ? (
              <button className="btn btn-primary btn-lg" disabled={!canAdvance || (step === 2 && !allFilled)}
                onClick={() => setStep(s => s + 1)}
                style={{ opacity: (step === 2 && !allFilled) ? 0.4 : 1 }}>
                Próximo <Icon name="chevron-right" size={14} />
              </button>
            ) : (
              <button className="btn btn-primary btn-lg" disabled={!allFilled} onClick={confirm}
                style={{ opacity: allFilled ? 1 : 0.4 }}>
                <Icon name="check" size={14} /> Adicionar ao pedido
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="pz-side">
        <div className="pz-summary">
          <div className="pz-sum-head">
            <div>
              <div className="pz-sum-title">Pizza {size.name}</div>
              <div className="pz-sum-sub">{size.slices} fatias · {partition === 1 ? 'Inteira' : `${partition} sabores`}</div>
            </div>
            <span className="badge">{size.size}</span>
          </div>

          <div>
            <div className="pz-sum-section-label">Sabores</div>
            <div className="pz-sum-list">
              {effSlots.map((sid, i) => {
                const f = sid ? FLAVORS.find(fl => fl.id === sid) : null;
                return (
                  <div key={i} className={'pz-sum-item ' + (f ? '' : 'empty')}>
                    <span className="name">{i + 1}. {f ? f.name : 'não selecionado'}</span>
                    {f && <span className="val">{BRL(f.prices[size.size])}</span>}
                  </div>
                );
              })}
            </div>
          </div>

          <div>
            <div className="pz-sum-section-label">Personalização</div>
            <div className="pz-sum-list">
              {Object.entries(ingredients).map(([id, st]) => {
                const ing = INGREDIENTS.find(i => i.id === id);
                if (!ing) return null;
                if (ing.default && st === 'in') return null;
                if (!ing.default && st === 'out') return null;
                const sign = st === 'in' ? '+' : '−';
                const txt = st === 'in' ? ing.name : 'sem ' + ing.name.toLowerCase();
                return (
                  <div key={id} className="pz-sum-item">
                    <span className="name" style={{ color: st === 'in' ? 'var(--fg)' : 'var(--fg-muted)' }}>{sign} {txt}</span>
                    {ing.price > 0 && st === 'in' && <span className="val">+{BRL(ing.price)}</span>}
                  </div>
                );
              })}
              {Object.entries(ingredients).every(([id, st]) => {
                const ing = INGREDIENTS.find(i => i.id === id);
                return ing && ((ing.default && st === 'in') || (!ing.default && st === 'out'));
              }) && (
                <div className="pz-sum-item empty"><span className="name">Nenhuma alteração</span></div>
              )}
            </div>
          </div>

          {notes && (
            <div>
              <div className="pz-sum-section-label">Observações</div>
              <div style={{ fontSize: 13, color: 'var(--fg-muted)', fontStyle: 'italic' }}>"{notes}"</div>
            </div>
          )}

          <div style={{ marginTop: 'auto', paddingTop: 16, borderTop: '1px dashed var(--line)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, color: 'var(--fg-muted)', marginBottom: 4 }}>
              <span>Base ({size.name})</span>
              <span style={{ fontFamily: 'var(--font-mono)' }}>{BRL(basePrice)}</span>
            </div>
            {extras > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, color: 'var(--fg-muted)', marginBottom: 4 }}>
                <span>Adicionais</span>
                <span style={{ fontFamily: 'var(--font-mono)' }}>+{BRL(extras)}</span>
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 700, fontSize: 17, paddingTop: 6, borderTop: '1px dashed var(--line)', marginTop: 6 }}>
              <span>Total</span>
              <span style={{ fontFamily: 'var(--font-mono)' }}>{BRL(total)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

window.PizzaFlow = PizzaFlow;
