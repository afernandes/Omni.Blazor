// Forneria Don Tonhão — pizza data
// All money values in BRL cents to avoid float drift.

const BRL = (cents) => 'R$ ' + (cents / 100).toFixed(2).replace('.', ',');

// Pricing strategies
//   'highest' — cobra o sabor mais caro
//   'average' — média dos sabores
const SHOP_PRICE_STRATEGY = 'highest'; // shop-level default

// ——— Tamanhos
const SIZES = [
  { id: 'g', name: 'Pizza Grande', desc: '40cm · 8 fatias', maxFlavors: 2, fromCents: 5900 },
  { id: 'b', name: 'Pizza Broto',  desc: '25cm · 4 fatias', maxFlavors: 2, fromCents: 3900 },
];

// ——— Sabores. priceBySize: cents per size id
const FLAVOR_GROUPS = ['Tradicionais', 'Especiais', 'Veganos', 'Doces'];

const FLAVORS = [
  { id: 'cal', name: 'Calabresa',         group: 'Tradicionais', ing: 'Calabresa fatiada, muçarela, cebola, orégano', tag: '', priceBySize: { g: 5900, b: 3900 } },
  { id: 'mar', name: 'Margherita',        group: 'Tradicionais', ing: 'Molho, muçarela, tomate fresco, manjericão', tag: 'veg', priceBySize: { g: 6400, b: 4400 } },
  { id: '4q',  name: '4 Queijos',         group: 'Especiais',    ing: 'Muçarela, gorgonzola, parmesão, catupiry', tag: 'veg', priceBySize: { g: 7400, b: 5200 } },
  { id: 'fc',  name: 'Frango c/ Catupiry', group: 'Tradicionais', ing: 'Frango desfiado, catupiry, orégano', tag: '', priceBySize: { g: 6900, b: 4800 } },
  { id: 'por', name: 'Portuguesa',        group: 'Tradicionais', ing: 'Presunto, ovos, cebola, azeitona, muçarela', tag: '', priceBySize: { g: 6900, b: 4800 } },
  { id: 'pep', name: 'Pepperoni',         group: 'Especiais',    ing: 'Pepperoni curado, muçarela, orégano', tag: '', priceBySize: { g: 7400, b: 5200 } },
  { id: 'nap', name: 'Napolitana',        group: 'Tradicionais', ing: 'Tomate, muçarela, alho, manjericão, azeite', tag: 'veg', priceBySize: { g: 6400, b: 4400 } },
  { id: 'atu', name: 'Atum',              group: 'Especiais',    ing: 'Atum, cebola, azeitona, muçarela', tag: '', priceBySize: { g: 6400, b: 4400 } },
  { id: '3qs', name: '3 Queijos Scala',   group: 'Especiais',    ing: 'Muçarela, gorgonzola, parmesão gratinado', tag: 'veg', priceBySize: { g: 7900, b: 5500 } },
  { id: 'bcr', name: 'Bacon Crocante',    group: 'Especiais',    ing: 'Bacon, cheddar, muçarela, orégano', tag: '', priceBySize: { g: 6900, b: 4800 } },
  { id: 'fch', name: 'Frango c/ Cheddar', group: 'Especiais',    ing: 'Frango, cheddar, cebola caramelizada', tag: '', priceBySize: { g: 7200, b: 5000 } },
  { id: 'brb', name: 'Brócolis c/ Bacon', group: 'Especiais',    ing: 'Brócolis, bacon, muçarela, alho', tag: '', priceBySize: { g: 6700, b: 4700 } },
];

// ——— Bordas
const BORDAS = [
  { id: 'no',  name: 'Sem borda',                cents: 0 },
  { id: 'fi',  name: 'Fina',                     cents: 0 },
  { id: 'tr',  name: 'Tradicional',              cents: 0 },
  { id: 'ca',  name: 'Recheada com Catupiry',    cents: 1000 },
  { id: 'ch',  name: 'Recheada com Cheddar',     cents: 1000 },
  { id: 'cc',  name: 'Recheada com Cream Cheese',cents: 1200 },
];

// ——— Extras (used in personalize sheet)
const EXTRAS = [
  { id: 'cat', name: 'Catupiry',            cents: 400 },
  { id: 'che', name: 'Cheddar extra',       cents: 400 },
  { id: 'bac', name: 'Bacon crocante',      cents: 500 },
  { id: 'muc', name: 'Muçarela extra',      cents: 300 },
  { id: 'ceb', name: 'Cebola caramelizada', cents: 400 },
  { id: 'ore', name: 'Orégano extra',       cents: 0 },
];

// ——— Categorias da home
const CATEGORIES = [
  { id: 'destaques',  name: 'Destaques', emoji: '🔥' },
  { id: 'salgadas',   name: 'Pizzas Salgadas', emoji: '🍕' },
  { id: 'doces',      name: 'Pizzas Doces', emoji: '🍫' },
  { id: 'veganas',    name: 'Veganas', emoji: '🌱' },
  { id: 'bebidas',    name: 'Bebidas', emoji: '🥤' },
  { id: 'adicionais', name: 'Adicionais', emoji: '🍟' },
];

// ——— Bebidas e adicionais
const DRINKS = [
  { id: 'd1', name: 'Coca-Cola 2L',                desc: 'Gelada, ideal para a pizza grande.',  cents: 1400, kind: 'drink' },
  { id: 'd2', name: 'Suco Natural de Laranja 500ml', desc: 'Espremido na hora, sem açúcar.',     cents: 1250, kind: 'drink' },
  { id: 'd3', name: 'Heineken Long Neck 330ml',    desc: 'Long neck gelado.',                    cents: 1190, kind: 'drink' },
  { id: 'd4', name: 'Água Mineral c/ Gás 500ml',   desc: 'Crystal com gás.',                     cents: 700,  kind: 'drink' },
];

// ——— Helpers
function flavorPrice(flavor, sizeId) { return flavor.priceBySize[sizeId] || 0; }

function combinedFlavorsPrice(flavorIds, sizeId, strategy = SHOP_PRICE_STRATEGY) {
  if (!flavorIds.length) return 0;
  const prices = flavorIds.map(id => {
    const f = FLAVORS.find(f => f.id === id);
    return f ? flavorPrice(f, sizeId) : 0;
  });
  if (strategy === 'average') {
    return Math.round(prices.reduce((a,b) => a+b, 0) / prices.length);
  }
  return Math.max(...prices);
}

function priorityFlavor(flavorIds, sizeId) {
  // returns flavor object whose price drives the total (in 'highest' strategy)
  let top = null;
  let topPr = -1;
  flavorIds.forEach(id => {
    const f = FLAVORS.find(f => f.id === id);
    if (!f) return;
    const pr = flavorPrice(f, sizeId);
    if (pr > topPr) { topPr = pr; top = f; }
  });
  return top;
}

window.PizzaData = {
  BRL, SIZES, FLAVOR_GROUPS, FLAVORS, BORDAS, EXTRAS, CATEGORIES, DRINKS,
  SHOP_PRICE_STRATEGY,
  flavorPrice, combinedFlavorsPrice, priorityFlavor,
};
