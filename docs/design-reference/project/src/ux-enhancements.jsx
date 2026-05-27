// ════════════════════════════════════════════════
// UX Enhancements — features compartilháveis
// - NotificationCenter (sino + dropdown timeline)
// - useFocusTrap (hook para modais)
// - PreCheckoutModal (conferência antes do pagamento)
// - ProductInspect (popover de ficha técnica)
// - EmptyState (componente reutilizável)
// ════════════════════════════════════════════════

const { useState: useUx, useEffect: useUxEffect, useRef: useUxRef, useMemo: useUxMemo } = React;

// ─── Focus trap hook ──────────────────────────────
// Pass containerRef to traps focus within container while open.
function useFocusTrap(open, containerRef, onClose) {
  useUxEffect(() => {
    if (!open || !containerRef.current) return;
    const root = containerRef.current;

    const focusables = () => root.querySelectorAll(
      'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
    );

    // Move focus into the trap
    const first = focusables()[0];
    const previouslyFocused = document.activeElement;
    first?.focus?.();

    const onKey = (e) => {
      if (e.key === 'Escape' && onClose) {
        e.preventDefault();
        onClose();
        return;
      }
      if (e.key !== 'Tab') return;
      const els = Array.from(focusables());
      if (els.length === 0) return;
      const first = els[0];
      const last = els[els.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
      try { previouslyFocused?.focus?.(); } catch {}
    };
  }, [open]);
}

// ─── Notifications data ──────────────────────────
const NOTIF_DEMO = [
  { id: 1, kind: 'urgent', icon: 'flame',   title: 'Estoque crítico',     body: 'Muçarela 1,8h de cobertura · pedido a Fornecedor X aberto',  time: 'há 4 min', actions: ['Ver estoque'], unread: true },
  { id: 2, kind: 'order',  icon: 'cart',    title: 'Pedido #8417 enviado', body: 'Mesa 07 · 3 itens · cozinha estima 12 min',                  time: 'há 6 min', unread: true },
  { id: 3, kind: 'info',   icon: 'bag',     title: 'iFood: 3 novos',       body: 'Pedidos sincronizados automaticamente',                       time: 'há 12 min', unread: true },
  { id: 4, kind: 'good',   icon: 'star',    title: 'Meta atingida',        body: 'Ticket médio R$ 79,34 · 100% da meta',                       time: 'há 23 min' },
  { id: 5, kind: 'info',   icon: 'truck',   title: 'Entregador chegou',    body: 'Pedro M. retornou da rota · disponível para nova entrega',   time: 'há 38 min' },
  { id: 6, kind: 'warn',   icon: 'clock',   title: 'Mesa 12 · 1h aberta',  body: 'Sugerir levar conta?',                                       time: 'há 52 min' },
];

function NotificationCenter({ open, onToggle, onClose }) {
  const ref = useUxRef(null);
  const [items, setItems] = useUx(NOTIF_DEMO);
  const [filter, setFilter] = useUx('all');
  useFocusTrap(open, ref, onClose);

  // Click outside
  useUxEffect(() => {
    if (!open) return;
    const onClick = (e) => {
      if (e.target.closest('.notif-center') || e.target.closest('.notif-trigger')) return;
      onClose();
    };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, [open]);

  const unread = items.filter(n => n.unread).length;
  const visible = filter === 'unread' ? items.filter(n => n.unread) : items;

  const markAllRead = () => setItems(arr => arr.map(n => ({ ...n, unread: false })));
  const markRead = (id) => setItems(arr => arr.map(n => n.id === id ? { ...n, unread: false } : n));

  return (
    <>
      <button className="notif-trigger" onClick={onToggle} title="Notificações" aria-label="Notificações">
        <Icon name="bell" size={16} />
        {unread > 0 && <span className="notif-trigger-badge">{unread > 9 ? '9+' : unread}</span>}
      </button>

      {open && (
        <div className="notif-center" ref={ref} role="dialog" aria-label="Centro de notificações">
          <div className="notif-head">
            <div>
              <div className="notif-title">Notificações</div>
              <div className="notif-sub">{unread > 0 ? `${unread} não lidas` : 'Tudo em dia'}</div>
            </div>
            <button className="btn-ghost btn" style={{ padding: '4px 8px', fontSize: 11 }} onClick={markAllRead}>
              Marcar tudo lido
            </button>
          </div>
          <div className="notif-filter">
            <button className={filter === 'all' ? 'on' : ''} onClick={() => setFilter('all')}>Todas <span className="ct">{items.length}</span></button>
            <button className={filter === 'unread' ? 'on' : ''} onClick={() => setFilter('unread')}>Não lidas <span className="ct">{unread}</span></button>
          </div>
          <div className="notif-list">
            {visible.length === 0 ? (
              <EmptyState
                icon="bell"
                title="Sem notificações"
                sub="Você está em dia · alertas operacionais aparecem aqui em tempo real" />
            ) : visible.map(n => (
              <button key={n.id}
                className={'notif-item ' + (n.unread ? 'unread ' : '') + 'kind-' + n.kind}
                onClick={() => markRead(n.id)}>
                <span className="notif-item-ico"><Icon name={n.icon} size={14} /></span>
                <div className="notif-item-body">
                  <div className="notif-item-title">{n.title}</div>
                  <div className="notif-item-text">{n.body}</div>
                  <div className="notif-item-foot">
                    <span className="notif-item-time">{n.time}</span>
                    {n.actions && n.actions.map(a => (
                      <span key={a} className="notif-item-action">{a} →</span>
                    ))}
                  </div>
                </div>
                {n.unread && <span className="notif-item-dot" />}
              </button>
            ))}
          </div>
          <div className="notif-foot">
            <a className="linklike">Ver histórico completo</a>
            <a className="linklike" style={{ color: 'var(--fg-muted)' }}>Preferências</a>
          </div>
        </div>
      )}
    </>
  );
}

// ─── Empty State component ─────────────────────────
function EmptyState({ icon = 'cart', title, sub, ctaLabel, ctaIcon, onCta, secondaryLabel, onSecondary, compact = false }) {
  return (
    <div className={'empty-state ' + (compact ? 'compact' : '')}>
      <div className="empty-state-ico">
        <Icon name={icon} size={compact ? 24 : 36} />
      </div>
      <div className="empty-state-title">{title}</div>
      {sub && <div className="empty-state-sub">{sub}</div>}
      {(ctaLabel || secondaryLabel) && (
        <div className="empty-state-actions">
          {ctaLabel && (
            <button className="btn btn-primary" onClick={onCta}>
              {ctaIcon && <Icon name={ctaIcon} size={13} />} {ctaLabel}
            </button>
          )}
          {secondaryLabel && (
            <button className="btn btn-ghost" onClick={onSecondary}>{secondaryLabel}</button>
          )}
        </div>
      )}
    </div>
  );
}

// ─── PreCheckout Modal — conferência antes do pagamento ─
function PreCheckoutModal({ open, cart, customer, mode, modeDetails, subtotal, deliveryFee, total, discounts, onConfirm, onCancel }) {
  const ref = useUxRef(null);
  useFocusTrap(open, ref, onCancel);
  if (!open) return null;

  const modeLabels = {
    balcao: 'Balcão', delivery: 'Delivery', retirada: 'Retirada para retirar',
    mesa: 'Mesa', comanda: 'Comanda',
  };

  const totalItems = cart.reduce((n, i) => n + i.qty, 0);

  return (
    <div className="prechk-overlay" onClick={(e) => { if (e.target === e.currentTarget) onCancel(); }}>
      <div className="prechk-modal" ref={ref} role="dialog" aria-label="Conferência do pedido">
        <div className="prechk-head">
          <div>
            <div className="prechk-eyebrow">CONFERÊNCIA · LEIA EM VOZ ALTA</div>
            <div className="prechk-title">{totalItems} {totalItems === 1 ? 'item' : 'itens'} · {modeLabels[mode] || mode}</div>
          </div>
          <button className="prechk-close" onClick={onCancel} aria-label="Voltar">
            <Icon name="x" size={20} />
          </button>
        </div>

        {customer && (
          <div className="prechk-customer">
            <div className="prechk-customer-av">{customer.name.split(' ').slice(0,2).map(w => w[0]).join('')}</div>
            <div>
              <div className="prechk-customer-name">{customer.name}</div>
              <div className="prechk-customer-meta">{customer.phone}{customer.points ? ' · ' + customer.points + ' pts' : ''}</div>
            </div>
          </div>
        )}

        <div className="prechk-items">
          {cart.map(item => (
            <div key={item.id} className="prechk-item">
              <div className="prechk-item-qty">{item.qty}×</div>
              <div className="prechk-item-name">
                {item.name}
                {item.notes && item.notes.length > 0 && (
                  <ul className="prechk-item-notes">
                    {item.notes.map((n, i) => <li key={i}>{n}</li>)}
                  </ul>
                )}
              </div>
              <div className="prechk-item-price">{BRL(item.price * item.qty)}</div>
            </div>
          ))}
        </div>

        <div className="prechk-totals">
          <div className="prechk-tt-row"><span>Subtotal</span><b>{BRL(subtotal)}</b></div>
          {deliveryFee > 0 && <div className="prechk-tt-row"><span>Entrega</span><b>{BRL(deliveryFee)}</b></div>}
          {discounts > 0 && <div className="prechk-tt-row good"><span>Descontos</span><b>−{BRL(discounts)}</b></div>}
          <div className="prechk-tt-row grand"><span>Total</span><b>{BRL(total)}</b></div>
        </div>

        <div className="prechk-foot">
          <button className="btn btn-ghost btn-lg" onClick={onCancel}>
            <Icon name="chevron-left" size={14} /> Voltar e editar
          </button>
          <button className="btn btn-primary btn-xl" onClick={onConfirm}>
            <Icon name="card" size={16} /> Confirmar e ir ao pagamento
            <span style={{ marginLeft: 8, fontFamily: 'var(--font-mono)', opacity: 0.85, fontSize: 13 }}>{BRL(total)}</span>
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Product Inspect — popover com ficha do produto ─
function ProductInspect({ product, anchor, onClose }) {
  if (!product || !anchor) return null;

  // Position: prefer right of anchor; clamp to viewport
  const popW = 280;
  const popH = 240;
  const margin = 12;
  let left = anchor.right + margin;
  let top = anchor.top;
  if (left + popW > window.innerWidth - 8) {
    left = anchor.left - popW - margin;
  }
  if (left < 8) left = Math.max(8, anchor.left + (anchor.width - popW) / 2);
  if (top + popH > window.innerHeight - 8) {
    top = Math.max(8, window.innerHeight - popH - 8);
  }

  // Mock data — in real app comes from product.fichaTecnica
  const ingredients = product.ingredients || ['Massa artesanal','Molho da casa','Muçarela de búfala','Manjericão'];
  const cost = (product.price * 0.32).toFixed(2);
  const margin_ = ((1 - 0.32) * 100).toFixed(0);

  return (
    <div className="inspect-pop" style={{ left, top }} role="tooltip">
      <div className="inspect-head">
        <div className="inspect-name">{product.name}</div>
        {product.tag && <span className="inspect-tag">{product.tag}</span>}
      </div>
      {product.desc && <div className="inspect-desc">{product.desc}</div>}
      <div className="inspect-section">
        <div className="inspect-label">Ingredientes</div>
        <div className="inspect-ingredients">
          {ingredients.map((i, k) => <span key={k} className="inspect-ing">{i}</span>)}
        </div>
      </div>
      <div className="inspect-section">
        <div className="inspect-label">Métrica</div>
        <div className="inspect-metrics">
          <div className="inspect-metric">
            <div className="m-label">Preço</div>
            <div className="m-val">{BRL(product.price)}</div>
          </div>
          <div className="inspect-metric">
            <div className="m-label">Custo</div>
            <div className="m-val">{BRL(parseFloat(cost))}</div>
          </div>
          <div className="inspect-metric">
            <div className="m-label">Margem</div>
            <div className="m-val" style={{ color: 'var(--good)' }}>{margin_}%</div>
          </div>
        </div>
      </div>
      <div className="inspect-foot">
        <kbd>Esc</kbd> ou clique fora para fechar
      </div>
    </div>
  );
}

// Hook to attach inspect-on-shift-click + long-press
function useProductInspect() {
  const [inspect, setInspect] = useUx(null); // { product, anchor }
  useUxEffect(() => {
    if (!inspect) return;
    const onKey = (e) => { if (e.key === 'Escape') setInspect(null); };
    const onClick = (e) => {
      if (e.target.closest('.inspect-pop')) return;
      setInspect(null);
    };
    document.addEventListener('keydown', onKey);
    setTimeout(() => document.addEventListener('mousedown', onClick), 50);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onClick);
    };
  }, [inspect]);
  return { inspect, setInspect };
}

Object.assign(window, {
  useFocusTrap,
  NotificationCenter,
  EmptyState,
  PreCheckoutModal,
  ProductInspect,
  useProductInspect,
});

// ════════════════════════════════════════════════
// Onboarding tour — coach marks numerados
// ════════════════════════════════════════════════

const TOUR_STEPS = [
  {
    selector: '.brand',
    title: 'Bem-vinda ao PDV Forneria',
    body: 'Tour rápido de 6 passos pra você operar como gente da casa. Pode pular a qualquer momento.',
    pos: 'bottom-left',
  },
  {
    selector: '.omni-input, .omni-box, [data-tour="omni"]',
    title: 'Omni Box · entrada única',
    body: 'Digite código do produto, telefone do cliente (C:9999), número da mesa (M07) ou $ para encerrar. Tudo aqui.',
    pos: 'bottom',
  },
  {
    selector: '.fav-rail, [data-tour="fav"]',
    title: 'Favoritos & recentes',
    body: 'Os últimos 8 produtos vendidos aparecem aqui. Toque para adicionar com 1 clique.',
    pos: 'bottom',
    skipIfMissing: true,
  },
  {
    selector: '.cart-modes, [data-tour="modes"]',
    title: 'Modo de venda',
    body: 'Balcão, delivery, retirada, mesa ou comanda. Atalhos Ctrl+1 a Ctrl+5.',
    pos: 'left',
    skipIfMissing: true,
  },
  {
    selector: '.notif-trigger, [data-tour="notif"]',
    title: 'Centro de notificações',
    body: 'Alertas operacionais em tempo real: estoque, iFood, mesas chamando.',
    pos: 'bottom-right',
  },
  {
    selector: '.help-trigger, [data-tour="help"]',
    title: 'Atalhos · tudo sem mouse',
    body: 'Pressione ? a qualquer momento. F2 abre catálogo, F5 finaliza, F9 cancela, Ctrl+N pausa.',
    pos: 'bottom-right',
  },
];

function Tour({ open, onClose }) {
  const [step, setStep] = useUx(0);
  const [box, setBox] = useUx(null);
  const cur = TOUR_STEPS[step];

  useUxEffect(() => {
    if (!open) return;
    // Measure target element
    const measure = () => {
      if (!cur) return;
      const sel = cur.selector.split(',').map(s => s.trim());
      let el = null;
      for (const s of sel) {
        el = document.querySelector(s);
        if (el) break;
      }
      if (!el) {
        if (cur.skipIfMissing && step < TOUR_STEPS.length - 1) {
          setStep(step + 1);
        } else {
          setBox(null);
        }
        return;
      }
      const r = el.getBoundingClientRect();
      setBox({ top: r.top, left: r.left, width: r.width, height: r.height, pos: cur.pos });
    };
    measure();
    const id = setInterval(measure, 200);
    window.addEventListener('resize', measure);
    return () => { clearInterval(id); window.removeEventListener('resize', measure); };
  }, [open, step]);

  useUxEffect(() => {
    if (!open) return;
    const onKey = (e) => {
      if (e.key === 'Escape') onClose();
      else if (e.key === 'ArrowRight' || e.key === 'Enter') next();
      else if (e.key === 'ArrowLeft') prev();
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, step]);

  if (!open) return null;
  const next = () => {
    if (step < TOUR_STEPS.length - 1) setStep(s => s + 1);
    else onClose(true);
  };
  const prev = () => { if (step > 0) setStep(s => s - 1); };

  // Compute card position from target box + pos
  const cardW = 320;
  let cardLeft, cardTop;
  if (box) {
    const gap = 12;
    const pos = box.pos || 'bottom';
    if (pos === 'bottom' || pos === 'bottom-left' || pos === 'bottom-right') {
      cardTop = box.top + box.height + gap;
      cardLeft = pos === 'bottom-left' ? box.left
               : pos === 'bottom-right' ? (box.left + box.width - cardW)
               : (box.left + box.width / 2 - cardW / 2);
    } else if (pos === 'top') {
      cardTop = box.top - 180 - gap;
      cardLeft = box.left + box.width / 2 - cardW / 2;
    } else if (pos === 'left') {
      cardLeft = box.left - cardW - gap;
      cardTop = box.top;
    } else if (pos === 'right') {
      cardLeft = box.left + box.width + gap;
      cardTop = box.top;
    }
    cardLeft = Math.max(12, Math.min(cardLeft, window.innerWidth - cardW - 12));
    cardTop = Math.max(12, Math.min(cardTop, window.innerHeight - 200));
  } else {
    cardLeft = window.innerWidth / 2 - cardW / 2;
    cardTop = window.innerHeight / 2 - 100;
  }

  return (
    <div className="tour-backdrop">
      {box && (
        <div className="tour-spotlight"
          style={{
            top: box.top - 6, left: box.left - 6,
            width: box.width + 12, height: box.height + 12,
          }} />
      )}
      <div className="tour-card" style={{ left: cardLeft, top: cardTop, width: cardW }}>
        <div className="tour-card-step">
          {step + 1} de {TOUR_STEPS.length}
          <span className="tour-progress">
            {TOUR_STEPS.map((_, i) => (
              <span key={i} className={'tour-dot ' + (i <= step ? 'on' : '')} />
            ))}
          </span>
        </div>
        <div className="tour-card-title">{cur.title}</div>
        <div className="tour-card-body">{cur.body}</div>
        <div className="tour-card-foot">
          <button className="btn btn-ghost" onClick={() => onClose(true)}>Pular tour</button>
          <div style={{ display: 'flex', gap: 6 }}>
            {step > 0 && <button className="btn btn-ghost" onClick={prev}>← Voltar</button>}
            <button className="btn btn-primary" onClick={next}>
              {step === TOUR_STEPS.length - 1 ? 'Concluir ✓' : 'Próximo →'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ════════════════════════════════════════════════
// Receipt preview — recibo 80mm + ações
// ════════════════════════════════════════════════

function ReceiptPreview({ open, order, onClose }) {
  const ref = useUxRef(null);
  useFocusTrap(open, ref, onClose);
  if (!open || !order) return null;

  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  const dateStr = `${pad(now.getDate())}/${pad(now.getMonth()+1)}/${now.getFullYear()} ${pad(now.getHours())}:${pad(now.getMinutes())}`;

  const subtotal = order.items.reduce((s, i) => s + i.price * i.qty, 0);

  const modeLabels = { balcao: 'BALCÃO', delivery: 'DELIVERY', retirada: 'RETIRADA', mesa: 'MESA', comanda: 'COMANDA' };

  return (
    <div className="receipt-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="receipt-wrap" ref={ref} role="dialog" aria-label="Pré-visualização do recibo">
        <div className="receipt-head">
          <div>
            <div className="receipt-eyebrow">PRÉ-VISUALIZAÇÃO</div>
            <div className="receipt-title">Comanda #{order.id || '8417'}</div>
          </div>
          <button className="prechk-close" onClick={onClose} aria-label="Fechar">
            <Icon name="x" size={18} />
          </button>
        </div>

        <div className="receipt-paper-wrap">
          <div className="receipt-paper">
            <div className="r-header">
              <div className="r-brand">FORNERIA</div>
              <div className="r-store">Forno do Bairro · Vila Madalena</div>
              <div className="r-store-meta">Rua Aspicuelta, 542 · (11) 3815-0000</div>
              <div className="r-store-meta">CNPJ 12.345.678/0001-90</div>
            </div>
            <div className="r-divider">═══════════════════════════</div>
            <div className="r-line"><span>COMANDA</span><b>#{order.id || '8417'}</b></div>
            <div className="r-line"><span>DATA</span><b>{dateStr}</b></div>
            <div className="r-line"><span>OPERADOR</span><b>Carla R.</b></div>
            <div className="r-line"><span>MODO</span><b>{modeLabels[order.mode] || order.mode || '—'}</b></div>
            {order.customer && (
              <>
                <div className="r-line"><span>CLIENTE</span><b>{order.customer.name}</b></div>
                <div className="r-line"><span>TELEFONE</span><b>{order.customer.phone}</b></div>
              </>
            )}
            <div className="r-divider">───────────────────────────</div>
            <div className="r-items-head">
              <span>ITEM</span>
              <span>QTD</span>
              <span>VALOR</span>
            </div>
            <div className="r-divider">───────────────────────────</div>
            {order.items.map(it => (
              <div key={it.id} className="r-item">
                <div className="r-item-name">{it.name}</div>
                {it.notes && it.notes.length > 0 && (
                  <div className="r-item-notes">
                    {it.notes.map((n, i) => <div key={i}>  &gt; {n}</div>)}
                  </div>
                )}
                <div className="r-item-vals">
                  <span>{BRL(it.price)} ×{it.qty}</span>
                  <b>{BRL(it.price * it.qty)}</b>
                </div>
              </div>
            ))}
            <div className="r-divider">───────────────────────────</div>
            <div className="r-line"><span>SUBTOTAL</span><b>{BRL(subtotal)}</b></div>
            {order.deliveryFee > 0 && <div className="r-line"><span>ENTREGA</span><b>{BRL(order.deliveryFee)}</b></div>}
            {order.discounts > 0 && <div className="r-line"><span>DESCONTOS</span><b>−{BRL(order.discounts)}</b></div>}
            <div className="r-divider">═══════════════════════════</div>
            <div className="r-total"><span>TOTAL</span><b>{BRL(order.total)}</b></div>
            <div className="r-divider">═══════════════════════════</div>
            <div className="r-foot">
              <div>Obrigado pela preferência!</div>
              <div className="r-foot-meta">Acompanhe seu pedido em forneria.app/p/{order.id || '8417'}</div>
              <div className="r-qr">
                {[...Array(13*13)].map((_, i) => (
                  <span key={i} style={{ background: (i * 7 + 3) % 3 === 0 ? '#000' : 'transparent' }} />
                ))}
              </div>
              <div className="r-cut">✂ - - - - - - - - - - - - - - -</div>
            </div>
          </div>
        </div>

        <div className="receipt-foot">
          <button className="btn btn-ghost"><Icon name="cash" size={14} /> Abrir gaveta</button>
          <button className="btn"><Icon name="phone" size={14} /> WhatsApp</button>
          <button className="btn btn-primary"><Icon name="print" size={14} /> Imprimir</button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { Tour, ReceiptPreview, TOUR_STEPS });

