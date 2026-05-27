// Demo data for the Forneria PDV prototype
const BRL = (n) => 'R$ ' + n.toFixed(2).replace('.', ',');

const CATEGORIES = [
  { id: 'all',     name: 'Todos' },
  { id: 'pizza',   name: 'Pizzas' },
  { id: 'burger',  name: 'Burgers' },
  { id: 'drink',   name: 'Bebidas' },
  { id: 'dessert', name: 'Sobremesas' },
  { id: 'entry',   name: 'Entradas' },
];

const PRODUCTS = [
  // Pizzas (configurable)
  { id: 'pz-m',  cat: 'pizza', name: 'Pizza Média',  price: 42.00, tag: 'CONFIG', configurable: true, size: 'M',  slices: 6 },
  { id: 'pz-g',  cat: 'pizza', name: 'Pizza Grande', price: 58.00, tag: 'CONFIG', configurable: true, size: 'G',  slices: 8 },
  { id: 'pz-gg', cat: 'pizza', name: 'Pizza Família', price: 72.00, tag: 'CONFIG', configurable: true, size: 'GG', slices: 12 },
  // Burgers
  { id: 'bg-1', code: '301', cat: 'burger', name: 'Cheeseburger Duplo', price: 28.00, tag: 'BURGER' },
  { id: 'bg-2', code: '302', cat: 'burger', name: 'Burger da Casa',     price: 32.00, tag: 'BURGER' },
  { id: 'bg-3', code: '303', cat: 'burger', name: 'Veggie Burger',      price: 26.00, tag: 'BURGER' },
  { id: 'bg-4', code: '304', cat: 'burger', name: 'Combo Smash + Fritas', price: 38.00, tag: 'COMBO' },
  // Drinks
  { id: 'dr-1', code: '201', cat: 'drink', name: 'Coca-Cola 350ml',   price: 7.50, tag: 'LATA' },
  { id: 'dr-2', code: '202', cat: 'drink', name: 'Guaraná 2L',        price: 14.00, tag: '2L' },
  { id: 'dr-3', code: '203', cat: 'drink', name: 'Suco de Laranja',   price: 9.00, tag: '500ML' },
  { id: 'dr-4', code: '204', cat: 'drink', name: 'Cerveja Long Neck', price: 10.00, tag: '355ML' },
  { id: 'dr-5', code: '205', cat: 'drink', name: 'Água com Gás',      price: 5.00, tag: '500ML' },
  // Desserts
  { id: 'ds-1', code: '401', cat: 'dessert', name: 'Petit Gâteau',      price: 22.00, tag: 'QUENTE' },
  { id: 'ds-2', code: '402', cat: 'dessert', name: 'Brownie c/ Sorvete', price: 18.00, tag: 'DOCE' },
  // Entradas
  { id: 'en-1', code: '501', cat: 'entry', name: 'Batata Rústica', price: 24.00, tag: 'PORÇÃO' },
  { id: 'en-2', code: '502', cat: 'entry', name: 'Polenta Frita',  price: 22.00, tag: 'PORÇÃO' },
];

const PIZZA_SIZES = [
  { size: 'M',  name: 'Média',   slices: 6,  maxFlavors: 2, priceRule: 'max' },
  { size: 'G',  name: 'Grande',  slices: 8,  maxFlavors: 2, priceRule: 'max' },
  { size: 'GG', name: 'Família', slices: 12, maxFlavors: 4, priceRule: 'max' },
];

const FLAVORS = [
  { id: 'cal', code: '101', name: 'Calabresa',          desc: 'Calabresa fatiada, muçarela, cebola, orégano',       prices: { M: 42, G: 58, GG: 72 },
    ingredients: ['Calabresa fatiada','Muçarela','Cebola','Orégano'] },
  { id: 'qua', code: '102', name: 'Quatro Queijos',     desc: 'Muçarela, provolone, gorgonzola, parmesão',         prices: { M: 52, G: 68, GG: 82 },
    ingredients: ['Muçarela','Provolone','Gorgonzola','Parmesão'] },
  { id: 'por', code: '103', name: 'Portuguesa',         desc: 'Presunto, ovo, ervilha, palmito, cebola, muçarela', prices: { M: 46, G: 62, GG: 76 },
    ingredients: ['Presunto','Ovo','Ervilha','Palmito','Cebola','Muçarela'] },
  { id: 'mrg', code: '104', name: 'Margherita',         desc: 'Muçarela de búfala, tomate, manjericão',            prices: { M: 46, G: 62, GG: 76 },
    ingredients: ['Muçarela de búfala','Tomate fresco','Manjericão'] },
  { id: 'pep', code: '105', name: 'Pepperoni',          desc: 'Molho, muçarela, pepperoni artesanal',              prices: { M: 48, G: 64, GG: 78 },
    ingredients: ['Pepperoni artesanal','Muçarela','Molho especial'] },
  { id: 'fra', code: '106', name: 'Frango c/ Catupiry', desc: 'Frango desfiado, catupiry, milho, muçarela',        prices: { M: 48, G: 64, GG: 78 },
    ingredients: ['Frango desfiado','Catupiry','Milho','Muçarela'] },
  { id: 'chc', code: '107', name: 'Chocolate Belga',    desc: 'Chocolate belga 70%, morangos frescos',             prices: { M: 54, G: 70, GG: 84 },
    ingredients: ['Chocolate belga 70%','Morango fresco'] },
  { id: 'vgn', code: '108', name: 'Veggie (vegana)',    desc: 'Abobrinha, berinjela, tomate seco, rúcula',         prices: { M: 46, G: 62, GG: 76 },
    ingredients: ['Abobrinha','Berinjela','Tomate seco','Rúcula'] },
];

