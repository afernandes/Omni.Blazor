// ——————————————————————————————————————————
// Forneria — Onboarding: all step components + App
// ——————————————————————————————————————————

const { useState, useEffect, useRef } = React;

// ——— tiny helpers ———
function CheckIco({ size = 11 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5 12l5 5L20 7" />
    </svg>
  );
}
function ChevronDown({ size = 12 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
      <path d="M6 9l6 6 6-6" />
    </svg>
  );
}
function Toggle({ on, onClick, label }) {
  return (
    <button type="button" role="switch" aria-checked={on} aria-label={label}
      className={"ob-toggle" + (on ? " on" : "")} onClick={onClick} />
  );
}

// ——— Progress bar ———
const STEPS_META = [
  { num: 1, label: "Boas-vindas" },
  { num: 2, label: "Perfil" },
  { num: 3, label: "Cardápio" },
  { num: 4, label: "Operacional" },
  { num: 5, label: "Equipe" },
  { num: 6, label: "Concluído" },
];

function ProgressBar({ current }) {
  return (
    <header className="ob-header">
      <a href="login.html" className="ob-brand">
        <div className="brand-mark">F</div>
        <span>Forneria</span>
      </a>
      <nav className="ob-stepper" aria-label="Progresso">
        {STEPS_META.map((s, i) => {
          const done = current > s.num;
          const active = current === s.num;
          return (
            <React.Fragment key={s.num}>
              <div className={"ob-step" + (active ? " active" : "") + (done ? " done" : "")}>
                <div className="ob-step-circle">
                  {done ? <CheckIco size={11} /> : s.num}
                </div>
                <span className="ob-step-label">{s.label}</span>
              </div>
              {i < STEPS_META.length - 1 && (
                <div className={"ob-step-line" + (done ? " done" : "")} />
              )}
            </React.Fragment>
          );
        })}
      </nav>
      <div className="ob-header-right">
        <span className="ob-step-counter">{current} / 6</span>
      </div>
    </header>
  );
}

// ——— Footer ———
function StepFooter({ step, onBack, onNext, onSkip, selectedModels, totalItems }) {
  const count = selectedModels.length;
  return (
    <footer className="ob-footer">
      <div className="ob-footer-info">
        {step === 3 && count === 0 && (
          <span style={{ color: "var(--fg-soft)" }}>Nenhum modelo selecionado</span>
        )}
        {step === 3 && count > 0 && (
          <span>
            <strong style={{ color: "var(--accent)" }}>{count} {count === 1 ? "modelo" : "modelos"} selecionado{count !== 1 ? "s" : ""}</strong>
            {totalItems > 0 ? ` — ≈ ${totalItems} itens serão criados` : " — cardápio em branco"}
          </span>
        )}
      </div>
      <div className="ob-footer-actions">
        {step === 5 && (
          <button className="btn btn-ghost" onClick={onSkip}>Pular por agora</button>
        )}
        {step > 1 && (
          <button className="btn" onClick={onBack}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round"><path d="M19 12H5M11 6l-6 6 6 6" /></svg>
            Voltar
          </button>
        )}
        <button className="btn btn-primary btn-lg" onClick={onNext}>
          {step === 1 ? "Vamos começar" : step === 5 ? "Concluir" : "Continuar"}
          {step !== 5 && (
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
          )}
        </button>
      </div>
    </footer>
  );
}

