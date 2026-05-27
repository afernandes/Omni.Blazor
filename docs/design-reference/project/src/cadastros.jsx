// ============================================================
// Módulo de Cadastros — Produtos, Pizzas/Sabores, Ingredientes,
// Clientes, Entregadores, Promoções, Zonas de Entrega
// ============================================================

const { useState: useC, useEffect: useCE, useMemo: useCM, useRef: useCR } = React;

// ——— shared helpers ———
function CadModal({ title, onClose, children, wide }) {
  useCE(() => {
    const esc = (e) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', esc);
    return () => window.removeEventListener('keydown', esc);
  }, [onClose]);
  return (
    <div className="cad-overlay" onClick={onClose}>
      <div className={'cad-modal ' + (wide ? 'wide' : '')} onClick={e => e.stopPropagation()}>
        <div className="cad-modal-head">
          <span className="cad-modal-title">{title}</span>
          <button className="btn btn-ghost btn-icon" onClick={onClose}><Icon name="x" size={16} /></button>
        </div>
        <div className="cad-modal-body">{children}</div>
      </div>
    </div>
  );
}

function CadField({ label, children, hint }) {
  return (
    <div className="cad-field">
      <label className="label">{label}</label>
      {children}
      {hint && <div className="cad-field-hint">{hint}</div>}
    </div>
  );
}

function CadRow({ children }) {
  return <div className="cad-row">{children}</div>;
}

function CadSection({ title, children }) {
  return (
    <div className="cad-section">
      <div className="cad-section-title">{title}</div>
      {children}
    </div>
  );
}

function EmptyState({ icon, title, sub, action }) {
  return (
    <div className="cad-empty">
      <Icon name={icon} size={36} />
      <div className="cad-empty-title">{title}</div>
      <div className="cad-empty-sub">{sub}</div>
      {action && <button className="btn btn-primary" style={{ marginTop: 12 }} onClick={action.fn}><Icon name="plus" size={13} />{action.label}</button>}
    </div>
  );
}

function SearchBar({ value, onChange, placeholder }) {
  return (
    <div className="input-with-ico" style={{ maxWidth: 320 }}>
      <span className="ico"><Icon name="search" size={15} /></span>
      <input className="input" placeholder={placeholder || 'Buscar…'} value={value} onChange={e => onChange(e.target.value)} />
    </div>
  );
}

function StatusBadge({ active }) {
  return <span className={'badge ' + (active ? 'good' : '')}>{active ? 'Ativo' : 'Inativo'}</span>;
}

// ============================================================
// 1. PRODUTOS
// ============================================================
const INIT_PRODUCTS = [
  { id: 'p1', name: 'Pizza Média',         cat: 'pizza',   price: 42.00, status: true,  size: 'M',  configurable: true,  desc: 'Pizza artesanal 6 fatias', ncm: '1905.90.90', slices: 6 },
  { id: 'p2', name: 'Pizza Grande',        cat: 'pizza',   price: 58.00, status: true,  size: 'G',  configurable: true,  desc: 'Pizza artesanal 8 fatias', ncm: '1905.90.90', slices: 8 },
  { id: 'p3', name: 'Pizza Família',       cat: 'pizza',   price: 72.00, status: true,  size: 'GG', configurable: true,  desc: 'Pizza artesanal 12 fatias',ncm: '1905.90.90', slices: 12 },
  { id: 'p4', name: 'Cheeseburger Duplo',  cat: 'burger',  price: 28.00, status: true,  size: '',   configurable: false, desc: 'Blend 180g + queijo duplo', ncm: '1602.32.00', slices: 0 },
  { id: 'p5', name: 'Burger da Casa',      cat: 'burger',  price: 32.00, status: true,  size: '',   configurable: false, desc: 'Blend especial da casa', ncm: '1602.32.00', slices: 0 },
  { id: 'p6', name: 'Coca-Cola 350ml',     cat: 'drink',   price: 7.50,  status: true,  size: '',   configurable: false, desc: 'Gelada, lata 350ml', ncm: '2202.10.00', slices: 0 },
  { id: 'p7', name: 'Guaraná 2L',          cat: 'drink',   price: 14.00, status: true,  size: '',   configurable: false, desc: 'Garrafa 2 litros', ncm: '2202.10.00', slices: 0 },
  { id: 'p8', name: 'Petit Gâteau',        cat: 'dessert', price: 22.00, status: false, size: '',   configurable: false, desc: 'Bolo quente recheado', ncm: '1905.90.90', slices: 0 },
  { id: 'p9', name: 'Batata Rústica',      cat: 'entry',   price: 24.00, status: true,  size: '',   configurable: false, desc: 'Porção 300g c/ molho', ncm: '2004.10.00', slices: 0 },
];

const CAT_OPTS = [
  { id: 'pizza', name: 'Pizza' }, { id: 'burger', name: 'Burger' },
  { id: 'drink', name: 'Bebida' }, { id: 'dessert', name: 'Sobremesa' },
  { id: 'entry', name: 'Entrada' }, { id: 'other', name: 'Outro' },
];

