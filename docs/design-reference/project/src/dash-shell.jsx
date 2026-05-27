// ——— Dashboard — Header + Sidebar ———
const { useState: useSH, useEffect: useSE } = React;
const Icon = window.Icon;

// Extra SVG icons not in the base Icon set
const IcHome    = () => <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>;
const IcKDS     = () => <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>;
const IcUsers   = () => <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>;
const IcBar     = () => <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>;
const IcMoney   = () => <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 1 0 0 7h5a3.5 3.5 0 1 1 0 7H6"/></svg>;
const IcChevL   = () => <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M15 18l-6-6 6-6"/></svg>;
const IcChevR   = () => <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M9 18l6-6-6-6"/></svg>;
const IcChevD   = () => <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M6 9l6 6 6-6"/></svg>;
const IcLogout  = () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>;
const IcSwitch  = () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><path d="M17 1l4 4-4 4"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><path d="M7 23l-4-4 4-4"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg>;

const NAV_ITEMS = [
  { id:'dashboard',  label:'Dashboard',       Ic:IcHome,                                         group:'main'   },
  { id:'pdv',        label:'PDV / Caixa',     Ic:() => <Icon name="pos" size={17} />,             group:'main',  href:'index.html', target:'_blank' },
  { id:'mesas',      label:'Mesas / Comandas',Ic:() => <Icon name="table" size={17} />,           group:'main'   },
  { id:'pedidos',    label:'Pedidos',         Ic:() => <Icon name="cart" size={17} />,             group:'main'   },
  { id:'kds',        label:'KDS',             Ic:IcKDS,                                          group:'main'   },
  { id:'clientes',   label:'Clientes',        Ic:IcUsers,                                        group:'mgmt'   },
  { id:'relatorios', label:'Relatórios',      Ic:IcBar,                                          group:'mgmt'   },
  { id:'financeiro', label:'Financeiro',      Ic:IcMoney,                                        group:'mgmt'   },
  { id:'config',     label:'Configurações',   Ic:() => <Icon name="admin" size={17} />,           group:'system' },
];

const GROUP_LABELS = { main:'Principal', mgmt:'Gestão', system:'Sistema' };

