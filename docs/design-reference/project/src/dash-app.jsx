// ——— Dashboard — App principal + estado ———
const { useState: useAS, useEffect: useAE } = React;
const Icon = window.Icon;

function App() {
  // Persistência de tema
  const [dark, setDark] = useAS(() => localStorage.getItem('dash-dark') === 'true');
  const [role, setRole] = useAS('gerente');           // 'gerente' | 'operador'
  const [period, setPeriod] = useAS('hoje');
  const [orders, setOrders] = useAS(() => [...DASH_SEED_ORDERS]);
  const [newOrderId, setNewOrderId] = useAS(null);
  const [modalOrder, setModalOrder] = useAS(null);
  const [notifOpen, setNotifOpen] = useAS(false);
  const [profileOpen, setProfileOpen] = useAS(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useAS(false);
  const [sidebarOpen, setSidebarOpen] = useAS(false);
  const [storeOpen, setStoreOpen] = useAS(true);
  const [notifs] = useAS([...DASH_NOTIFS]);

  // Aplicar tema / accent
  useAE(() => {
    document.documentElement.dataset.theme  = dark ? 'dark' : 'light';
    document.documentElement.dataset.accent = 'amber';
    localStorage.setItem('dash-dark', String(dark));
  }, [dark]);

  // Simulação de pedido ao vivo a cada 8 segundos
  useAE(() => {
    let counter = 4726;
    let poolIdx = 0;
    const interval = setInterval(() => {
      const tpl = DASH_ORDER_POOL[poolIdx % DASH_ORDER_POOL.length];
      poolIdx++;
      const now = new Date();
      const t = `${now.getHours().toString().padStart(2,'0')}:${now.getMinutes().toString().padStart(2,'0')}`;
      const o = { ...tpl, id: String(counter++), status:'preparo', time:t };
      setOrders(prev => [o, ...prev].slice(0, 8));
      setNewOrderId(o.id);
      setTimeout(() => setNewOrderId(null), 900);
    }, 8000);
    return () => clearInterval(interval);
  }, []);

  // Data formatada
  const today = new Date();
  const WD = ['Domingo','Segunda-feira','Terça-feira','Quarta-feira','Quinta-feira','Sexta-feira','Sábado'];
  const MO = ['janeiro','fevereiro','março','abril','maio','junho','julho','agosto','setembro','outubro','novembro','dezembro'];
  const dateStr = `${WD[today.getDay()]}, ${today.getDate()} de ${MO[today.getMonth()]} de ${today.getFullYear()}`;
  const greet   = today.getHours() < 12 ? 'Bom dia' : today.getHours() < 18 ? 'Boa tarde' : 'Boa noite';

  // Hamburger: mobile = drawer, desktop = colapso
  const onHamburger = () => {
    if (window.innerWidth <= 1024) setSidebarOpen(v => !v);
    else setSidebarCollapsed(v => !v);
  };

  const PERIODS = [
    { id:'hoje',   label:'Hoje'        },
    { id:'semana', label:'Esta semana' },
    { id:'mes',    label:'Este mês'    },
  ];

  return (
    <div className={'dash-app' + (sidebarCollapsed ? ' sidebar-collapsed' : '')}
         onClick={() => { setNotifOpen(false); setProfileOpen(false); }}>

      <DashHeader
        dark={dark} onToggleDark={() => setDark(v => !v)}
        notifs={notifs} notifOpen={notifOpen} setNotifOpen={setNotifOpen}
        profileOpen={profileOpen} setProfileOpen={setProfileOpen}
        onHamburger={onHamburger}
        storeOpen={storeOpen} setStoreOpen={setStoreOpen}
        onSwitchRole={() => setRole(v => v === 'gerente' ? 'operador' : 'gerente')}
        role={role}
      />

      <DashSidebar
        collapsed={sidebarCollapsed} setCollapsed={setSidebarCollapsed}
        open={sidebarOpen} onClose={() => setSidebarOpen(false)}
      />

      <main className="dash-main">
        {/* ── Subheader fixo da página ── */}
        <div className="d-page-head">
          <div>
            <div className="d-greeting">{greet}, Anderson! <span>👋</span></div>
            <div className="d-date">Forneria Don Tonhão · {dateStr}</div>
          </div>
          <div className="d-page-head-right">
            {role === 'gerente' && (
              <div className="d-period-pills">
                {PERIODS.map(p => (
                  <button key={p.id} className={'d-pill' + (period === p.id ? ' active' : '')}
                          onClick={e => { e.stopPropagation(); setPeriod(p.id); }}>
                    {p.label}
                  </button>
                ))}
                <button className="d-pill">Personalizado</button>
              </div>
            )}
            <button className="btn"><Icon name="sliders" size={13} />Exportar</button>
          </div>
        </div>

        {/* ── Toggle de perfil ── */}
        <div className="d-view-toggle-bar" onClick={e => e.stopPropagation()}>
          <div className="d-view-toggle">
            {[['gerente','📊 Gerente / Dono'],['operador','🖥 Operador']].map(([v, lbl]) => (
              <button key={v} className={'d-vt-btn' + (role === v ? ' active' : '')}
                      onClick={() => setRole(v)}>{lbl}</button>
            ))}
          </div>
        </div>

        {role === 'gerente'
          ? <GerenteView orders={orders} newOrderId={newOrderId} period={period} dark={dark} onOpenModal={setModalOrder} />
          : <OperadorView />
        }
      </main>

      {modalOrder && <OrderModal order={modalOrder} onClose={() => setModalOrder(null)} />}
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