function ProductForm({ product, onSave, onClose }) {
  const blank = { id: null, name: '', cat: 'pizza', price: '', status: true, size: '', configurable: false, desc: '', ncm: '', slices: 0, cfop: '5102', maxFlavors: 2 };
  const [form, setForm] = useC(product ? { ...blank, ...product } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={product ? 'Editar produto' : 'Novo produto'} onClose={onClose} wide>
      <CadSection title="Dados básicos">
        <CadRow>
          <CadField label="Nome do produto *">
            <input className="input" value={form.name} onChange={e => set('name', e.target.value)} placeholder="Ex: Pizza Grande" />
          </CadField>
          <CadField label="Categoria *">
            <select className="input" value={form.cat} onChange={e => set('cat', e.target.value)}>
              {CAT_OPTS.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </CadField>
          <CadField label="Preço (R$) *">
            <input className="input" type="number" step="0.01" value={form.price} onChange={e => set('price', e.target.value)} placeholder="0,00" />
          </CadField>
        </CadRow>
        <CadField label="Descrição">
          <textarea className="input" rows={2} value={form.desc} onChange={e => set('desc', e.target.value)} placeholder="Descrição exibida no cardápio digital…" style={{ resize: 'vertical' }} />
        </CadField>
        <CadRow>
          <CadField label="Status">
            <div className="tweak-segmented">
              <button className={form.status ? 'active' : ''} onClick={() => set('status', true)}>Ativo</button>
              <button className={!form.status ? 'active' : ''} onClick={() => set('status', false)}>Inativo</button>
            </div>
          </CadField>
          <CadField label="Produto configurável?" hint="Habilita fluxo de personalização (pizza, burgers montáveis)">
            <div className="tweak-segmented">
              <button className={form.configurable ? 'active' : ''} onClick={() => set('configurable', true)}>Sim</button>
              <button className={!form.configurable ? 'active' : ''} onClick={() => set('configurable', false)}>Não</button>
            </div>
          </CadField>
        </CadRow>
        {form.configurable && (
          <CadRow>
            <CadField label="Código do tamanho" hint="M, G ou GG">
              <input className="input" value={form.size} onChange={e => set('size', e.target.value.toUpperCase())} placeholder="G" style={{ maxWidth: 80 }} />
            </CadField>
            <CadField label="Nº de fatias">
              <input className="input" type="number" value={form.slices} onChange={e => set('slices', +e.target.value)} placeholder="8" style={{ maxWidth: 100 }} />
            </CadField>
            <CadField label="Máx. sabores" hint="1=inteira · 2=meio-a-meio · 4=quatro">
              <select className="input" value={form.maxFlavors} onChange={e => set('maxFlavors', +e.target.value)} style={{ maxWidth: 120 }}>
                {[1,2,3,4].map(n => <option key={n} value={n}>{n} {n===1?'sabor':'sabores'}</option>)}
              </select>
            </CadField>
          </CadRow>
        )}
      </CadSection>

      <CadSection title="Dados fiscais">
        <CadRow>
          <CadField label="NCM" hint="Nomenclatura Comum do Mercosul">
            <input className="input" value={form.ncm} onChange={e => set('ncm', e.target.value)} placeholder="1905.90.90" />
          </CadField>
          <CadField label="CFOP">
            <input className="input" value={form.cfop} onChange={e => set('cfop', e.target.value)} placeholder="5102" />
          </CadField>
          <CadField label="Alíquota ICMS (%)">
            <input className="input" type="number" step="0.01" placeholder="0.00" style={{ maxWidth: 120 }} />
          </CadField>
        </CadRow>
      </CadSection>

      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar produto</button>
      </div>
    </CadModal>
  );
}

function ProductosView() {
  const [items, setItems] = useC(INIT_PRODUCTS);
  const [q, setQ] = useC('');
  const [catFilter, setCatFilter] = useC('all');
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);

  const filtered = useCM(() => items.filter(p =>
    (catFilter === 'all' || p.cat === catFilter) &&
    (!q || p.name.toLowerCase().includes(q.toLowerCase()))
  ), [items, catFilter, q]);

  const save = (form) => {
    if (form.id) setItems(prev => prev.map(p => p.id === form.id ? { ...p, ...form } : p));
    else setItems(prev => [...prev, { ...form, id: 'p' + Date.now() }]);
    setEditing(null); setCreating(false);
  };

  const toggle = (id) => setItems(prev => prev.map(p => p.id === id ? { ...p, status: !p.status } : p));
  const remove = (id) => setItems(prev => prev.filter(p => p.id !== id));

  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <SearchBar value={q} onChange={setQ} placeholder="Buscar produto…" />
        <div style={{ display: 'flex', gap: 6 }}>
          {['all', ...CAT_OPTS.map(c => c.id)].map(id => (
            <button key={id} className={'chip ' + (catFilter === id ? 'active' : '')} onClick={() => setCatFilter(id)}>
              {id === 'all' ? 'Todos' : CAT_OPTS.find(c => c.id === id)?.name}
            </button>
          ))}
        </div>
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Novo produto</button>
        </div>
      </div>

      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead>
            <tr>
              <th>Produto</th><th>Categoria</th><th>Preço</th><th>Tamanho</th><th>NCM</th><th>Status</th><th></th>
            </tr>
          </thead>
          <tbody>
            {filtered.map(p => (
              <tr key={p.id}>
                <td>
                  <div style={{ fontWeight: 600 }}>{p.name}</div>
                  <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>{p.desc}</div>
                </td>
                <td><span className="badge">{CAT_OPTS.find(c => c.id === p.cat)?.name || p.cat}</span></td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(parseFloat(p.price) || 0)}</span></td>
                <td>{p.configurable ? <span className="badge accent">{p.size} · {p.slices}fts</span> : <span style={{ color: 'var(--fg-soft)' }}>—</span>}</td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--fg-muted)' }}>{p.ncm || '—'}</span></td>
                <td><StatusBadge active={p.status} /></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(p)} title="Editar"><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => toggle(p.id)} title={p.status ? 'Desativar' : 'Ativar'}><Icon name={p.status ? 'x' : 'check'} size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => remove(p.id)} title="Excluir" style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--fg-soft)' }}>Nenhum produto encontrado.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {(creating || editing) && (
        <ProductForm product={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />
      )}
    </div>
  );
}

