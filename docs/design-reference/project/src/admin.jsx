// Tenant admin — plans, API keys, domain
function AdminView() {
  const [plan, setPlan] = React.useState('pro');
  const [masked, setMasked] = React.useState({ gmaps: true, pay: true, nfce: true });

  const plans = [
    { id: 'basic', name: 'Básico', price: 149, features: [
      ['pdv', 'PDV Frente de Caixa'], ['cadastro', 'Cadastros ilimitados'], ['cardapio', 'Cardápio Digital'],
    ], not: [['ifood', 'Integração iFood'], ['fiscal', 'Módulo fiscal NFCe'], ['relatorios', 'Relatórios avançados']] },
    { id: 'pro', name: 'Pro', price: 299, current: true, features: [
      ['pdv', 'PDV Frente de Caixa'], ['cadastro', 'Cadastros ilimitados'], ['cardapio', 'Cardápio Digital'],
      ['ifood', 'Integração iFood'], ['garcom', 'Módulo Garçom (ilimitado)'], ['relatorios', 'Relatórios gerenciais'],
    ], not: [['fiscal', 'Módulo fiscal NFCe'], ['multi', 'Multi-unidades']] },
    { id: 'premium', name: 'Premium', price: 549, features: [
      ['pdv', 'Tudo do Pro'], ['fiscal', 'Módulo fiscal NFCe'],
      ['multi', 'Multi-unidades'], ['fidelidade', 'Fidelidade avançada'], ['api', 'API pública'], ['sla', 'SLA prioritário'],
    ], not: [] },
  ];

  return (
    <div className="admin-body">
      <div className="admin-card full">
        <h3>Plano do tenant</h3>
        <div className="sub">Módulos habilitados dependem do plano. Upgrade instantâneo, downgrade no próximo ciclo.</div>
        <div className="plan-grid">
          {plans.map(p => (
            <div key={p.id} className={'plan-tile ' + (plan === p.id ? 'current' : '')}>
              {plan === p.id && <span className="badge accent" style={{ position: 'absolute', top: 12, right: 12 }}>Plano atual</span>}
              <div className="plan-tile-name">{p.name}</div>
              <div className="plan-tile-price">{BRL(p.price)}<span className="unit"> /mês</span></div>
              <ul>
                {p.features.map(([k, v]) => (
                  <li key={k} className="on">
                    <Icon name="check" size={12} className="check" />
                    <span>{v}</span>
                  </li>
                ))}
                {p.not.map(([k, v]) => (
                  <li key={k}>
                    <Icon name="x" size={12} className="check" />
                    <span style={{ textDecoration: 'line-through' }}>{v}</span>
                  </li>
                ))}
              </ul>
              <button className={'btn ' + (plan === p.id ? '' : 'btn-primary')}
                style={{ justifyContent: 'center', marginTop: 8 }}
                onClick={() => setPlan(p.id)}>
                {plan === p.id ? 'Gerenciar' : 'Selecionar'}
              </button>
            </div>
          ))}
        </div>
      </div>

      <div className="admin-card">
        <h3>Domínio personalizado</h3>
        <div className="sub">Aponte seu subdomínio para a plataforma via CNAME.</div>
        <div className="domain-row">
          <span className="status-dot"></span>
          <span style={{ flex: 1 }}>pedidos.fornodobairro.com.br</span>
          <span className="badge good">Ativo · SSL</span>
        </div>
        <div style={{ marginTop: 10, fontSize: 12, color: 'var(--fg-muted)', fontFamily: 'var(--font-mono)' }}>
          CNAME → tenants.forneria.app
        </div>
        <div style={{ display: 'flex', gap: 8, marginTop: 14 }}>
          <button className="btn"><Icon name="plus" size={13} /> Adicionar domínio</button>
          <button className="btn btn-ghost">Documentação DNS</button>
        </div>
      </div>

      <div className="admin-card">
        <h3>Papéis e permissões</h3>
        <div className="sub">Controle baseado em papel (RBAC).</div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {[
            { name: 'Admin',   who: 'Dono do tenant',     perm: 'Configurações, plano, pagamentos, todos os módulos', count: 1 },
            { name: 'Gerente', who: 'Supervisão do salão', perm: 'Relatórios, cadastros, HUB (sem plano/financeiro)', count: 2 },
            { name: 'Caixa',   who: 'PDV',                perm: 'Tela de pedidos, pagamento, caixa do operador',     count: 4 },
            { name: 'Garçom',  who: 'Salão',              perm: 'Lançar pedidos em mesa, visualizar comandas',       count: 6 },
          ].map(r => (
            <div key={r.name} className="card" style={{ padding: 12, display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{r.name}</div>
                <div style={{ fontSize: 11.5, color: 'var(--fg-muted)' }}>{r.perm}</div>
              </div>
              <span className="badge">{r.count} usr</span>
              <button className="btn btn-ghost btn-icon"><Icon name="chevron-right" size={14} /></button>
            </div>
          ))}
        </div>
      </div>

      <div className="admin-card full">
        <h3>Chaves de API</h3>
        <div className="sub">Chaves isoladas por tenant. Criptografia em repouso (AES-256).</div>
        <div className="keys-list">
          {[
            { id: 'gmaps', title: 'Google Maps', sub: 'Cálculo dinâmico de taxa de entrega', val: 'AIzaSyD•••••••••••••••••••••••••-xKp4', status: 'Ativa' },
            { id: 'pay',   title: 'Gateway de pagamento', sub: 'Stripe · capturas via webhook', val: 'sk_live_•••••••••••••••••••••••••Qx8g', status: 'Ativa' },
            { id: 'nfce',  title: 'Emissor fiscal (PlugNotas)', sub: 'NFCe — envio assíncrono em fila', val: 'pn_prod_•••••••••••••••••••••••••tZ2', status: 'Pendente' },
            { id: 'ifood', title: 'iFood Webhook', sub: 'Recebimento de pedidos via API', val: 'Ainda não configurada', status: 'Não conectada', none: true },
          ].map(k => (
            <div key={k.id} className="key-row">
              <div>
                <div className="key-title">{k.title}</div>
                <div className="key-sub">{k.sub}</div>
                {!k.none && (
                  <div className="key-input">{k.val}</div>
                )}
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 6 }}>
                <span className={'badge ' + (k.status === 'Ativa' ? 'good' : k.status === 'Pendente' ? 'warn' : 'plain')}>{k.status}</span>
                <div style={{ display: 'flex', gap: 6 }}>
                  {!k.none && <button className="btn" style={{ padding: '4px 10px', fontSize: 11 }}>Ver</button>}
                  <button className="btn btn-primary" style={{ padding: '4px 10px', fontSize: 11 }}>{k.none ? 'Conectar' : 'Rotacionar'}</button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

window.AdminView = AdminView;
