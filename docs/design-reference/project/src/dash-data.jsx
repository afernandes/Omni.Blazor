// ════════════════════════════════════════════════
// Dashboard — Mock data
// ════════════════════════════════════════════════
const BRL = (n) => 'R$ ' + n.toFixed(2).replace('.', ',');

// Sparkline data — receita por hora (12h às 22h)
const REVENUE_TODAY = [240, 320, 580, 920, 1080, 720, 380, 420, 680, 760, 540, 280];
const REVENUE_PREV  = [180, 290, 510, 780, 920, 640, 320, 380, 580, 690, 480, 240];
const HOURS_LBL = ['11h','12h','13h','14h','15h','16h','17h','18h','19h','20h','21h','22h'];

// Distribuição por tipo
const ORDER_TYPES = [
  { name: 'Mesas',     value: 2000, color: '#92400e' },
  { name: 'Delivery',  value: 1192, color: '#d97706' },
  { name: 'Balcão',    value: 655,  color: '#fbbf24' },
];

// Pedidos recentes
const RECENT_ORDERS = [
  { id: '4721', type: 'mesa',     typeName: 'Mesa 7',  customer: '—',                 items: '2× Margherita Grande, 1× Coca 2L',           value: 156.00, status: 'preparo',   time: '13:42' },
  { id: '4720', type: 'delivery', typeName: 'Delivery',customer: 'Ana Beatriz S.',    items: '1× Calabresa Família, 2× Brahma 600',       value: 98.50,  status: 'entregue',  time: '13:35' },
  { id: '4719', type: 'balcao',   typeName: 'Balcão',  customer: 'Cliente avulso',    items: '1× Cheeseburger Duplo, 1× Coca 350',        value: 35.50,  status: 'entregue',  time: '13:28' },
  { id: '4718', type: 'mesa',     typeName: 'Mesa 12', customer: 'Marina T.',         items: '1× Quattro Formaggi, 1× Tiramisu, 2× Vinho',value: 287.00, status: 'preparo',   time: '13:18' },
  { id: '4717', type: 'delivery', typeName: 'Delivery',customer: 'João P. Henrique',  items: '2× Pepperoni Média',                         value: 84.00,  status: 'cancelado', time: '13:10' },
  { id: '4716', type: 'mesa',     typeName: 'Mesa 3',  customer: 'Família Silva',     items: '2× Família Mista, 4× Suco Laranja',          value: 198.00, status: 'entregue',  time: '13:02' },
  { id: '4715', type: 'balcao',   typeName: 'Balcão',  customer: '—',                 items: '1× Combo Smash + Fritas',                    value: 38.00,  status: 'entregue',  time: '12:55' },
];

// Produtos mais vendidos
const TOP_PRODUCTS = [
  { rank: 1, name: 'Pizza Margherita Grande',  qty: 24, value: 1416, share: 100 },
  { rank: 2, name: 'Pizza Calabresa Família',  qty: 18, value: 1296, share: 92 },
  { rank: 3, name: 'Cheeseburger Duplo',       qty: 31, value: 868,  share: 61 },
  { rank: 4, name: 'Pizza Quattro Formaggi G', qty: 12, value: 720,  share: 51 },
  { rank: 5, name: 'Combo Smash + Fritas',     qty: 19, value: 722,  share: 51 },
];

// Mapa de mesas
const TABLES_STATE = [
  { n: 1,  status: 'livre' },
  { n: 2,  status: 'ocupada' },
  { n: 3,  status: 'ocupada' },
  { n: 4,  status: 'livre' },
  { n: 5,  status: 'conta' },
  { n: 6,  status: 'ocupada' },
  { n: 7,  status: 'ocupada' },
  { n: 8,  status: 'livre' },
  { n: 9,  status: 'ocupada' },
  { n: 10, status: 'livre' },
  { n: 11, status: 'conta' },
  { n: 12, status: 'ocupada' },
];

// Notificações
const DASH_NOTIFS = [
  { kind: 'danger', text: '3 pedidos atrasados há mais de 25min',           time: 'agora',  unread: true },
  { kind: 'warn',   text: 'Estoque baixo: Mussarela (3kg), Calabresa (2kg)', time: '5min',   unread: true },
  { kind: 'good',   text: 'Meta diária atingida — R$ 3.500',                 time: '12min',  unread: true },
  { kind: 'accent', text: 'Novo cliente cadastrado: Marina Toledo',          time: '20min',  unread: false },
];

// Pedidos vivos do operador
const OP_LIVE_ORDERS = [
  { id: '4721', typeName: 'Mesa 7',   customer: '—',              items: '2× Margherita G, 1× Coca 2L',  value: 156.00, wait: 8  },
  { id: '4720', typeName: 'Delivery', customer: 'Ana Beatriz S.', items: '1× Calabresa F, 2× Brahma',    value: 98.50,  wait: 12 },
  { id: '4718', typeName: 'Mesa 12',  customer: 'Marina T.',      items: '1× Quattro F, 1× Tiramisu',    value: 287.00, wait: 18 },
  { id: '4717', typeName: 'Balcão',   customer: 'Pedro M.',       items: '1× Combo Smash',                value: 38.00,  wait: 4  },
  { id: '4716', typeName: 'Mesa 3',   customer: 'Família Silva',  items: '2× Família Mista, 4× Suco',    value: 198.00, wait: 22 },
];

// Pool de novos pedidos para simulação ao vivo
const ORDER_POOL = [
  { type:'mesa',     typeName:'Mesa 8',   customer:'—',                  items:'1× Pepperoni Grande, 2× Coca 350', value:78.00 },
  { type:'delivery', typeName:'Delivery', customer:'Carlos M.',          items:'1× Família Mista',                  value:92.00 },
  { type:'balcao',   typeName:'Balcão',   customer:'—',                  items:'1× Burger da Casa, 1× Suco',        value:42.00 },
  { type:'mesa',     typeName:'Mesa 5',   customer:'Casal',              items:'2× Vinho, 1× Tábua de Frios',       value:165.00 },
  { type:'delivery', typeName:'Delivery', customer:'Bruna L.',           items:'1× Veggie Burger, 1× Petit Gâteau', value:48.00 },
];

// Legacy aliases for files that haven't been refactored yet
const DASH_BRL = BRL;
const DASH_SEED_ORDERS = RECENT_ORDERS;
const DASH_ORDER_POOL = ORDER_POOL;
const DASH_OP_LIVE_ORDERS = OP_LIVE_ORDERS;
// Notifications need legacy shape (id/read/color/action) for dash-shell.jsx
const DASH_NOTIFS_LEGACY = DASH_NOTIFS.map((n,i) => ({
  id: i, text: n.text, time: n.time, color: n.kind,
  read: !n.unread, action: null,
}));

Object.assign(window, {
  BRL, REVENUE_TODAY, REVENUE_PREV, HOURS_LBL, ORDER_TYPES,
  RECENT_ORDERS, TOP_PRODUCTS, TABLES_STATE,
  OP_LIVE_ORDERS, ORDER_POOL,
  // legacy
  DASH_BRL, DASH_SEED_ORDERS, DASH_ORDER_POOL, DASH_OP_LIVE_ORDERS,
  DASH_NOTIFS: DASH_NOTIFS_LEGACY,
});
