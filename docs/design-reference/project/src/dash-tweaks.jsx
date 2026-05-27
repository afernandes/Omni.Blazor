// ——— Dashboard — Tweaks panel ———
const Tw = window;
const { TweaksPanel, useTweaks, TweakSection, TweakRadio } = Tw;

function DashTweaks() {
  const [t, setTweak] = useTweaks(window.__DASH_TWEAK_DEFAULTS);

  React.useEffect(() => {
    document.documentElement.dataset.tom = t.tom;
    document.documentElement.dataset.densidade = t.densidade;
    document.documentElement.dataset.energia = t.energia;
  }, [t.tom, t.densidade, t.energia]);

  return (
    <TweaksPanel title="Tweaks">
      <TweakSection title="Tom" hint="Personalidade do produto">
        <TweakRadio value={t.tom} onChange={v => setTweak('tom', v)} options={[
          { value:'operacional',   label:'Operacional'   },
          { value:'hospitalidade', label:'Hospitalidade' },
          { value:'premium',       label:'Premium'       },
        ]} />
      </TweakSection>
      <TweakSection title="Densidade" hint="Ar entre os elementos">
        <TweakRadio value={t.densidade} onChange={v => setTweak('densidade', v)} options={[
          { value:'compacto',    label:'Compacto'    },
          { value:'confortavel', label:'Confortável' },
          { value:'amplo',       label:'Amplo'       },
        ]} />
      </TweakSection>
      <TweakSection title="Energia" hint="Movimento e saturação">
        <TweakRadio value={t.energia} onChange={v => setTweak('energia', v)} options={[
          { value:'calmo',    label:'Calmo'    },
          { value:'vibrante', label:'Vibrante' },
        ]} />
      </TweakSection>
    </TweaksPanel>
  );
}

const tweaksRoot = document.getElementById('tweaks-root');
if (tweaksRoot) ReactDOM.createRoot(tweaksRoot).render(<DashTweaks />);