// ============================================================
// 2. SABORES DE PIZZA
// ============================================================
const INIT_FLAVORS = [
  { id: 'f1', name: 'Calabresa',           desc: 'Calabresa, cebola, azeitona, muçarela',       priceM: 42, priceG: 58, priceGG: 72, active: true  },
  { id: 'f2', name: 'Margherita',          desc: 'Muçarela de búfala, tomate, manjericão',      priceM: 46, priceG: 62, priceGG: 76, active: true  },
  { id: 'f3', name: 'Pepperoni',           desc: 'Molho, muçarela, pepperoni artesanal',        priceM: 48, priceG: 64, priceGG: 78, active: true  },
  { id: 'f4', name: 'Quatro Queijos',      desc: 'Muçarela, provolone, gorgonzola, parmesão',  priceM: 52, priceG: 68, priceGG: 82, active: true  },
  { id: 'f5', name: 'Portuguesa',          desc: 'Presunto, ovo, ervilha, palmito, muçarela',  priceM: 46, priceG: 62, priceGG: 76, active: true  },
  { id: 'f6', name: 'Frango c/ Catupiry',  desc: 'Frango desfiado, catupiry, milho, muçarela', priceM: 48, priceG: 64, priceGG: 78, active: true  },
  { id: 'f7', name: 'Chocolate Belga',     desc: 'Chocolate 70%, morangos frescos',            priceM: 54, priceG: 70, priceGG: 84, active: true  },
  { id: 'f8', name: 'Veggie (vegana)',      desc: 'Abobrinha, berinjela, tomate seco, rúcula',  priceM: 46, priceG: 62, priceGG: 76, active: false },
];

function FlavorForm({ flavor, onSave, onClose }) {
  const blank = { id: null, name: '', desc: '', priceM: '', priceG: '', priceGG: '', active: true };
  const [form, setForm] = useC(flavor ? { ...blank, ...flavor } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={flavor ? 'Editar sabor' : 'Novo sabor de pizza'} onClose={onClose} wide>
      <CadSection title="Informações do sabor">
        <CadField label="Nome do sabor *">
          <input className="input" value={form.name} onChange={e => set('name', e.target.value)} placeholder="Ex: Calabresa" />
        </CadField>
        <CadField label="Ingredientes / Descrição" hint="Exibido no cardápio digital">
          <textarea className="input" rows={2} value={form.desc} onChange={e => set('desc', e.target.value)} placeholder="Ex: Calabresa fatiada, cebola, azeitona preta, muçarela…" style={{ resize: 'vertical' }} />
        </CadField>
        <CadField label="Status">
          <div className="tweak-segmented" style={{ maxWidth: 200 }}>
            <button className={form.active ? 'active' : ''} onClick={() => set('active', true)}>Ativo</button>
            <button className={!form.active ? 'active' : ''} onClick={() => set('active', false)}>Inativo</button>
          </div>
        </CadField>
      </CadSection>

      <CadSection title="Preços por tamanho">
        <div className="cad-price-grid">
          {[['priceM','Média (M)','6 fatias'],['priceG','Grande (G)','8 fatias'],['priceGG','Família (GG)','12 fatias']].map(([k, label, hint]) => (
            <div key={k} className="cad-price-card">
              <div className="cad-price-size">{label}</div>
              <div className="cad-price-hint">{hint}</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8 }}>
                <span style={{ color: 'var(--fg-muted)', fontSize: 13 }}>R$</span>
                <input className="input" type="number" step="0.50" value={form[k]} onChange={e => set(k, e.target.value)} placeholder="0,00" style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }} />
              </div>
            </div>
          ))}
        </div>
        <div style={{ marginTop: 10, padding: '10px 14px', background: 'var(--bg-sunken)', borderRadius: 8, fontSize: 12, color: 'var(--fg-muted)' }}>
          <Icon name="sparkle" size={12} style={{ verticalAlign: -2, marginRight: 6 }} />
          Regra de preço: cobra pelo sabor mais caro em pizzas meio-a-meio. Configurável em Configurações do Tenant.
        </div>
      </CadSection>

      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar sabor</button>
      </div>
    </CadModal>
  );
}