// ——————————————————————————————————————
// STEP 1 — Boas-vindas
// ——————————————————————————————————————
function Step1({ data }) {
  const steps_list = [
    { n: 1, ico: "🏪", t: "Perfil do estabelecimento", d: "Nome, endereço, logo e cor da marca" },
    { n: 2, ico: "📋", t: "Modelos de cardápio", d: "Escolha entre 30+ tipos — editável depois" },
    { n: 3, ico: "⚙️", t: "Configurações operacionais", d: "Horários, mesas, delivery e retirada" },
    { n: 4, ico: "👥", t: "Equipe", d: "Convide colaboradores por e-mail (opcional)" },
    { n: 5, ico: "🎉", t: "Pronto!", d: "Acesse o dashboard e comece a operar" },
  ];
  return (
    <div className="ob-card ob-step1-layout">
      <div className="ob-step1-text">
        <span className="ob-eyebrow"><span className="ob-eyebrow-dot"></span>Configuração inicial</span>
        <h1 className="ob-title" style={{ marginTop: "12px" }}>
          Olá, {data.nomeUsuario}!<br />
          Vamos configurar o <em>{data.nomefantasia || "seu estabelecimento"}</em>
        </h1>
        <p className="ob-sub">São apenas 5 passos rápidos. Tudo pode ser editado a qualquer momento.</p>
        <div className="ob-checklist">
          {steps_list.map(item => (
            <div key={item.n} className="ob-checklist-item">
              <div className="ob-checklist-num">{item.n}</div>
              <div>
                <div className="ob-checklist-title">{item.ico} {item.t}</div>
                <div className="ob-checklist-desc">{item.d}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
      <div className="ob-step1-visual">
        <div className="ob-mosaic">
          {["🍕","🍔","🍜","🍗","🥗","🍺","🍰","☕","🌮","🥩","🍦","🧋"].map((e, i) => (
            <div key={i} className="ob-mosaic-tile" style={{ animationDelay: `${i * 80}ms` }}>{e}</div>
          ))}
        </div>
        <div className="ob-step1-metric">
          <div className="ob-step1-metric-val">≈ 12 min</div>
          <div className="ob-step1-metric-lbl">Tempo médio de setup</div>
        </div>
      </div>
    </div>
  );
}

// ——————————————————————————————————————
// STEP 2 — Perfil
// ——————————————————————————————————————
function Step2({ data, setData }) {
  const set = (k, v) => setData(d => ({ ...d, [k]: v }));
  const [cepLoading, setCepLoading] = useState(false);

  function handleCEP(raw) {
    const masked = raw.replace(/\D/g,"").replace(/^(\d{5})(\d{0,3}).*/,"$1-$2").slice(0, 9);
    set("cep", masked);
    const digits = raw.replace(/\D/g,"");
    if (digits.length === 8) {
      setCepLoading(true);
      setTimeout(() => {
        setData(d => ({ ...d, rua: "Rua das Flores", bairro: "Centro", cidade: "São Paulo", uf: "SP" }));
        setCepLoading(false);
      }, 700);
    }
  }

  const maskCNPJ = v => v.replace(/\D/g,"").replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2}).*/,"$1.$2.$3/$4-$5").slice(0,18);
  const maskPhone = v => v.replace(/\D/g,"").replace(/^(\d{2})(\d{5})(\d{4}).*/,"($1) $2-$3").slice(0,15);

  return (
    <div className="ob-card">
      <span className="ob-eyebrow"><span className="ob-eyebrow-dot"></span>Passo 2 de 5</span>
      <h2 className="ob-title" style={{ marginTop: "12px" }}>Perfil do estabelecimento</h2>
      <p className="ob-sub">Estas informações aparecem nos recibos e no cardápio digital.</p>
      <div className="ob-form">
        <div className="ob-row2">
          <div className="ob-field">
            <label className="label">Nome fantasia</label>
            <input className="input" placeholder="Ex: Pizzaria do João" value={data.nomefantasia} onChange={e => set("nomefantasia", e.target.value)} />
          </div>
          <div className="ob-field">
            <label className="label">CNPJ <span className="ob-optional">opcional</span></label>
            <input className="input" placeholder="00.000.000/0001-00" value={data.cnpj} onChange={e => set("cnpj", maskCNPJ(e.target.value))} />
          </div>
        </div>
        <div className="ob-row-cep">
          <div className="ob-field">
            <label className="label">CEP {cepLoading && <span className="ob-cep-loading">buscando…</span>}</label>
            <input className="input" placeholder="00000-000" value={data.cep} onChange={e => handleCEP(e.target.value)} maxLength={9} />
          </div>
          <div className="ob-field" style={{ flex: 2 }}>
            <label className="label">Logradouro</label>
            <input className="input" placeholder="Rua, Av…" value={data.rua} onChange={e => set("rua", e.target.value)} />
          </div>
        </div>
        <div className="ob-row3">
          <div className="ob-field" style={{ flex: 2 }}>
            <label className="label">Bairro</label>
            <input className="input" placeholder="Bairro" value={data.bairro} onChange={e => set("bairro", e.target.value)} />
          </div>
          <div className="ob-field" style={{ flex: 2 }}>
            <label className="label">Cidade</label>
            <input className="input" placeholder="Cidade" value={data.cidade} onChange={e => set("cidade", e.target.value)} />
          </div>
          <div className="ob-field" style={{ flex: 0, minWidth: "72px" }}>
            <label className="label">UF</label>
            <input className="input" placeholder="SP" value={data.uf} onChange={e => set("uf", e.target.value.toUpperCase())} maxLength={2} />
          </div>
        </div>
        <div className="ob-field">
          <label className="label">Telefone / WhatsApp</label>
          <input className="input" placeholder="(11) 99999-9999" value={data.telefone} onChange={e => set("telefone", maskPhone(e.target.value))} />
        </div>
        <div className="ob-field">
          <label className="label">Logotipo</label>
          <div className="ob-upload">
            <div className="ob-upload-ico">📎</div>
            <div className="ob-upload-title">Arraste ou clique para enviar</div>
            <div className="ob-upload-sub">PNG, JPG ou SVG · máx. 2 MB</div>
          </div>
        </div>
        <div className="ob-field">
          <label className="label">Cor principal da marca</label>
          <div className="ob-swatches">
            {BRAND_COLORS.map(c => (
              <button key={c.val} type="button" title={c.label}
                className={"ob-swatch" + (data.corMarca === c.val ? " active" : "")}
                style={{ background: c.val }}
                onClick={() => set("corMarca", c.val)} />
            ))}
          </div>
          <p className="ob-field-hint">Usada na personalização do cardápio digital</p>
        </div>
      </div>
    </div>
  );
}

