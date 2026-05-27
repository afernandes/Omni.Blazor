// ============================================================
// Fluxo de Pagamento / Finalização de Pedido
// Etapas: 1. Pagamento  2. Confirmação  3. Recibo
// ============================================================

const { useState: usePay, useEffect: usePayFX, useMemo: usePayMemo } = React;

// ——— helpers ———
const fmt = (n) => BRL(typeof n === 'string' ? parseFloat(n) || 0 : n || 0);
const parseBRL = (s) => parseFloat(String(s).replace(',', '.')) || 0;

// ——— Teclado numérico ———
function Numpad({ value, onChange }) {
  const press = (k) => {
    if (k === '⌫') {
      onChange(String(value).slice(0, -1) || '0');
    } else if (k === ',') {
      if (!String(value).includes(',')) onChange(String(value) + ',');
    } else {
      const raw = String(value) === '0' ? k : String(value) + k;
      onChange(raw);
    }
  };
  const keys = ['7','8','9','4','5','6','1','2','3','0',',','⌫'];
  return (
    <div className="numpad">
      {keys.map(k => (
        <button key={k} className={'numpad-key ' + (k === '⌫' ? 'del' : k === '0' ? 'zero' : '')} onClick={() => press(k)}>
          {k === '⌫' ? <Icon name="x" size={16} /> : k}
        </button>
      ))}
    </div>
  );
}

// ——— Método de pagamento card ———
function PayMethodCard({ id, label, icon, active, onClick, amount, onAmount }) {
  return (
    <div className={'pay-method ' + (active ? 'active' : '')} onClick={onClick}>
      <div className="pay-method-top">
        <Icon name={icon} size={18} />
        <span>{label}</span>
        {active && <span className="badge accent" style={{ marginLeft: 'auto' }}>selecionado</span>}
      </div>
      {active && onAmount && (
        <div className="pay-method-amount" onClick={e => e.stopPropagation()}>
          <span style={{ color: 'var(--fg-muted)', fontSize: 13 }}>Valor:</span>
          <input
            className="input"
            style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, fontSize: 16, textAlign: 'right' }}
            value={amount}
            onChange={e => onAmount(e.target.value)}
            placeholder="0,00"
          />
        </div>
      )}
    </div>
  );
}

