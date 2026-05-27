// ════════════════════════════════════════════════
// Shared App Shell — used by PDV and Dashboard
// Components: AppShell, TopBar, Sidebar, NavItem
// ════════════════════════════════════════════════
const { useState: useShellState, useEffect: useShellEffect } = React;
const ShellIcon = window.Icon;

// ─── Reusable bits ──────────────────────────────────

function Clock() {
  const [t, setT] = useShellState(() => new Date());
  useShellEffect(() => {
    const id = setInterval(() => setT(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  const pad = n => n.toString().padStart(2, '0');
  return <span className="topbar-clock">{pad(t.getHours())}:{pad(t.getMinutes())}:{pad(t.getSeconds())}</span>;
}

function StoreBadge({ open, onToggle }) {
  return (
    <button className={'store-badge ' + (open ? 'is-open' : 'is-closed')} onClick={onToggle}>
      <span className="store-badge-dot" />
      {open ? 'Aberto' : 'Fechado'}
    </button>
  );
}

function ThemeToggle({ dark, onToggle }) {
  return (
    <button className="topbar-icon-btn" onClick={onToggle} aria-label="Alternar tema" title="Alternar tema">
      <ShellIcon name={dark ? 'sun' : 'moon'} size={16} />
    </button>
  );
}

function NotifButton({ count, open, onToggle, items, onClose }) {
  const unread = items.filter(n => n.unread).length;
  return (
    <div style={{position:'relative'}} onClick={e => e.stopPropagation()}>
      <button className="topbar-icon-btn" onClick={onToggle} aria-label="Notificações">
        <ShellIcon name="bell" size={16} />
        {unread > 0 && <span className="badge-dot">{unread}</span>}
      </button>
      {open && (
        <div className="dropdown-panel" style={{minWidth:340}}>
          <div className="dropdown-head">
            <span>Notificações</span>
            <button className="btn-ghost" style={{fontSize:11,padding:'2px 6px'}}>Marcar todas como lidas</button>
          </div>
          <div className="notif-list">
            {items.length === 0 && (
              <div style={{padding:'24px 16px',textAlign:'center',color:'var(--fg-soft)',fontSize:13}}>
                Sem notificações
              </div>
            )}
            {items.map((n,i) => (
              <div key={i} className={'notif-item' + (n.unread ? ' is-unread' : '')}>
                <span className={'notif-dot is-' + (n.kind || 'accent')} />
                <div style={{flex:1}}>
                  <div className="notif-text">{n.text}</div>
                  <div className="notif-time">{n.time}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function UserMenu({ user, open, onToggle }) {
  return (
    <div style={{position:'relative'}} onClick={e => e.stopPropagation()}>
      <div className="topbar-user is-button" onClick={onToggle}>
        <div className="avatar">{user.initials}</div>
        <div>
          <div className="topbar-user-name">{user.name}</div>
          <div className="topbar-user-role">{user.role}</div>
        </div>
      </div>
      {open && (
        <div className="dropdown-panel" style={{minWidth:220}}>
          <div className="dropdown-head"><span>{user.name}</span></div>
          <button className="dropdown-item"><ShellIcon name="user" size={14} />Meu perfil</button>
          <button className="dropdown-item"><ShellIcon name="admin" size={14} />Configurações</button>
          <button className="dropdown-item" style={{color:'var(--danger)'}}><ShellIcon name="x" size={14} />Sair</button>
        </div>
      )}
    </div>
  );
}

// ─── TopBar ──────────────────────────────────────────

function TopBar({
  brand,                // {name, tenant?, mark}
  onMobileMenu,         // mobile hamburger
  showSearch = true,    // ⌘K hint
  showClock = false,
  showStoreBadge = false, storeOpen, onStoreToggle,
  showThemeToggle = false, dark, onDarkToggle,
  showNotif = false,    notifs = [], notifOpen, onNotifToggle,
  user, userOpen, onUserToggle,
}) {
  return (
    <div className="topbar">
      {onMobileMenu && (
        <button className="mobile-menu-btn" onClick={onMobileMenu} aria-label="Menu">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M3 6h18M3 12h18M3 18h18"/></svg>
        </button>
      )}
      <div className="brand">
        <div className="brand-mark">{brand.mark || 'F'}</div>
        <span className="brand-name">{brand.name}</span>
        {brand.tenant && <>
          <span className="brand-slash">/</span>
          <span className="brand-tenant">{brand.tenant}</span>
        </>}
      </div>
      <div className="topbar-spacer" />

      {showSearch && (
        <div className="topbar-kbd">
          <span className="kbd">⌘K</span>
          <span>Buscar pedido, cliente ou produto</span>
        </div>
      )}
      {showClock && <Clock />}
      {showStoreBadge && <StoreBadge open={storeOpen} onToggle={onStoreToggle} />}
      {showThemeToggle && <ThemeToggle dark={dark} onToggle={onDarkToggle} />}
      {showNotif && <NotifButton items={notifs} open={notifOpen} onToggle={onNotifToggle} />}

      {user && <UserMenu user={user} open={userOpen} onToggle={onUserToggle} />}
    </div>
  );
}

// ─── Sidebar ────────────────────────────────────────

function NavItem({ active, icon, label, count, onClick }) {
  return (
    <button className={'nav-item ' + (active ? 'active' : '')} onClick={onClick}>
      <ShellIcon name={icon} className="nav-ico" size={17} />
      <span className="nav-label">{label}</span>
      {count != null && <span className="nav-count">{count}</span>}
    </button>
  );
}

function Sidebar({
  open,                // mobile drawer state
  onClose,             // mobile close handler
  collapsible = false, collapsed, onToggleCollapse,
  topMeta,             // {name, plan} optional sidebar header (used by Dashboard)
  groups,              // [{label, items: [{id, label, icon, count}]}]
  activeId, onSelect,
  footer,              // optional ReactNode (e.g. plan card or links)
}) {
  return (
    <>
      {open && <div className="sidebar-backdrop" onClick={onClose} />}
      <aside className={'sidebar ' + (open ? 'open' : '')}>
        {topMeta && (
          <div className="sidebar-top">
            <div className="sidebar-top-meta">
              <div className="sidebar-top-name">{topMeta.name}</div>
              {topMeta.plan && <span className="sidebar-top-plan">{topMeta.plan}</span>}
            </div>
            {collapsible && (
              <button className="sidebar-collapse" onClick={onToggleCollapse} aria-label="Colapsar">
                <ShellIcon name={collapsed ? 'chevron-right' : 'chevron-left'} size={14} />
              </button>
            )}
          </div>
        )}
        {groups.map(g => (
          <React.Fragment key={g.label}>
            <div className="nav-section-label">{g.label}</div>
            {g.items.map(it => (
              <NavItem key={it.id}
                active={activeId === it.id}
                icon={it.icon} label={it.label} count={it.count}
                onClick={() => { onSelect(it.id); onClose && onClose(); }} />
            ))}
          </React.Fragment>
        ))}
        {footer && <div className="sidebar-footer">{footer}</div>}
      </aside>
    </>
  );
}

// ─── AppShell ──────────────────────────────────────

function AppShell({ collapsed, children }) {
  return (
    <div className={'app' + (collapsed ? ' sidebar-collapsed' : '')}>
      {children}
    </div>
  );
}

Object.assign(window, { AppShell, TopBar, Sidebar, NavItem, Clock, StoreBadge, ThemeToggle, NotifButton, UserMenu });