// ——————————————————————————————————————
// STEP 3 — Modelos de Cardápio
// ——————————————————————————————————————
function ModelCard({ model, isSelected, isDisabled, onToggle }) {
  const [open, setOpen] = useState(false);
  return (
    <button type="button"
      className={"ob-model" + (isSelected ? " selected" : "") + (isDisabled ? " disabled" : "") + (model.exclusive ? " exclusive" : "")}
      onClick={() => !isDisabled && onToggle(model.id)}>
      {/* checkmark badge */}
      <div className="ob-model-check"><CheckIco /></div>
      <div className="ob-model-top">
        <span className="ob-model-emoji">{model.emoji}</span>
        <div style={{ flex: 1 }}>
          <div className="ob-model-name">{model.name}</div>
          <div className="ob-model-desc">{model.desc}</div>
        </div>
      </div>
      <div className="ob-model-foot">
        {model.count > 0
          ? <span className="ob-model-count">≈ {model.count} itens</span>
          : <span className="ob-model-count" style={{ opacity: 0.5 }}>—</span>
        }
        {model.preview.length > 0 && (
          <button type="button" className="ob-preview-btn"
            onClick={e => { e.stopPropagation(); setOpen(o => !o); }}>
            {open ? "Ocultar" : "Ver prévia"} <ChevronDown size={11} />
          </button>
        )}
      </div>
      {open && (
        <div className="ob-preview-body" onClick={e => e.stopPropagation()}>
          <div className="ob-preview-label">Exemplos</div>
          <div className="ob-preview-items">{model.preview.join(" · ")}</div>
          {model.cats.length > 0 && (
            <div className="ob-preview-cats">
              {model.cats.map(c => <span key={c} className="ob-preview-cat">{c}</span>)}
            </div>
          )}
        </div>
      )}
    </button>
  );
}