function SaboresView() {
  const [items, setItems] = useC(INIT_FLAVORS);
  const [q, setQ] = useC('');
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);

  const filtered = useCM(() => items.filter(f =>
    !q || f.name.toLowerCase().includes(q.toLowerCase()) || f.desc.toLowerCase().includes(q.toLowerCase())
  ), [items, q]);

  const save = (form) => {
    if (form.id) setItems(prev => prev.map(f => f.id === form.id ? { ...f, ...form } : f));
    else setItems(prev => [...prev, { ...form, id: 'fl' + Date.now() }]);
    setEditing(null); setCreating(false);
  };

  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <SearchBar value={q} onChange={setQ} placeholder="Buscar sabor ou ingrediente…" />
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Novo sabor</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead>
            <tr><th>Sabor</th><th>Ingredientes</th><th>Média</th><th>Grande</th><th>Família</th><th>Status</th><th></th></tr>
          </thead>
          <tbody>
            {filtered.map(f => (
              <tr key={f.id}>
                <td><div style={{ fontWeight: 600 }}>{f.name}</div></td>
                <td style={{ color: 'var(--fg-muted)', fontSize: 12, maxWidth: 240 }}>{f.desc}</td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(f.priceM)}</span></td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(f.priceG)}</span></td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(f.priceGG)}</span></td>
                <td><StatusBadge active={f.active} /></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(f)}><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== f.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <FlavorForm flavor={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// 3. INGREDIENTES
// ============================================================
const INIT_INGREDIENTS = [
  { id: 'i1', name: 'Muçarela extra',           unit: 'g',      price: 6.00,  stock: 2400, min: 500, default: true,  removable: true  },
  { id: 'i2', name: 'Borda recheada catupiry',  unit: 'unid',   price: 12.00, stock: 80,   min: 20,  default: false, removable: false },
  { id: 'i3', name: 'Catupiry',                 unit: 'g',      price: 5.00,  stock: 1200, min: 200, default: false, removable: false },
  { id: 'i4', name: 'Bacon',                    unit: 'g',      price: 7.00,  stock: 800,  min: 100, default: false, removable: false },
  { id: 'i5', name: 'Cebola',                   unit: 'unid',   price: 0,     stock: 150,  min: 30,  default: true,  removable: true  },
  { id: 'i6', name: 'Azeitona preta',           unit: 'g',      price: 0,     stock: 600,  min: 100, default: true,  removable: true  },
  { id: 'i7', name: 'Orégano',                  unit: 'g',      price: 0,     stock: 200,  min: 50,  default: true,  removable: true  },
  { id: 'i8', name: 'Pão brioche',              unit: 'unid',   price: 0,     stock: 60,   min: 10,  default: true,  removable: false },
  { id: 'i9', name: 'Blend de carne 180g',      unit: 'g',      price: 0,     stock: 4000, min: 500, default: true,  removable: false },
];

function IngredientForm({ ingredient, onSave, onClose }) {
  const blank = { id: null, name: '', unit: 'g', price: 0, stock: 0, min: 0, default: false, removable: false };
  const [form, setForm] = useC(ingredient ? { ...blank, ...ingredient } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={ingredient ? 'Editar ingrediente' : 'Novo ingrediente'} onClose={onClose}>
      <CadSection title="Dados do ingrediente">
        <CadRow>
          <CadField label="Nome *">
            <input className="input" value={form.name} onChange={e => set('name', e.target.value)} placeholder="Ex: Muçarela extra" />
          </CadField>
          <CadField label="Unidade">
            <select className="input" value={form.unit} onChange={e => set('unit', e.target.value)} style={{ maxWidth: 100 }}>
              {['g','kg','ml','l','unid','fatia','porção'].map(u => <option key={u} value={u}>{u}</option>)}
            </select>
          </CadField>
          <CadField label="Preço extra (R$)" hint="0 = sem custo adicional">
            <input className="input" type="number" step="0.50" value={form.price} onChange={e => set('price', +e.target.value)} style={{ maxWidth: 120 }} />
          </CadField>
        </CadRow>
        <CadRow>
          <CadField label="Estoque atual">
            <input className="input" type="number" value={form.stock} onChange={e => set('stock', +e.target.value)} />
          </CadField>
          <CadField label="Estoque mínimo" hint="Alerta abaixo deste nível">
            <input className="input" type="number" value={form.min} onChange={e => set('min', +e.target.value)} />
          </CadField>
        </CadRow>
        <CadRow>
          <CadField label="Padrão no produto?" hint="Vem incluído por padrão">
            <div className="tweak-segmented"><button className={form.default ? 'active' : ''} onClick={() => set('default', true)}>Sim</button><button className={!form.default ? 'active' : ''} onClick={() => set('default', false)}>Não</button></div>
          </CadField>
          <CadField label="Cliente pode remover?">
            <div className="tweak-segmented"><button className={form.removable ? 'active' : ''} onClick={() => set('removable', true)}>Sim</button><button className={!form.removable ? 'active' : ''} onClick={() => set('removable', false)}>Não</button></div>
          </CadField>
        </CadRow>
      </CadSection>
      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar ingrediente</button>
      </div>
    </CadModal>
  );
}

function IngredientesView() {
  const [items, setItems] = useC(INIT_INGREDIENTS);
  const [q, setQ] = useC('');
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);

  const filtered = useCM(() => items.filter(i => !q || i.name.toLowerCase().includes(q.toLowerCase())), [items, q]);
  const save = (form) => {
    if (form.id) setItems(prev => prev.map(i => i.id === form.id ? { ...i, ...form } : i));
    else setItems(prev => [...prev, { ...form, id: 'i' + Date.now() }]);
    setEditing(null); setCreating(false);
  };

  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <SearchBar value={q} onChange={setQ} placeholder="Buscar ingrediente…" />
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Novo ingrediente</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead>
            <tr><th>Ingrediente</th><th>Unidade</th><th>Preço extra</th><th>Estoque</th><th>Padrão</th><th>Removível</th><th></th></tr>
          </thead>
          <tbody>
            {filtered.map(i => {
              const low = i.stock <= i.min;
              return (
                <tr key={i.id}>
                  <td style={{ fontWeight: 600 }}>{i.name}</td>
                  <td><span className="badge">{i.unit}</span></td>
                  <td><span style={{ fontFamily: 'var(--font-mono)' }}>{i.price > 0 ? '+' + BRL(i.price) : '—'}</span></td>
                  <td>
                    <span style={{ fontFamily: 'var(--font-mono)', color: low ? 'var(--danger)' : 'var(--fg)' }}>
                      {i.stock} {i.unit}
                    </span>
                    {low && <span className="badge danger" style={{ marginLeft: 6 }}>baixo</span>}
                  </td>
                  <td><span className={'badge ' + (i.default ? 'good' : '')}>{i.default ? 'Sim' : 'Não'}</span></td>
                  <td><span className={'badge ' + (i.removable ? 'accent' : '')}>{i.removable ? 'Sim' : 'Não'}</span></td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="btn btn-ghost btn-icon" onClick={() => setEditing(i)}><Icon name="sliders" size={14} /></button>
                      <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== i.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <IngredientForm ingredient={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// 4. CLIENTES
// ============================================================
const INIT_CUSTOMERS_CAD = [
  { id: 'c1', name: 'Marina Toledo',    phone: '(11) 98823-1204', email: 'marina@email.com', points: 842,  orders: 23, since: '2023-04-10', address: 'R. das Laranjeiras, 412 — Centro' },
  { id: 'c2', name: 'Rafael Okamoto',   phone: '(11) 99100-5512', email: '',                 points: 210,  orders: 7,  since: '2023-08-22', address: 'Av. Paulista, 2200 — Bela Vista' },
  { id: 'c3', name: 'Juliana Pires',    phone: '(11) 98401-7720', email: 'ju@email.com',     points: 1320, orders: 34, since: '2022-11-01', address: 'R. Aspicuelta, 88 — Vila Madalena' },
  { id: 'c4', name: 'Diego Salles',     phone: '(11) 97600-4401', email: '',                 points: 45,   orders: 2,  since: '2024-01-15', address: '' },
  { id: 'c5', name: 'Beatriz Monteiro', phone: '(11) 98250-9988', email: 'bea@email.com',    points: 580,  orders: 14, since: '2023-06-30', address: 'R. dos Três Irmãos, 120 — Morumbi' },
];

function CustomerForm({ customer, onSave, onClose }) {
  const blank = { id: null, name: '', phone: '', email: '', cpf: '', points: 0, address: '', number: '', complement: '', neighborhood: '', city: 'São Paulo', state: 'SP', zip: '' };
  const [form, setForm] = useC(customer ? { ...blank, ...customer } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={customer ? 'Editar cliente' : 'Novo cliente'} onClose={onClose} wide>
      <CadSection title="Dados pessoais">
        <CadRow>
          <CadField label="Nome completo *"><input className="input" value={form.name} onChange={e => set('name', e.target.value)} /></CadField>
          <CadField label="Telefone *"><input className="input" value={form.phone} onChange={e => set('phone', e.target.value)} placeholder="(11) 99999-0000" /></CadField>
        </CadRow>
        <CadRow>
          <CadField label="E-mail"><input className="input" type="email" value={form.email} onChange={e => set('email', e.target.value)} /></CadField>
          <CadField label="CPF"><input className="input" value={form.cpf || ''} onChange={e => set('cpf', e.target.value)} placeholder="000.000.000-00" /></CadField>
          <CadField label="Pontos de fidelidade">
            <input className="input" type="number" value={form.points} onChange={e => set('points', +e.target.value)} style={{ maxWidth: 140, fontFamily: 'var(--font-mono)', fontWeight: 600 }} />
          </CadField>
        </CadRow>
      </CadSection>
      <CadSection title="Endereço principal">
        <CadRow>
          <CadField label="CEP"><input className="input" value={form.zip || ''} onChange={e => set('zip', e.target.value)} placeholder="00000-000" style={{ maxWidth: 140 }} /></CadField>
          <CadField label="Rua / Logradouro"><input className="input" value={form.address} onChange={e => set('address', e.target.value)} /></CadField>
          <CadField label="Número"><input className="input" value={form.number || ''} onChange={e => set('number', e.target.value)} style={{ maxWidth: 100 }} /></CadField>
        </CadRow>
        <CadRow>
          <CadField label="Complemento"><input className="input" value={form.complement || ''} onChange={e => set('complement', e.target.value)} /></CadField>
          <CadField label="Bairro"><input className="input" value={form.neighborhood || ''} onChange={e => set('neighborhood', e.target.value)} /></CadField>
          <CadField label="Cidade"><input className="input" value={form.city || ''} onChange={e => set('city', e.target.value)} /></CadField>
        </CadRow>
      </CadSection>
      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar cliente</button>
      </div>
    </CadModal>
  );
}

function ClientesView() {
  const [items, setItems] = useC(INIT_CUSTOMERS_CAD);
  const [q, setQ] = useC('');
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);

  const filtered = useCM(() => items.filter(c =>
    !q || c.name.toLowerCase().includes(q.toLowerCase()) || c.phone.includes(q)
  ), [items, q]);

  const save = (form) => {
    if (form.id) setItems(prev => prev.map(c => c.id === form.id ? { ...c, ...form } : c));
    else setItems(prev => [...prev, { ...form, id: 'c' + Date.now(), orders: 0, since: new Date().toISOString().slice(0,10) }]);
    setEditing(null); setCreating(false);
  };

  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <SearchBar value={q} onChange={setQ} placeholder="Buscar por nome ou telefone…" />
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Novo cliente</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead><tr><th>Cliente</th><th>Telefone</th><th>Endereço</th><th>Pedidos</th><th>Pontos</th><th>Cliente desde</th><th></th></tr></thead>
          <tbody>
            {filtered.map(c => (
              <tr key={c.id}>
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div className="avatar" style={{ width: 32, height: 32, fontSize: 12 }}>{c.name.split(' ').map(w => w[0]).slice(0,2).join('')}</div>
                    <div>
                      <div style={{ fontWeight: 600 }}>{c.name}</div>
                      {c.email && <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>{c.email}</div>}
                    </div>
                  </div>
                </td>
                <td style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{c.phone}</td>
                <td style={{ color: 'var(--fg-muted)', fontSize: 12 }}>{c.address || '—'}</td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{c.orders}</span></td>
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <Icon name="sparkle" size={12} style={{ color: 'var(--accent)' }} />
                    <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{c.points}</span>
                  </div>
                </td>
                <td style={{ color: 'var(--fg-muted)', fontSize: 12 }}>{c.since}</td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(c)}><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== c.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <CustomerForm customer={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// 5. ENTREGADORES
// ============================================================
const INIT_MOTOBOYS = [
  { id: 'm1', name: 'Carlos Motos',   phone: '(11) 97000-1234', vehicle: 'Moto', plate: 'ABC-1234', active: true,  available: true  },
  { id: 'm2', name: 'Fernanda Lima',  phone: '(11) 96100-5678', vehicle: 'Moto', plate: 'DEF-5678', active: true,  available: false },
  { id: 'm3', name: 'João Correia',   phone: '(11) 98200-9012', vehicle: 'Bicicleta', plate: '—', active: true,  available: true  },
  { id: 'm4', name: 'Paulo Santos',   phone: '(11) 95300-3456', vehicle: 'Carro', plate: 'GHI-9012', active: false, available: false },
];

function MotoboyForm({ motoboy, onSave, onClose }) {
  const blank = { id: null, name: '', phone: '', vehicle: 'Moto', plate: '', active: true, available: true };
  const [form, setForm] = useC(motoboy ? { ...blank, ...motoboy } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={motoboy ? 'Editar entregador' : 'Novo entregador'} onClose={onClose}>
      <CadSection title="Dados do entregador">
        <CadRow>
          <CadField label="Nome *"><input className="input" value={form.name} onChange={e => set('name', e.target.value)} /></CadField>
          <CadField label="Telefone *"><input className="input" value={form.phone} onChange={e => set('phone', e.target.value)} placeholder="(11) 99999-0000" /></CadField>
        </CadRow>
        <CadRow>
          <CadField label="Veículo">
            <select className="input" value={form.vehicle} onChange={e => set('vehicle', e.target.value)}>
              {['Moto','Bicicleta','Carro','Patinete'].map(v => <option key={v} value={v}>{v}</option>)}
            </select>
          </CadField>
          <CadField label="Placa"><input className="input" value={form.plate} onChange={e => set('plate', e.target.value)} placeholder="AAA-0000" /></CadField>
        </CadRow>
        <CadRow>
          <CadField label="Status"><div className="tweak-segmented"><button className={form.active ? 'active' : ''} onClick={() => set('active', true)}>Ativo</button><button className={!form.active ? 'active' : ''} onClick={() => set('active', false)}>Inativo</button></div></CadField>
          <CadField label="Disponível agora"><div className="tweak-segmented"><button className={form.available ? 'active' : ''} onClick={() => set('available', true)}>Sim</button><button className={!form.available ? 'active' : ''} onClick={() => set('available', false)}>Não</button></div></CadField>
        </CadRow>
      </CadSection>
      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar entregador</button>
      </div>
    </CadModal>
  );
}

function EntregadoresView() {
  const [items, setItems] = useC(INIT_MOTOBOYS);
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);
  const save = (form) => {
    if (form.id) setItems(prev => prev.map(m => m.id === form.id ? { ...m, ...form } : m));
    else setItems(prev => [...prev, { ...form, id: 'm' + Date.now() }]);
    setEditing(null); setCreating(false);
  };
  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Novo entregador</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead><tr><th>Entregador</th><th>Telefone</th><th>Veículo</th><th>Placa</th><th>Status</th><th>Disponível</th><th></th></tr></thead>
          <tbody>
            {items.map(m => (
              <tr key={m.id}>
                <td style={{ fontWeight: 600 }}>{m.name}</td>
                <td style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{m.phone}</td>
                <td><span className="badge">{m.vehicle}</span></td>
                <td style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{m.plate}</td>
                <td><StatusBadge active={m.active} /></td>
                <td><span className={'badge ' + (m.available ? 'good' : 'warn')}>{m.available ? 'Sim' : 'Em entrega'}</span></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(m)}><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== m.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <MotoboyForm motoboy={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// 6. PROMOÇÕES
// ============================================================
const PROMO_TYPES = [
  { id: 'percent', name: 'Desconto %' },
  { id: 'fixed', name: 'Desconto fixo (R$)' },
  { id: 'buyget', name: 'Compre e Ganhe' },
  { id: 'day', name: 'Promoção do dia' },
];
const DAYS = ['Dom','Seg','Ter','Qua','Qui','Sex','Sáb'];

const INIT_PROMOS = [
  { id: 'pr1', name: 'Terça da Calabresa', type: 'percent', discount: 20, day: 2, product: 'Calabresa', active: true,  desc: '20% OFF na Pizza Calabresa às terças' },
  { id: 'pr2', name: 'Combo Pizza + Refri', type: 'buyget', buyQty: 1, buyProduct: 'Pizza Grande', getQty: 1, getProduct: 'Coca-Cola 350ml', active: true, desc: 'Compre 1 Pizza Grande, Ganhe 1 Coca' },
  { id: 'pr3', name: 'Frete grátis Sexta', type: 'fixed', discount: 8, day: 5, active: false, desc: 'Taxa de entrega grátis às sextas' },
];

function PromoForm({ promo, onSave, onClose }) {
  const blank = { id: null, name: '', type: 'percent', discount: '', day: '', buyQty: 1, buyProduct: '', getQty: 1, getProduct: '', active: true, desc: '' };
  const [form, setForm] = useC(promo ? { ...blank, ...promo } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  return (
    <CadModal title={promo ? 'Editar promoção' : 'Nova promoção'} onClose={onClose} wide>
      <CadSection title="Configuração">
        <CadRow>
          <CadField label="Nome da promoção *"><input className="input" value={form.name} onChange={e => set('name', e.target.value)} placeholder="Ex: Terça da Calabresa" /></CadField>
          <CadField label="Tipo *">
            <select className="input" value={form.type} onChange={e => set('type', e.target.value)}>
              {PROMO_TYPES.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </CadField>
          <CadField label="Status"><div className="tweak-segmented"><button className={form.active ? 'active' : ''} onClick={() => set('active', true)}>Ativa</button><button className={!form.active ? 'active' : ''} onClick={() => set('active', false)}>Inativa</button></div></CadField>
        </CadRow>
        <CadField label="Descrição interna"><input className="input" value={form.desc} onChange={e => set('desc', e.target.value)} placeholder="Resumo para a equipe…" /></CadField>
      </CadSection>

      {(form.type === 'percent' || form.type === 'fixed') && (
        <CadSection title="Desconto">
          <CadRow>
            <CadField label={form.type === 'percent' ? 'Desconto (%)' : 'Desconto (R$)'}>
              <input className="input" type="number" step="0.5" value={form.discount} onChange={e => set('discount', e.target.value)} style={{ maxWidth: 140 }} />
            </CadField>
            <CadField label="Dia da semana (opcional)">
              <select className="input" value={form.day ?? ''} onChange={e => set('day', e.target.value)}>
                <option value="">Todos os dias</option>
                {DAYS.map((d, i) => <option key={i} value={i}>{d}</option>)}
              </select>
            </CadField>
            <CadField label="Produto alvo (opcional)">
              <input className="input" value={form.product || ''} onChange={e => set('product', e.target.value)} placeholder="Deixe em branco para qualquer produto" />
            </CadField>
          </CadRow>
        </CadSection>
      )}

      {form.type === 'buyget' && (
        <CadSection title="Compre e Ganhe">
          <CadRow>
            <CadField label="Compre (qtd)"><input className="input" type="number" value={form.buyQty} onChange={e => set('buyQty', +e.target.value)} style={{ maxWidth: 100 }} /></CadField>
            <CadField label="Produto obrigatório"><input className="input" value={form.buyProduct} onChange={e => set('buyProduct', e.target.value)} placeholder="Ex: Pizza Grande" /></CadField>
          </CadRow>
          <CadRow>
            <CadField label="Ganhe (qtd)"><input className="input" type="number" value={form.getQty} onChange={e => set('getQty', +e.target.value)} style={{ maxWidth: 100 }} /></CadField>
            <CadField label="Produto brinde"><input className="input" value={form.getProduct} onChange={e => set('getProduct', e.target.value)} placeholder="Ex: Coca-Cola 350ml" /></CadField>
          </CadRow>
          <div style={{ padding: '10px 14px', background: 'var(--accent-softer)', borderRadius: 8, fontSize: 12, color: 'var(--accent)', marginTop: 8 }}>
            <Icon name="sparkle" size={12} style={{ verticalAlign: -2, marginRight: 6 }} />
            No PDV, ao atingir a condição, aparece um modal: "Parabéns! Você ganhou {form.getQty}× {form.getProduct || '…'}. Escolha o sabor."
          </div>
        </CadSection>
      )}

      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar promoção</button>
      </div>
    </CadModal>
  );
}

function PromocoesView() {
  const [items, setItems] = useC(INIT_PROMOS);
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);
  const save = (form) => {
    if (form.id) setItems(prev => prev.map(p => p.id === form.id ? { ...p, ...form } : p));
    else setItems(prev => [...prev, { ...form, id: 'pr' + Date.now() }]);
    setEditing(null); setCreating(false);
  };
  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Nova promoção</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead><tr><th>Promoção</th><th>Tipo</th><th>Regra</th><th>Status</th><th></th></tr></thead>
          <tbody>
            {items.map(p => (
              <tr key={p.id}>
                <td>
                  <div style={{ fontWeight: 600 }}>{p.name}</div>
                  <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>{p.desc}</div>
                </td>
                <td><span className="badge">{PROMO_TYPES.find(t => t.id === p.type)?.name || p.type}</span></td>
                <td style={{ fontSize: 12, color: 'var(--fg-muted)' }}>
                  {p.type === 'percent' && `${p.discount}% OFF ${p.day != null && p.day !== '' ? '· ' + DAYS[p.day] : ''}`}
                  {p.type === 'fixed' && `R$ ${p.discount} OFF ${p.day != null && p.day !== '' ? '· ' + DAYS[p.day] : ''}`}
                  {p.type === 'buyget' && `${p.buyQty}× ${p.buyProduct} → Ganhe ${p.getQty}× ${p.getProduct}`}
                </td>
                <td><StatusBadge active={p.active} /></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(p)}><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== p.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <PromoForm promo={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// 7. ZONAS DE ENTREGA
// ============================================================
const INIT_ZONES = [
  { id: 'z1', name: 'Centro',         fee: 5.00,  eta: 30, active: true,  neighborhoods: 'Centro, Sé, República' },
  { id: 'z2', name: 'Vila Madalena',  fee: 8.00,  eta: 40, active: true,  neighborhoods: 'Vila Madalena, Pinheiros, Jardins' },
  { id: 'z3', name: 'Pinheiros',      fee: 9.00,  eta: 45, active: true,  neighborhoods: 'Pinheiros, Perdizes, Sumaré' },
  { id: 'z4', name: 'Morumbi',        fee: 14.00, eta: 55, active: false, neighborhoods: 'Morumbi, Vila Andrade' },
];

function ZoneForm({ zone, onSave, onClose }) {
  const blank = { id: null, name: '', fee: '', eta: 45, active: true, neighborhoods: '' };
  const [form, setForm] = useC(zone ? { ...blank, ...zone } : blank);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));
  return (
    <CadModal title={zone ? 'Editar zona' : 'Nova zona de entrega'} onClose={onClose}>
      <CadSection title="Configuração da zona">
        <CadField label="Nome da zona *"><input className="input" value={form.name} onChange={e => set('name', e.target.value)} placeholder="Ex: Centro" /></CadField>
        <CadRow>
          <CadField label="Taxa de entrega (R$) *"><input className="input" type="number" step="0.5" value={form.fee} onChange={e => set('fee', e.target.value)} /></CadField>
          <CadField label="Tempo estimado (min)"><input className="input" type="number" value={form.eta} onChange={e => set('eta', +e.target.value)} style={{ maxWidth: 120 }} /></CadField>
          <CadField label="Status"><div className="tweak-segmented"><button className={form.active ? 'active' : ''} onClick={() => set('active', true)}>Ativa</button><button className={!form.active ? 'active' : ''} onClick={() => set('active', false)}>Inativa</button></div></CadField>
        </CadRow>
        <CadField label="Bairros cobertos" hint="Separe por vírgula">
          <textarea className="input" rows={3} value={form.neighborhoods} onChange={e => set('neighborhoods', e.target.value)} placeholder="Centro, Sé, República…" style={{ resize: 'vertical' }} />
        </CadField>
      </CadSection>
      <div className="cad-modal-foot">
        <button className="btn" onClick={onClose}>Cancelar</button>
        <button className="btn btn-primary" onClick={() => onSave(form)}><Icon name="check" size={13} /> Salvar zona</button>
      </div>
    </CadModal>
  );
}

function ZonasView() {
  const [items, setItems] = useC(INIT_ZONES);
  const [editing, setEditing] = useC(null);
  const [creating, setCreating] = useC(false);
  const save = (form) => {
    if (form.id) setItems(prev => prev.map(z => z.id === form.id ? { ...z, ...form } : z));
    else setItems(prev => [...prev, { ...form, id: 'z' + Date.now() }]);
    setEditing(null); setCreating(false);
  };
  return (
    <div className="cad-view">
      <div className="cad-toolbar">
        <div style={{ marginLeft: 'auto' }}>
          <button className="btn btn-primary" onClick={() => setCreating(true)}><Icon name="plus" size={13} /> Nova zona</button>
        </div>
      </div>
      <div className="cad-table-wrap">
        <table className="cad-table">
          <thead><tr><th>Zona</th><th>Bairros</th><th>Taxa</th><th>Tempo</th><th>Status</th><th></th></tr></thead>
          <tbody>
            {items.map(z => (
              <tr key={z.id}>
                <td style={{ fontWeight: 600 }}>{z.name}</td>
                <td style={{ color: 'var(--fg-muted)', fontSize: 12, maxWidth: 260 }}>{z.neighborhoods}</td>
                <td><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{BRL(z.fee)}</span></td>
                <td style={{ fontFamily: 'var(--font-mono)', color: 'var(--fg-muted)' }}>{z.eta} min</td>
                <td><StatusBadge active={z.active} /></td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="btn btn-ghost btn-icon" onClick={() => setEditing(z)}><Icon name="sliders" size={14} /></button>
                    <button className="btn btn-ghost btn-icon" onClick={() => setItems(prev => prev.filter(x => x.id !== z.id))} style={{ color: 'var(--danger)' }}><Icon name="trash" size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(creating || editing) && <ZoneForm zone={editing} onSave={save} onClose={() => { setEditing(null); setCreating(false); }} />}
    </div>
  );
}

// ============================================================
// ROOT: CadastrosView — sub-navegação interna
// ============================================================
const CAD_SECTIONS = [
  { id: 'produtos',     label: 'Produtos',        icon: 'menu',    sub: 'Todos os itens do cardápio' },
  { id: 'sabores',      label: 'Sabores de Pizza', icon: 'pizza',   sub: 'Sabores e preços por tamanho' },
  { id: 'ingredientes', label: 'Ingredientes',     icon: 'flame',   sub: 'Matérias-primas e adicionais' },
  { id: 'clientes',     label: 'Clientes',         icon: 'user',    sub: 'Base de clientes e fidelidade' },
  { id: 'entregadores', label: 'Entregadores',     icon: 'truck',   sub: 'Motoboys e disponibilidade' },
  { id: 'promocoes',    label: 'Promoções',        icon: 'sparkle', sub: 'Descontos e Compre & Ganhe' },
  { id: 'zonas',        label: 'Zonas de Entrega', icon: 'store',   sub: 'Regiões e taxas de delivery' },
];

function CadastrosView() {
  const [section, setSection] = useC('produtos');
  const active = CAD_SECTIONS.find(s => s.id === section);

  return (
    <div className="cad-shell">
      <div className="cad-subnav">
        {CAD_SECTIONS.map(s => (
          <button key={s.id}
            className={'cad-subnav-item ' + (section === s.id ? 'active' : '')}
            onClick={() => setSection(s.id)}>
            <Icon name={s.icon} size={15} />
            <span>{s.label}</span>
          </button>
        ))}
      </div>
      <div className="cad-content">
        <div className="view-head" style={{ padding: '16px 24px 12px', marginBottom: 0 }}>
          <div>
            <h2 className="view-title" style={{ fontSize: 17 }}>{active?.label}</h2>
            <p className="view-sub">{active?.sub}</p>
          </div>
        </div>
        {section === 'produtos'     && <ProductosView />}
        {section === 'sabores'      && <SaboresView />}
        {section === 'ingredientes' && <IngredientesView />}
        {section === 'clientes'     && <ClientesView />}
        {section === 'entregadores' && <EntregadoresView />}
        {section === 'promocoes'    && <PromocoesView />}
        {section === 'zonas'        && <ZonasView />}
      </div>
    </div>
  );
}

window.CadastrosView = CadastrosView;
