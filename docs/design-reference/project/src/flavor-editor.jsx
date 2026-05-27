// FlavorEditor — per-flavor pizza customization modal

function FlavorEditor({ flavor, partition, custom = {}, onSave, onClose }) {
  const defaultIngredients = flavor.ingredients || [];
  const [removed, setRemoved] = React.useState(custom.removed || []);
  const [extras, setExtras] = React.useState(custom.extras || []);
  const [note, setNote] = React.useState(custom.note || '');

  const toggleRemove = (ing) =>
    setRemoved(r => r.includes(ing) ? r.filter(x => x !== ing) : [...r, ing]);

  const adjustExtra = (id, delta) =>
    setExtras(arr => {
      const existing = arr.find(x => x.id === id);
      if (!existing) return delta > 0 ? [...arr, { id, qty: 1 }] : arr;
      const newQty = existing.qty + delta;
      return newQty <= 0 ? arr.filter(x => x.id !== id) : arr.map(x => x.id === id ? { ...x, qty: newQty } : x);
    });

  const extraQty = (id) => (extras.find(x => x.id === id) || {}).qty || 0;

  const extrasTotal = (window.FLAVOR_EXTRAS || []).reduce((s, fe) => {
    return s + (extraQty(fe.id) * fe.price);
  }, 0);

  const label = partition === 1 ? '1/2' : partition === 2 ? '2/2' : '';

  const save = () => {
    onSave({ removed, extras, note, extrasDelta: extrasTotal });
  };

  return (
    <div className="fe-overlay" onClick={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="fe-modal">
        <div className="fe-head">
          <div className="fe-title">Personalizando {label && <span className="fe-half">{label}</span>} {flavor.name}</div>
          <button className="btn btn-ghost btn-icon" onClick={onClose}><Icon name="x" size={16} /></button>
        </div>
        <div className="fe-body">
          <section className="fe-section">
            <div className="fe-section-head">
              <span className="fe-section-label">REMOVER INGREDIENTES</span>
              <span className="fe-section-sub">sem custo</span>
            </div>
            <div className="fe-chips">
              {defaultIngredients.map(ing => {
                const off = removed.includes(ing);
                return (
                  <button key={ing}
                    className={'fe-chip ' + (off ? 'off' : 'on')}
                    onClick={() => toggleRemove(ing)}>
                    {!off && <Icon name="check" size={13} style={{ color: 'var(--good)' }} />}
                    {off && <span className="fe-chip-x">✕</span>}
                    {ing}
                  </button>
                );
              })}
            </div>
          </section>

          <section className="fe-section">
            <div className="fe-section-head">
              <span className="fe-section-label">ADICIONAR EXTRAS</span>
              <span className="fe-section-sub">turbine seu sabor</span>
            </div>
            <div className="fe-extras-list">
              {(window.FLAVOR_EXTRAS || []).map(fe => {
                const qty = extraQty(fe.id);
                return (
                  <div key={fe.id} className="fe-extra-row">
                    <div className="fe-extra-thumb"></div>
                    <div className="fe-extra-info">
                      <div className="fe-extra-name">{fe.name}</div>
                    </div>
                    <div className={'fe-extra-price ' + (fe.price === 0 ? 'free' : '')}>
                      {fe.price === 0 ? 'Grátis' : '+ ' + BRL(fe.price)}
                    </div>
                    <div className="fe-extra-ctrl">
                      {qty > 0 && (
                        <>
                          <button className="fe-extra-btn minus" onClick={() => adjustExtra(fe.id, -1)}>−</button>
                          <span className="fe-extra-qty">{qty}</span>
                        </>
                      )}
                      <button className={'fe-extra-btn plus ' + (qty > 0 ? 'active' : '')}
                        onClick={() => adjustExtra(fe.id, 1)}>+</button>
                    </div>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="fe-section">
            <div className="fe-section-head">
              <span className="fe-section-label">OBSERVAÇÃO</span>
              <span className="fe-section-sub">para este sabor</span>
            </div>
            <textarea
              className="fe-note"
              value={note}
              onChange={e => setNote(e.target.value)}
              placeholder="Ex: bem temperado, capricha no recheio"
              rows={3}
            />
          </section>
        </div>

        <div className="fe-foot">
          {extrasTotal > 0 && (
            <div className="fe-foot-delta">+{BRL(extrasTotal)} neste sabor</div>
          )}
          <button className="btn btn-primary" style={{ flex: 1, justifyContent: 'center' }} onClick={save}>
            Salvar personalização
          </button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { FlavorEditor });