function Step3({ data, setData }) {
  const allItems = MENU_MODELS.flatMap(g => g.items);
  function toggle(id) {
    const model = allItems.find(m => m.id === id);
    const sel = new Set(data.selectedModels);
    if (model.exclusive) {
      sel.has(id) ? sel.delete(id) : (sel.clear(), sel.add(id));
    } else {
      sel.delete("zero");
      sel.has(id) ? sel.delete(id) : sel.add(id);
    }
    setData(d => ({ ...d, selectedModels: [...sel] }));
  }
  const isZero = data.selectedModels.includes("zero");

  return (
    <div className="ob-wide">
      <span className="ob-eyebrow"><span className="ob-eyebrow-dot"></span>Passo 3 de 5 · Etapa principal</span>
      <h2 className="ob-title" style={{ marginTop: "12px" }}>Como é o seu cardápio?</h2>
      <p className="ob-sub">Selecione os modelos que fazem sentido para o seu estabelecimento.<br />Você pode escolher quantos quiser — tudo pode ser editado depois.</p>
      {MENU_MODELS.map(group => (
        <div key={group.group}>
          <div className="ob-group-hd">{group.group}</div>
          <div className="ob-models-grid">
            {group.items.map(model => (
              <ModelCard key={model.id} model={model}
                isSelected={data.selectedModels.includes(model.id)}
                isDisabled={isZero && !model.exclusive}
                onToggle={toggle} />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

// ——————————————————————————————————————
// STEP 4 — Operacional
// ——————————————————————————————————————
function Step4({ data, setData }) {
  const set = (k, v) => setData(d => ({ ...d, [k]: v }));
  const setH = (id, field, val) => setData(d => ({ ...d, horarios: { ...d.horarios, [id]: { ...d.horarios[id], [field]: val } } }));

  return (
    <div className="ob-card">
      <span className="ob-eyebrow"><span className="ob-eyebrow-dot"></span>Passo 4 de 5</span>
      <h2 className="ob-title" style={{ marginTop: "12px" }}>Configurações operacionais</h2>
      <p className="ob-sub">Defina como e quando você opera. Tudo pode ser alterado depois.</p>
      <div className="ob-form">
        <div>
          <div className="ob-section-hd">Horário de funcionamento</div>
          <div className="ob-hours">
            {DAYS.map(day => {
              const h = data.horarios[day.id];
              return (
                <div key={day.id} className={"ob-day-row" + (h.on ? " on" : "")}>
                  <span className="ob-day-name">{day.short}</span>
                  <Toggle on={h.on} onClick={() => setH(day.id, "on", !h.on)} label={day.label} />
                  <div className={"ob-day-times" + (h.on ? "" : " off")}>
                    <input type="time" className="input" style={{ padding: "7px 10px", fontSize: "13px", width: "110px" }}
                      value={h.ab} onChange={e => setH(day.id, "ab", e.target.value)} disabled={!h.on} />
                    <span style={{ color: "var(--fg-soft)", fontSize: "12px" }}>até</span>
                    <input type="time" className="input" style={{ padding: "7px 10px", fontSize: "13px", width: "110px" }}
                      value={h.fe} onChange={e => setH(day.id, "fe", e.target.value)} disabled={!h.on} />
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        <div className="ob-row2">
          <div className="ob-field">
            <label className="label">Número de mesas / comandas</label>
            <input type="number" className="input" min={0} max={999}
              value={data.mesas} onChange={e => set("mesas", Number(e.target.value))} />
          </div>
          <div className="ob-field">
            <label className="label">Tempo médio de preparo — <strong style={{ fontFamily: "var(--font-mono)", color: "var(--accent)" }}>{data.tempoPrep} min</strong></label>
            <input type="range" min={5} max={120} step={5} className="ob-range"
              value={data.tempoPrep} onChange={e => set("tempoPrep", Number(e.target.value))} />
          </div>
        </div>

        <div>
          <div className="ob-section-hd">Canais de atendimento</div>
          <div className="ob-modes">
            <div>
              <div className={"ob-mode-row" + (data.delivery ? " active" : "")} onClick={() => set("delivery", !data.delivery)}>
                <div className="ob-mode-ico">🛵</div>
                <div style={{ flex: 1 }}>
                  <div className="ob-mode-title">Delivery</div>
                  <div className="ob-mode-sub">Pedidos para entrega em domicílio</div>
                </div>
                <Toggle on={data.delivery} onClick={e => { e.stopPropagation(); set("delivery", !data.delivery); }} label="Delivery" />
              </div>
              {data.delivery && (
                <div className="ob-mode-expand">
                  <div className="ob-row2">
                    <div className="ob-field">
                      <label className="label">Taxa de entrega (R$)</label>
                      <input type="number" className="input" min={0} placeholder="0,00"
                        value={data.taxaEntrega} onChange={e => set("taxaEntrega", e.target.value)} />
                    </div>
                    <div className="ob-field">
                      <label className="label">Raio de entrega (km)</label>
                      <input type="number" className="input" min={1} placeholder="5"
                        value={data.raio} onChange={e => set("raio", e.target.value)} />
                    </div>
                  </div>
                </div>
              )}
            </div>
            <div className={"ob-mode-row" + (data.retirada ? " active" : "")} onClick={() => set("retirada", !data.retirada)}>
              <div className="ob-mode-ico">🏪</div>
              <div style={{ flex: 1 }}>
                <div className="ob-mode-title">Retirada no balcão</div>
                <div className="ob-mode-sub">Cliente retira pessoalmente no local</div>
              </div>
              <Toggle on={data.retirada} onClick={e => { e.stopPropagation(); set("retirada", !data.retirada); }} label="Retirada" />
            </div>
            <div className={"ob-mode-row" + (data.local ? " active" : "")} onClick={() => set("local", !data.local)}>
              <div className="ob-mode-ico">🪑</div>
              <div style={{ flex: 1 }}>
                <div className="ob-mode-title">Consumo no local</div>
                <div className="ob-mode-sub">Mesas e comandas no salão</div>
              </div>
              <Toggle on={data.local} onClick={e => { e.stopPropagation(); set("local", !data.local); }} label="Consumo no local" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ——————————————————————————————————————
// STEP 5 — Equipe
// ——————————————————————————————————————
function Step5({ data, setData }) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Gerente");

  function add() {
    if (!email.trim() || !email.includes("@")) return;
    setData(d => ({ ...d, invites: [...d.invites, { email: email.trim(), role }] }));
    setEmail("");
  }
  function remove(i) { setData(d => ({ ...d, invites: d.invites.filter((_, idx) => idx !== i) })); }

  return (
    <div className="ob-card">
      <span className="ob-eyebrow"><span className="ob-eyebrow-dot"></span>Passo 5 de 5 · Opcional</span>
      <h2 className="ob-title" style={{ marginTop: "12px" }}>Convide sua equipe</h2>
      <p className="ob-sub">Cada colaborador recebe um link para criar a própria senha. Pode pular e adicionar depois.</p>
      <div className="ob-invite-form">
        <div className="ob-field" style={{ flex: 2 }}>
          <label className="label">E-mail do colaborador</label>
          <input type="email" className="input" placeholder="colaborador@email.com"
            value={email} onChange={e => setEmail(e.target.value)}
            onKeyDown={e => e.key === "Enter" && add()} />
        </div>
        <div className="ob-field">
          <label className="label">Cargo</label>
          <select className="input" value={role} onChange={e => setRole(e.target.value)} style={{ appearance: "auto" }}>
            {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
          </select>
        </div>
        <div className="ob-field">
          <label className="label" style={{ opacity: 0 }}>.</label>
          <button type="button" className="btn btn-primary" onClick={add} style={{ height: "42px" }}>
            + Convidar
          </button>
        </div>
      </div>
      {data.invites.length > 0 && (
        <div className="ob-invite-list">
          <div className="ob-invite-count">{data.invites.length} {data.invites.length === 1 ? "convite pendente" : "convites pendentes"}</div>
          {data.invites.map((inv, i) => (
            <div key={i} className="ob-invite-item">
              <div className="ob-invite-av">{inv.email[0].toUpperCase()}</div>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600, fontSize: "13px" }}>{inv.email}</div>
                <div style={{ fontSize: "11px", color: "var(--fg-muted)", fontFamily: "var(--font-mono)", textTransform: "uppercase", letterSpacing: "0.05em" }}>{inv.role}</div>
              </div>
              <button className="ob-invite-rm" onClick={() => remove(i)} aria-label="Remover">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M18 6L6 18M6 6l12 12" /></svg>
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ——————————————————————————————————————
// STEP 6 — Tudo pronto!
// ——————————————————————————————————————
function Step6({ data }) {
  const allItems = MENU_MODELS.flatMap(g => g.items);
  const selected = allItems.filter(m => data.selectedModels.includes(m.id));
  const totalItems = selected.reduce((s, m) => s + m.count, 0);
  const modes = [data.delivery && "Delivery", data.retirada && "Retirada", data.local && "Consumo local"].filter(Boolean);

  const confetti = Array.from({ length: 24 }, (_, i) => ({
    id: i,
    left: 4 + Math.random() * 92,
    color: ["var(--accent)", "#dc2626", "#059669", "#2563eb", "#7c3aed", "#db2777"][i % 6],
    dur: 2.2 + Math.random() * 1.8,
    delay: Math.random() * 1.6,
    rot: Math.random() * 360,
    w: 6 + Math.round(Math.random() * 6),
  }));

  return (
    <div className="ob-wide ob-done">
      <div className="ob-confetti" aria-hidden="true">
        {confetti.map(p => (
          <div key={p.id} className="ob-confetti-piece"
            style={{ left: p.left + "%", background: p.color, animationDuration: p.dur + "s", animationDelay: p.delay + "s", width: p.w + "px", transform: `rotate(${p.rot}deg)` }} />
        ))}
      </div>
      <div className="ob-done-hero">
        <div className="ob-done-icon">🎉</div>
        <h1 className="ob-done-title">Tudo configurado!</h1>
        <p className="ob-done-sub">O <strong>{data.nomefantasia || "seu estabelecimento"}</strong> está pronto para operar no Forneria.</p>
      </div>

      <div className="ob-summary-grid">
        <div className="ob-summary-card">
          <div className="ob-summary-label">Estabelecimento</div>
          <div style={{ fontWeight: 600, fontSize: "16px", marginTop: "6px" }}>{data.nomefantasia || "—"}</div>
          {data.cidade && <div style={{ fontSize: "12px", color: "var(--fg-muted)", marginTop: "3px" }}>{data.cidade}{data.uf ? ", " + data.uf : ""}</div>}
        </div>
        <div className="ob-summary-card">
          <div className="ob-summary-label">Itens no cardápio</div>
          <div style={{ fontFamily: "var(--font-mono)", fontWeight: 700, fontSize: "28px", marginTop: "4px" }}>{totalItems > 0 ? "≈ " + totalItems : "0"}</div>
          <div style={{ fontSize: "11px", color: "var(--fg-muted)", fontFamily: "var(--font-mono)" }}>{selected.length} {selected.length === 1 ? "modelo" : "modelos"}</div>
        </div>
        <div className="ob-summary-card">
          <div className="ob-summary-label">Canais ativos</div>
          <div style={{ fontWeight: 600, fontSize: "14px", marginTop: "6px", lineHeight: 1.5 }}>{modes.length > 0 ? modes.join(" · ") : "—"}</div>
        </div>
        <div className="ob-summary-card">
          <div className="ob-summary-label">Equipe convidada</div>
          <div style={{ fontFamily: "var(--font-mono)", fontWeight: 700, fontSize: "28px", marginTop: "4px" }}>{data.invites.length}</div>
          <div style={{ fontSize: "11px", color: "var(--fg-muted)", fontFamily: "var(--font-mono)" }}>{data.invites.length === 1 ? "colaborador" : "colaboradores"}</div>
        </div>
      </div>

      {selected.length > 0 && (
        <div className="ob-summary-models">
          <div className="ob-summary-label">Modelos selecionados</div>
          <div className="ob-model-badges">
            {selected.map(m => <span key={m.id} className="ob-model-badge">{m.emoji} {m.name}</span>)}
          </div>
        </div>
      )}

      <div className="ob-done-actions">
        <a href="index.html" className="btn btn-primary btn-xl">
          Ir para o Dashboard
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
        </a>
        <a href="cardapio.html" className="btn btn-lg">
          Ver meu Cardápio
        </a>
      </div>
    </div>
  );
}

// ——————————————————————————————————————
// APP ROOT
// ——————————————————————————————————————
const DEFAULT_HORARIOS = {
  dom: { on: false, ab: "10:00", fe: "22:00" },
  seg: { on: true,  ab: "10:00", fe: "22:00" },
  ter: { on: true,  ab: "10:00", fe: "22:00" },
  qua: { on: true,  ab: "10:00", fe: "22:00" },
  qui: { on: true,  ab: "10:00", fe: "22:00" },
  sex: { on: true,  ab: "10:00", fe: "23:00" },
  sab: { on: true,  ab: "10:00", fe: "23:00" },
};

function OnboardingApp() {
  const [step, setStep] = useState(1);
  const [data, setData] = useState({
    nomeUsuario: "Maria",
    nomefantasia: "Pizzaria Forneria",
    cnpj: "", cep: "", rua: "", bairro: "", cidade: "", uf: "", telefone: "",
    corMarca: "#d97706",
    selectedModels: [],
    horarios: DEFAULT_HORARIOS,
    mesas: 12, delivery: true, taxaEntrega: "5", raio: "5",
    retirada: true, local: true, tempoPrep: 25,
    invites: [],
  });

  const contentRef = useRef(null);
  const allItems = MENU_MODELS.flatMap(g => g.items);
  const totalItems = data.selectedModels.reduce((s, id) => {
    const m = allItems.find(x => x.id === id);
    return s + (m?.count || 0);
  }, 0);

  function go(dir) {
    setStep(s => Math.max(1, Math.min(6, s + dir)));
    if (contentRef.current) contentRef.current.scrollTop = 0;
  }

  // apply brand color to CSS var on change
  useEffect(() => {
    document.documentElement.style.setProperty("--accent", data.corMarca);
    const isDark = data.corMarca === "#0a0a0a";
    document.documentElement.style.setProperty("--accent-hover", isDark ? "#333" : data.corMarca + "cc");
    document.documentElement.style.setProperty("--accent-fg", "#ffffff");
  }, [data.corMarca]);

  return (
    <div className="ob-shell">
      <ProgressBar current={step} />
      <div className="ob-body" ref={contentRef}>
        {step === 1 && <Step1 data={data} setData={setData} />}
        {step === 2 && <Step2 data={data} setData={setData} />}
        {step === 3 && <Step3 data={data} setData={setData} />}
        {step === 4 && <Step4 data={data} setData={setData} />}
        {step === 5 && <Step5 data={data} setData={setData} />}
        {step === 6 && <Step6 data={data} />}
      </div>
      {step < 6 && (
        <StepFooter
          step={step}
          onBack={() => go(-1)}
          onNext={() => go(1)}
          onSkip={() => setStep(6)}
          selectedModels={data.selectedModels}
          totalItems={totalItems}
        />
      )}
    </div>
  );
}

Object.assign(window, { OnboardingApp });