// ============================================================
// TELA PRINCIPAL DO PAGAMENTO
// ============================================================
function PaymentFlow({ total, customer, items, mode, modeDetails, onClose, onConfirm }) {
  const [step, setStep] = usePay(1); // 1=pagamento, 2=confirmação, 3=recibo

  // Pagamento
  const [methods, setMethods] = usePay([]);         // lista de métodos escolhidos
  const [split, setSplit] = usePay(false);          // pagamento dividido
  const [cashInput, setCashInput] = usePay('');     // valor em dinheiro digitado
  const [numpadTarget, setNumpadTarget] = usePay(null); // qual campo o numpad controla
  const [loyalty, setLoyalty] = usePay(false);      // resgatar pontos
  const [loyaltyPts, setLoyaltyPts] = usePay(0);

  // Dados gerados na confirmação
  const [orderNum, setOrderNum] = usePay('');
  const [paidAt, setPaidAt] = usePay(null);
  // Snapshot dos itens/total no momento da confirmação (o carrinho é limpo depois)
  const [snapItems, setSnapItems] = usePay(null);
  const [snapTotal, setSnapTotal] = usePay(null);

  const METHODS = [
    { id: 'cash',   label: 'Dinheiro',  icon: 'cash',  hasChange: true },
    { id: 'card',   label: 'Cartão',    icon: 'card',  hasChange: false },
    { id: 'pix',    label: 'Pix',       icon: 'pix',   hasChange: false },
    { id: 'voucher',label: 'Vale',      icon: 'star',  hasChange: false },
  ];

  const loyaltyDiscount = loyalty ? loyaltyPts * 0.01 : 0; // 1 ponto = R$ 0,01
  const totalWithDiscount = Math.max(0, total - loyaltyDiscount);

  const toggleMethod = (id) => {
    if (!split) {
      setMethods([{ id, amount: '' }]);
      if (id === 'cash') setNumpadTarget('cash-0');
      else setNumpadTarget(null);
    } else {
      setMethods(prev => {
        const exists = prev.find(m => m.id === id);
        if (exists) return prev.filter(m => m.id !== id);
        return [...prev, { id, amount: '' }];
      });
    }
  };

  const setMethodAmount = (id, val) => {
    setMethods(prev => prev.map(m => m.id === id ? { ...m, amount: val } : m));
  };

  const paidTotal = usePayMemo(() => {
    if (!split) {
      const m = methods[0];
      if (!m) return 0;
      if (m.id === 'cash') return parseBRL(cashInput) || totalWithDiscount;
      return totalWithDiscount;
    }
    return methods.reduce((s, m) => s + (parseBRL(m.amount) || 0), 0);
  }, [methods, cashInput, split, totalWithDiscount]);

  const change = usePayMemo(() => {
    const m = methods[0];
    if (!split && m?.id === 'cash') {
      return Math.max(0, parseBRL(cashInput) - totalWithDiscount);
    }
    if (split) return Math.max(0, paidTotal - totalWithDiscount);
    return 0;
  }, [methods, cashInput, split, paidTotal, totalWithDiscount]);

  const remainingToPay = usePayMemo(() => {
    if (!split) return 0;
    return Math.max(0, totalWithDiscount - paidTotal);
  }, [split, totalWithDiscount, paidTotal]);

  const canProceed = usePayMemo(() => {
    if (methods.length === 0) return false;
    if (!split) return true;
    return paidTotal >= totalWithDiscount;
  }, [methods, split, paidTotal, totalWithDiscount]);

  const confirm = () => {
    const num = '#' + String(Math.floor(4800 + Math.random() * 200)).padStart(4, '0');
    // snapshot antes de limpar o carrinho
    setSnapItems([...items]);
    setSnapTotal(totalWithDiscount);
    setOrderNum(num);
    setPaidAt(new Date());
    setStep(3);
    onConfirm && onConfirm({ orderNum: num, total: totalWithDiscount, methods, change, loyaltyDiscount });
  };

  // ——— step 1: pagamento ———
  const cashOpen = !split && methods[0]?.id === 'cash';
  if (step === 1) return (
    <div className="pay-overlay" onClick={onClose}>
      <div className={'pay-modal ' + (cashOpen ? 'pay-modal-wide' : '')} onClick={e => e.stopPropagation()}>
        <div className="pay-head">
          <div>
            <div className="pay-head-title">Finalizar pedido</div>
            <div className="pay-head-sub">{items.length} {items.length === 1 ? 'item' : 'itens'} · {mode}</div>
          </div>
          <button className="btn btn-ghost btn-icon" onClick={onClose}><Icon name="x" size={16} /></button>
        </div>

        <div className={'pay-body ' + (cashOpen ? 'pay-body-2col' : '')}>
        <div className="pay-body-left">
          {/* Resumo do pedido */}
          <div className="pay-summary">
            <div className="pay-summary-head">Resumo</div>
            {items.map((item, i) => (
              <div key={i} className="pay-summary-row">
                <span className="pay-summary-qty">{item.qty}×</span>
                <span className="pay-summary-name">{item.name}</span>
                <span className="pay-summary-price">{fmt(item.price * item.qty)}</span>
              </div>
            ))}
            {mode === 'delivery' && (
              <div className="pay-summary-row" style={{ color: 'var(--fg-muted)' }}>
                <span></span><span>Taxa de entrega</span>
                <span style={{ fontFamily: 'var(--font-mono)' }}>{fmt(8)}</span>
              </div>
            )}
            {loyalty && loyaltyDiscount > 0 && (
              <div className="pay-summary-row" style={{ color: 'var(--good)' }}>
                <span></span><span>Desconto fidelidade ({loyaltyPts} pts)</span>
                <span style={{ fontFamily: 'var(--font-mono)' }}>−{fmt(loyaltyDiscount)}</span>
              </div>
            )}
            <div className="pay-summary-total">
              <span>Total</span>
              <span>{fmt(totalWithDiscount)}</span>
            </div>
          </div>

          {/* Fidelidade */}
          {customer && customer.points > 0 && (
            <div className={'pay-loyalty-row ' + (loyalty ? 'active' : '')} onClick={() => { setLoyalty(l => !l); if (!loyalty) setLoyaltyPts(Math.min(customer.points, Math.floor(totalWithDiscount * 100))); else setLoyaltyPts(0); }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <Icon name="sparkle" size={16} />
                <div>
                  <div style={{ fontWeight: 600, fontSize: 13 }}>Resgatar pontos de fidelidade</div>
                  <div style={{ fontSize: 11, color: loyalty ? 'var(--accent)' : 'var(--fg-muted)' }}>{customer.points} pts disponíveis · −{fmt(Math.min(customer.points, Math.floor(totalWithDiscount * 100)) * 0.01)}</div>
                </div>
              </div>
              <div className={'pay-toggle ' + (loyalty ? 'on' : '')}></div>
            </div>
          )}

          {/* Método de pagamento */}
          <div>
            <div className="pay-section-label">
              Forma de pagamento
              <button className={'pay-split-btn ' + (split ? 'active' : '')} onClick={() => { setSplit(s => !s); setMethods([]); }}>
                <Icon name="sliders" size={12} /> Dividir
              </button>
            </div>

            <div className="pay-methods-grid">
              {METHODS.map(m => (
                <PayMethodCard
                  key={m.id}
                  {...m}
                  active={methods.some(x => x.id === m.id)}
                  onClick={() => toggleMethod(m.id)}
                  amount={methods.find(x => x.id === m.id)?.amount || ''}
                  onAmount={split ? (v) => setMethodAmount(m.id, v) : null}
                />
              ))}
            </div>

            {/* Dividir: totais */}
            {split && methods.length > 0 && (
              <div className="pay-split-summary">
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                  <span style={{ color: 'var(--fg-muted)' }}>Pago até agora</span>
                  <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{fmt(paidTotal)}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, marginTop: 4 }}>
                  <span style={{ color: remainingToPay > 0 ? 'var(--danger)' : 'var(--good)', fontWeight: 600 }}>
                    {remainingToPay > 0 ? 'Falta pagar' : 'Troco'}
                  </span>
                  <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, color: remainingToPay > 0 ? 'var(--danger)' : 'var(--good)' }}>
                    {fmt(remainingToPay > 0 ? remainingToPay : change)}
                  </span>
                </div>
              </div>
            )}
          </div>
        </div>
        {cashOpen && (
          <div className="pay-body-right">
            <div className="pay-cash-section">
              <div className="pay-section-label">Valor recebido</div>
              <div className="pay-cash-display">
                <div className="pay-cash-value">
                  R$ {cashInput || '0'}
                </div>
                {parseBRL(cashInput) > 0 && (
                  <div className={'pay-change ' + (change > 0 ? 'has-change' : '')}>
                    <span>Troco:</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 700 }}>{fmt(change)}</span>
                  </div>
                )}
              </div>
              <div className="pay-cash-shortcuts">
                {[totalWithDiscount, Math.ceil(totalWithDiscount / 5) * 5, Math.ceil(totalWithDiscount / 10) * 10, Math.ceil(totalWithDiscount / 50) * 50].filter((v, i, a) => a.indexOf(v) === i).slice(0, 4).map(v => (
                  <button key={v} className={'pay-shortcut ' + (parseBRL(cashInput) === v ? 'active' : '')} onClick={() => setCashInput(String(v.toFixed(2)).replace('.', ','))}>
                    {fmt(v)}
                  </button>
                ))}
              </div>
              <Numpad value={cashInput} onChange={setCashInput} />
            </div>
          </div>
        )}
        </div>

        <div className="pay-foot">
          <button className="btn" onClick={onClose}><Icon name="chevron-left" size={14} /> Voltar</button>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 2 }}>
            <span style={{ fontSize: 11, color: 'var(--fg-soft)', textTransform: 'uppercase', letterSpacing: '0.05em', fontWeight: 600 }}>Total</span>
            <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, fontSize: 22 }}>{fmt(totalWithDiscount)}</span>
          </div>
          <button
            className="btn btn-primary btn-xl"
            style={{ opacity: canProceed ? 1 : 0.4 }}
            disabled={!canProceed}
            onClick={() => setStep(2)}>
            <Icon name="check" size={16} /> Confirmar pagamento
          </button>
        </div>
      </div>
    </div>
  );

  // ——— step 2: confirmação ———
  if (step === 2) return (
    <div className="pay-overlay">
      <div className="pay-modal pay-modal-sm" onClick={e => e.stopPropagation()}>
        <div className="pay-head">
          <div className="pay-head-title">Confirmar pagamento</div>
        </div>
        <div className="pay-body" style={{ gap: 14 }}>
          <div style={{ textAlign: 'center', padding: '8px 0 4px' }}>
            <div style={{ fontSize: 13, color: 'var(--fg-muted)', marginBottom: 6 }}>Total a receber</div>
            <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 800, fontSize: 36, letterSpacing: '-0.02em', color: 'var(--fg)' }}>{fmt(totalWithDiscount)}</div>
            {change > 0 && (
              <div style={{ marginTop: 8, padding: '8px 16px', background: 'color-mix(in oklab, var(--warn) 12%, transparent)', borderRadius: 8, display: 'inline-flex', gap: 8, alignItems: 'center' }}>
                <Icon name="cash" size={14} style={{ color: 'var(--warn)' }} />
                <span style={{ fontSize: 13, fontWeight: 600 }}>Troco: <span style={{ fontFamily: 'var(--font-mono)' }}>{fmt(change)}</span></span>
              </div>
            )}
          </div>

          <div style={{ background: 'var(--bg-sunken)', borderRadius: 10, padding: '14px 16px', display: 'flex', flexDirection: 'column', gap: 8 }}>
            {methods.map(m => {
              const def = METHODS.find(x => x.id === m.id);
              const amt = m.id === 'cash' ? parseBRL(cashInput) || totalWithDiscount : parseBRL(m.amount) || totalWithDiscount / methods.length;
              return (
                <div key={m.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}><Icon name={def.icon} size={14} />{def.label}</span>
                  <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{fmt(amt)}</span>
                </div>
              );
            })}
            {loyalty && loyaltyDiscount > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, color: 'var(--good)', borderTop: '1px dashed var(--line)', paddingTop: 8 }}>
                <span><Icon name="sparkle" size={14} /> Desconto fidelidade</span>
                <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>−{fmt(loyaltyDiscount)}</span>
              </div>
            )}
          </div>

          {customer && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '10px 14px', background: 'var(--bg-sunken)', borderRadius: 8 }}>
              <div className="avatar" style={{ width: 32, height: 32, fontSize: 12 }}>{customer.name.split(' ').map(w => w[0]).slice(0,2).join('')}</div>
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>{customer.name}</div>
                <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>+{Math.floor(totalWithDiscount)} pts de fidelidade</div>
              </div>
            </div>
          )}
        </div>
        <div className="pay-foot" style={{ justifyContent: 'space-between' }}>
          <button className="btn" onClick={() => setStep(1)}><Icon name="chevron-left" size={14} /> Voltar</button>
          <button className="btn btn-primary btn-xl" onClick={confirm}>
            <Icon name="check" size={16} /> Fechar pedido
          </button>
        </div>
      </div>
    </div>
  );

  // ——— step 3: recibo ———
  if (step === 3) {
    const receiptItems = snapItems || items;
    const receiptTotal = snapTotal !== null ? snapTotal : totalWithDiscount;
    return (
    <div className="pay-overlay">
      <div className="pay-modal pay-modal-sm" onClick={e => e.stopPropagation()}>
        <div className="pay-receipt">
          {/* Cabeçalho do recibo */}
          <div className="receipt-header">
            <div className="receipt-logo">F</div>
            <div className="receipt-store">Forno do Bairro</div>
            <div className="receipt-store-sub">Vila Madalena · CNPJ 00.000.000/0001-00</div>
            <div className="receipt-store-sub">{paidAt && paidAt.toLocaleString('pt-BR')}</div>
          </div>

          <div className="receipt-divider">— — — — — — — — — — — — —</div>

          {/* Número do pedido */}
          <div className="receipt-order-num">
            <div style={{ fontSize: 11, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--fg-soft)' }}>Pedido</div>
            <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 800, fontSize: 28, letterSpacing: '-0.01em' }}>{orderNum}</div>
          </div>

          {customer && (
            <div style={{ fontSize: 12, color: 'var(--fg-muted)', textAlign: 'center', marginBottom: 8 }}>
              Cliente: <b style={{ color: 'var(--fg)' }}>{customer.name}</b>
            </div>
          )}

          {/* Detalhes do modo */}
          {modeDetails && (() => {
            const d = modeDetails;
            const modeLabel = { balcao: 'Balcão', delivery: 'Delivery', retirada: 'Retirada', mesa: 'Mesa', comanda: 'Comanda' }[mode] || mode;
            return (
              <div style={{ background: 'var(--bg-sunken)', borderRadius: 8, padding: '8px 14px', fontSize: 12, display: 'flex', flexDirection: 'column', gap: 3 }}>
                <div style={{ fontWeight: 600, color: 'var(--fg)', marginBottom: 2 }}>{modeLabel}</div>
                {mode === 'delivery' && d.zoneId && (
                  <>
                    {d.address && <div style={{ color: 'var(--fg-muted)' }}>{d.address}</div>}
                    {d.zoneId !== 'maps' && <div style={{ color: 'var(--fg-muted)' }}>Zona: {ZONES_DELIVERY?.find(z => z.id === d.zoneId)?.name || d.zoneId}</div>}
                    {d.deliveryPerson && <div style={{ color: 'var(--fg-muted)' }}>Entregador: {d.deliveryPerson}</div>}
                  </>
                )}
                {mode === 'mesa' && d.tableNum && <div style={{ color: 'var(--fg-muted)' }}>Mesa {d.tableNum}{d.covers ? ` · ${d.covers} pessoas` : ''}{d.waiter && d.waiter !== 'auto' ? ` · Garçom: ${d.waiter}` : ''}</div>}
                {mode === 'comanda' && d.comandaNum && <div style={{ color: 'var(--fg-muted)' }}>Comanda #{d.comandaNum}{d.location ? ` · ${d.location}` : ''}</div>}
                {mode === 'retirada' && d.pickupName && <div style={{ color: 'var(--fg-muted)' }}>{d.pickupName}{d.etaMins ? ` · em ${d.etaMins} min` : ''}</div>}
                {mode === 'balcao' && d.label && <div style={{ color: 'var(--fg-muted)' }}>{d.label}</div>}
              </div>
            );
          })()}

          <div className="receipt-divider">— — — — — — — — — — — — —</div>

          {/* Itens */}
          <div className="receipt-items">
            {receiptItems.map((item, i) => (
              <div key={i} className="receipt-item">
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>{item.name}</div>
                  {item.notes && item.notes.length > 0 && (
                    <div style={{ fontSize: 10, color: 'var(--fg-muted)', marginTop: 2 }}>
                      {item.notes.map((n, j) => <div key={j}>› {n}</div>)}
                    </div>
                  )}
                </div>
                <div style={{ textAlign: 'right', flexShrink: 0 }}>
                  <div style={{ fontSize: 11, color: 'var(--fg-muted)' }}>{item.qty}× {fmt(item.price)}</div>
                  <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{fmt(item.price * item.qty)}</div>
                </div>
              </div>
            ))}
          </div>

          <div className="receipt-divider">— — — — — — — — — — — — —</div>

          {/* Totais */}
          <div className="receipt-totals">
            {mode === 'delivery' && (
              <div className="receipt-total-row">
                <span>Taxa de entrega</span>
                <span>{fmt(8)}</span>
              </div>
            )}
            {loyalty && loyaltyDiscount > 0 && (
              <div className="receipt-total-row" style={{ color: 'var(--good)' }}>
                <span>Desconto fidelidade</span>
                <span>−{fmt(loyaltyDiscount)}</span>
              </div>
            )}
            <div className="receipt-total-row grand">
              <span>TOTAL</span>
              <span>{fmt(receiptTotal)}</span>
            </div>
          </div>

          <div className="receipt-divider">— — — — — — — — — — — — —</div>

          {/* Pagamento */}
          <div className="receipt-totals">
            {methods.map(m => {
              const def = { cash: 'Dinheiro', card: 'Cartão', pix: 'Pix', voucher: 'Vale' }[m.id];
              const amt = m.id === 'cash' ? parseBRL(cashInput) || receiptTotal : parseBRL(m.amount) || receiptTotal;
              return (
                <div key={m.id} className="receipt-total-row">
                  <span>{def}</span>
                  <span>{fmt(amt)}</span>
                </div>
              );
            })}
            {change > 0 && (
              <div className="receipt-total-row" style={{ fontWeight: 600 }}>
                <span>Troco</span>
                <span>{fmt(change)}</span>
              </div>
            )}
          </div>

          {customer && (
            <>
              <div className="receipt-divider">— — — — — — — — — — — — —</div>
              <div style={{ textAlign: 'center', fontSize: 12, color: 'var(--fg-muted)' }}>
                <Icon name="sparkle" size={12} style={{ verticalAlign: -2, marginRight: 4 }} />
                +{Math.floor(receiptTotal)} pts adicionados · Saldo: {customer.points + Math.floor(receiptTotal)} pts
              </div>
            </>
          )}

          <div className="receipt-divider">— — — — — — — — — — — — —</div>
          <div style={{ textAlign: 'center', fontSize: 11, color: 'var(--fg-soft)', padding: '4px 0 8px' }}>
            Obrigado pela preferência!<br />Cupom não fiscal — Documento interno
          </div>
        </div>

        {/* Ações */}
        <div className="pay-foot" style={{ justifyContent: 'space-between', borderTop: '1px solid var(--line)' }}>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn"><Icon name="print" size={14} /> Imprimir</button>
            <button className="btn btn-ghost"><Icon name="phone" size={14} /> Enviar SMS</button>
          </div>
          <button className="btn btn-primary btn-lg" onClick={onClose}>
            <Icon name="plus" size={14} /> Novo pedido
          </button>
        </div>
      </div>
    </div>
  );
  }

  return null;
}

window.PaymentFlow = PaymentFlow;