// ——— Header ———
function DashHeader({ dark, onToggleDark, notifs, notifOpen, setNotifOpen, profileOpen, setProfileOpen, onHamburger, storeOpen, setStoreOpen, onSwitchRole, role }) {
  const [time, setTime] = useSH(new Date());
  useSE(() => { const t = setInterval(() => setTime(new Date()), 1000); return () => clearInterval(t); }, []);
  const unread = notifs.filter(n => !n.read).length;
  const hhmm = time.toLocaleTimeString('pt-BR', { hour:'2-digit', minute:'2-digit', second:'2-digit' });

  const closeDropdowns = () => { setNotifOpen(false); setProfileOpen(false); };

  return (
    <header className="dash-header" onClick={closeDropdowns}>
      <button className="d-hamburger" onClick={e => { e.stopPropagation(); onHamburger(); }}>
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M3 6h18M3 12h18M3 18h18"/></svg>
      </button>

      <a href="dashboard.html" className="brand" style={{textDecoration:'none',color:'inherit'}}>
        <div className="brand-mark">F</div>
        <span className="brand-name">Don Tonhão</span>
        <span className="brand-slash">/</span>
        <span className="brand-tenant">PDV</span>
      </a>

      <div className="d-search" onClick={e => e.stopPropagation()}>
        <span className="d-search-ico"><Icon name="search" size={13} /></span>
        <input type="text" placeholder="Buscar pedido, cliente, produto..." />
      </div>

      <div style={{flex:1}} />

      <span className="d-clock">{hhmm}</span>

      <div className="d-actions" onClick={e => e.stopPropagation()}>
        {/* Store status */}
        <button className={'d-store-badge ' + (storeOpen ? 'open' : 'closed')} onClick={() => setStoreOpen(v => !v)}>
          <span className="d-pulse-dot" />
          {storeOpen ? 'Aberto' : 'Fechado'}
        </button>

        {/* Dark mode */}
        <button className="d-icon-btn" onClick={onToggleDark} title={dark ? 'Modo claro' : 'Modo escuro'}>
          <Icon name={dark ? 'sun' : 'moon'} size={15} />
        </button>

        {/* Notifications */}
        <div style={{position:'relative'}}>
          <button className="d-icon-btn" onClick={e => { e.stopPropagation(); setNotifOpen(v => !v); setProfileOpen(false); }}>
            <Icon name="bell" size={15} />
            {unread > 0 && <span className="d-notif-badge">{unread}</span>}
          </button>
          {notifOpen && (
            <div className="d-dropdown" style={{width:340}} onClick={e => e.stopPropagation()}>
              <div className="d-dropdown-head">
                <span>Notificações</span>
                <button style={{fontSize:'11px',fontWeight:600,color:'var(--accent)',background:'none',border:'none',cursor:'pointer'}}>Marcar todas como lidas</button>
              </div>
              {notifs.map(n => (
                <div key={n.id} className={'d-notif-item' + (n.read ? '' : ' unread')}>
                  <span className={'d-notif-dot ' + n.color} />
                  <div style={{flex:1}}>
                    <div className="d-notif-text">{n.text}</div>
                    <div className="d-notif-time">{n.time}</div>
                  </div>
                  {n.action && <button className="d-notif-action">{n.action}</button>}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* User menu */}
        <div style={{position:'relative'}}>
          <button className="d-user-btn" onClick={e => { e.stopPropagation(); setProfileOpen(v => !v); setNotifOpen(false); }}>
            <div className="avatar" style={{width:28,height:28,borderRadius:7,fontSize:12,background:'var(--accent-soft)',color:'var(--accent)'}}>AS</div>
            <div className="d-user-info">
              <div className="d-user-name">Anderson Silva</div>
              <div className="d-user-role">{role === 'gerente' ? 'Gerente / Dono' : 'Operador de Caixa'}</div>
            </div>
            <IcChevD />
          </button>
          {profileOpen && (
            <div className="d-dropdown" style={{width:224}} onClick={e => e.stopPropagation()}>
              <div className="d-dropdown-head" style={{flexDirection:'column',alignItems:'flex-start',gap:2}}>
                <div style={{fontWeight:700,fontSize:13}}>Anderson Silva</div>
                <div style={{fontSize:11,color:'var(--fg-muted)',fontWeight:400}}>anderson@dontonhao.com.br</div>
              </div>
              <button className="d-dd-item" onClick={onSwitchRole}><IcSwitch />{role === 'gerente' ? 'Mudar para Operador' : 'Mudar para Gerente'}</button>
              <button className="d-dd-item"><Icon name="user" size={14} />Meu perfil</button>
              <button className="d-dd-item"><Icon name="store" size={14} />Trocar estabelecimento</button>
              <button className="d-dd-item" style={{color:'var(--danger)'}}><IcLogout />Sair</button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

// ——— Sidebar ———
function DashSidebar({ collapsed, setCollapsed, open, onClose }) {
  return (
    <>
      {open && <div className="d-sidebar-backdrop" onClick={onClose} />}
      <aside className={'dash-sidebar' + (collapsed ? ' collapsed' : '') + (open ? ' open' : '')}>
        <div className="d-sidebar-top">
          {!collapsed && (
            <div style={{flex:1,minWidth:0}}>
              <div className="d-store-name">Forneria Don Tonhão</div>
              <span className="d-plan-badge">PRO</span>
            </div>
          )}
          <button className="d-collapse-btn" onClick={e => { e.stopPropagation(); setCollapsed(v => !v); }}>
            {collapsed ? <IcChevR /> : <IcChevL />}
          </button>
        </div>

        <nav className="d-nav">
          {['main','mgmt','system'].map(group => (
            <React.Fragment key={group}>
              <div className="d-nav-label">{GROUP_LABELS[group]}</div>
              {NAV_ITEMS.filter(i => i.group === group).map(item => {
                const isActive = item.id === 'dashboard';
                const Tag = item.href ? 'a' : 'button';
                const extra = item.href ? { href:item.href, target:item.target || '_self' } : {};
                return (
                  <Tag key={item.id} className={'nav-item' + (isActive ? ' active' : '')} {...extra} style={{textDecoration:'none',color:'inherit'}}>
                    <item.Ic />
                    {!collapsed && <span className="nav-label">{item.label}</span>}
                  </Tag>
                );
              })}
            </React.Fragment>
          ))}
        </nav>

        {!collapsed && (
          <div className="d-sidebar-footer">
            <div style={{fontSize:'11px',color:'var(--fg-soft)',fontFamily:'var(--font-mono)',padding:'0 8px'}}>v2.4.1</div>
            <button className="d-footer-link"><Icon name="sparkle" size={13} />Ajuda &amp; Suporte</button>
          </div>
        )}
      </aside>
    </>
  );
}

Object.assign(window, { DashHeader, DashSidebar });
