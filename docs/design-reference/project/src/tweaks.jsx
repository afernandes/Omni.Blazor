// Tweaks panel
function TweaksPanel({ tweaks, setTweaks }) {
  const setKey = (k, v) => {
    const next = { ...tweaks, [k]: v };
    setTweaks(next);
    try {
      window.parent.postMessage({ type: '__edit_mode_set_keys', edits: { [k]: v } }, '*');
    } catch (e) {}
  };

  const accents = [
    { id: 'crimson', color: '#dc2626' },
    { id: 'amber',   color: '#d97706' },
    { id: 'emerald', color: '#059669' },
    { id: 'blue',    color: '#2563eb' },
    { id: 'violet',  color: '#7c3aed' },
  ];

  return (
    <div className="tweaks">
      <div className="tweaks-head">
        <span>Tweaks</span>
        <Icon name="sliders" size={14} />
      </div>
      <div className="tweaks-body">
        <div className="tweak-row">
          <div className="tweak-row-label">Cor de destaque</div>
          <div className="tweak-swatches">
            {accents.map(a => (
              <button key={a.id}
                className={'tweak-swatch ' + (tweaks.accent === a.id ? 'active' : '')}
                style={{ background: a.color }}
                onClick={() => setKey('accent', a.id)}
                title={a.id} />
            ))}
          </div>
        </div>
        <div className="tweak-row">
          <div className="tweak-row-label">Tema</div>
          <div className="tweak-segmented">
            <button className={!tweaks.dark ? 'active' : ''} onClick={() => setKey('dark', false)}><Icon name="sun" size={12} style={{ verticalAlign: -2, marginRight: 4 }} />Claro</button>
            <button className={tweaks.dark ? 'active' : ''} onClick={() => setKey('dark', true)}><Icon name="moon" size={12} style={{ verticalAlign: -2, marginRight: 4 }} />Escuro</button>
          </div>
        </div>
        <div className="tweak-row">
          <div className="tweak-row-label">Layout do PDV</div>
          <div className="tweak-segmented">
            <button className={tweaks.pdvLayout === 'sidebar' ? 'active' : ''} onClick={() => setKey('pdvLayout', 'sidebar')}>Sidebar</button>
            <button className={tweaks.pdvLayout === 'grid' ? 'active' : ''} onClick={() => setKey('pdvLayout', 'grid')}>Compacto</button>
          </div>
        </div>
        <div className="tweak-row">
          <div className="tweak-row-label">Densidade do grid</div>
          <div className="tweak-segmented">
            <button className={(tweaks.density || 'comfort') === 'comfort' ? 'active' : ''} onClick={() => setKey('density', 'comfort')}>Confortável</button>
            <button className={tweaks.density === 'compact' ? 'active' : ''} onClick={() => setKey('density', 'compact')}>Compacto</button>
          </div>
        </div>
      </div>
    </div>
  );
}

window.TweaksPanel = TweaksPanel;