const BORDAS = [
  { id: 'none',     name: 'Sem borda',             short: '—',  price: 0,  alias: ['none','sem','-'] },
  { id: 'catupiry', name: 'Recheada com Catupiry',  short: 'C',  price: 10, alias: ['c','cat','catupiry'] },
  { id: 'cheddar',  name: 'Recheada com Cheddar',   short: 'Ch', price: 10, alias: ['ch','che','cheddar'] },
  { id: 'cream',    name: 'Recheada com Cream Ch.', short: 'Cr', price: 12, alias: ['cr','cream'] },
];

const FLAVOR_EXTRAS = [
  { id: 'fe-cat',  name: 'Catupiry',           price: 4.00 },
  { id: 'fe-che',  name: 'Cheddar extra',       price: 4.00 },
  { id: 'fe-bac',  name: 'Bacon crocante',      price: 5.00 },
  { id: 'fe-muc',  name: 'Muçarela extra',      price: 3.00 },
  { id: 'fe-ceb',  name: 'Cebola caramelizada', price: 4.00 },
  { id: 'fe-ore',  name: 'Orégano extra',       price: 0.00 },
];

const INGREDIENTS = [
  { id: 'muc', name: 'Muçarela extra',    price: 6.00, removable: true, default: true },
  { id: 'bor', name: 'Borda recheada catupiry', price: 12.00, default: false },
  { id: 'cat', name: 'Catupiry',          price: 5.00, default: false },
  { id: 'bac', name: 'Bacon',             price: 7.00, default: false },
  { id: 'ceb', name: 'Cebola',            price: 0,    default: true, removable: true },
  { id: 'azt', name: 'Azeitona',          price: 0,    default: true, removable: true },
  { id: 'ore', name: 'Orégano',           price: 0,    default: true, removable: true },
];

const CUSTOMERS = [
  { id: 'c1', name: 'Marina Toledo',    phone: '(11) 98823-1204', address: 'R. das Laranjeiras, 412 — Centro', points: 842, orders: 23, coupon: { label: 'BEM-VINDO15', discount: '15%', value: 12.00, expires: '31/05' } },
  { id: 'c2', name: 'Rafael Okamoto',   phone: '(11) 99100-5512', address: 'Av. Paulista, 2200 — Bela Vista',  points: 210, orders: 7 },
  { id: 'c3', name: 'Juliana Pires',    phone: '(11) 98401-7720', address: 'R. Aspicuelta, 88 — Vila Madalena', points: 1320, orders: 34, coupon: { label: 'VIP10', discount: 'R$ 10', value: 10.00, expires: '15/06' } },
  { id: 'c4', name: 'Diego Salles',     phone: '(11) 97600-4401', address: 'R. Harmonia, 501 — Vila Madalena', points: 45, orders: 2 },
  { id: 'c5', name: 'Beatriz Monteiro', phone: '(11) 98250-9988', address: 'R. dos Três Irmãos, 120 — Morumbi',points: 580, orders: 14 },
];

const HUB_ORDERS = [
  { id: '#4812', channel: 'digital', customer: 'Marina Toledo', items: ['1× Pizza G Calabresa/Margherita', '1× Coca-Cola 2L'], total: 72, mins: 2, mode: 'Delivery' },
  { id: '#4813', channel: 'pdv',     customer: 'Mesa 07',        items: ['2× Cheeseburger Duplo', '2× Suco Laranja'],       total: 74, mins: 4, mode: 'Mesa' },
  { id: '#4814', channel: 'ifood',   customer: 'Pedro L.',       items: ['1× Pizza M Portuguesa', '1× Guaraná 2L'],          total: 56, mins: 1, mode: 'Delivery' },
  { id: '#4815', channel: 'garcom',  customer: 'Mesa 12',        items: ['1× Pizza GG Quatro Queijos/Pepperoni/Margherita/Frango','1× Cerveja', '1× Cerveja'], total: 102, mins: 6, mode: 'Mesa' },
  { id: '#4810', channel: 'digital', customer: 'Juliana Pires',  items: ['1× Veggie Burger', '1× Brownie c/ Sorvete'],       total: 44, mins: 9, mode: 'Retirada' },
  { id: '#4811', channel: 'pdv',     customer: 'Balcão',         items: ['1× Combo Smash + Fritas'],                         total: 38, mins: 12, mode: 'Balcão' },
  { id: '#4808', channel: 'ifood',   customer: 'Ana S.',         items: ['1× Pizza G Frango/Calabresa', '1× Coca-Cola 2L'],  total: 72, mins: 14, mode: 'Delivery' },
  { id: '#4809', channel: 'garcom',  customer: 'Mesa 03',        items: ['1× Batata Rústica','1× Cerveja'],                  total: 34, mins: 17, mode: 'Mesa' },
  { id: '#4805', channel: 'digital', customer: 'Rafael Okamoto', items: ['1× Pizza M Margherita', '1× Água c/ Gás'],         total: 51, mins: 22, mode: 'Delivery' },
  { id: '#4806', channel: 'pdv',     customer: 'Balcão',         items: ['2× Petit Gâteau'],                                 total: 44, mins: 25, mode: 'Balcão' },
];

Object.assign(window, { BRL, CATEGORIES, PRODUCTS, PIZZA_SIZES, FLAVORS, BORDAS, FLAVOR_EXTRAS, INGREDIENTS, CUSTOMERS, HUB_ORDERS });
