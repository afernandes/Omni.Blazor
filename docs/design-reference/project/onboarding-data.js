// ——————————————————————————————————————————
// Forneria — Onboarding seed data
// ——————————————————————————————————————————

const MENU_MODELS = [
  { group: "Comidas Quentes", items: [
    { id:"pizzas",      emoji:"🍕", name:"Pizzas",                       desc:"Pizzas clássicas e especiais com sabores, ingredientes e ficha técnica completa.",         preview:["Margherita","Calabresa","Portuguesa","4 Queijos","Frango c/ Catupiry","Pepperoni","Mussarela","Napolitana","Atum","Brócolis c/ Bacon","Chocolate","Banana c/ Canela","Romeu e Julieta"], cats:["Tradicionais","Especiais","Doces"],                                    count:25 },
    { id:"burgers",     emoji:"🍔", name:"Hambúrgueres & Smash Burgers",  desc:"Hambúrgueres artesanais, smash burgers, chicken burgers e combos completos.",             preview:["Classic Burger","Smash Duplo","BBQ Bacon","Crispy Chicken","Veggie Burger","X-Tudo","Fish Burger"],                                                                cats:["Tradicionais","Smash","Chicken","Vegetarianos","Combos"],              count:18 },
    { id:"mexicano",    emoji:"🌮", name:"Mexicano & Tex-Mex",            desc:"Tacos, burritos, quesadillas, nachos e pratos típicos mexicanos.",                        preview:["Taco de Carne","Burrito Frango","Quesadilla Queijo","Nachos Supreme","Bowl Mexicano"],                                                                          cats:["Tacos","Burritos","Bowls","Entradas"],                                 count:20 },
    { id:"massas",      emoji:"🍝", name:"Massas & Restaurante Italiano",  desc:"Massas frescas e secas, risotos, escalopes e pratos italianos clássicos.",                preview:["Spaghetti Carbonara","Fettuccine ao Molho Branco","Lasanha Bolonhesa","Risoto de Funghi","Gnocchi ao Pesto","Ravioli 4 Queijos"],                             cats:["Massas","Risotos","Pratos Especiais"],                                 count:22 },
    { id:"frango",      emoji:"🍗", name:"Frango & Grelhados",            desc:"Pratos com frango grelhado, assado, empanado e acompanhamentos.",                         preview:["Frango Grelhado","Frango à Parmegiana","Peito Recheado","Frango na Chapa","Strogonoff de Frango"],                                                          cats:["Grelhados","Empanados","Caldos"],                                      count:16 },
    { id:"churrasco",   emoji:"🥩", name:"Churrasco & Carnes",            desc:"Cortes bovinos, suínos e acompanhamentos típicos de churrascaria.",                       preview:["Picanha","Fraldinha","Costelinha BBQ","Cupim","Linguiça Toscana","Maminha ao Alho","Contrafilé"],                                                          cats:["Bovinos","Suínos","Acompanhamentos"],                                  count:20 },
    { id:"frutos-mar",  emoji:"🐟", name:"Frutos do Mar & Pescados",      desc:"Pratos com peixes, camarões, lulas e mariscos.",                                          preview:["Camarão na Moranga","Moqueca de Peixe","Tilápia Grelhada","Polvo ao Azeite","Bolinho de Bacalhau","Paella"],                                               cats:["Peixes","Frutos do Mar","Especiais"],                                  count:18 },
    { id:"japones",     emoji:"🍜", name:"Japonês & Oriental",            desc:"Sushis, sashimis, hot rolls, temakis, ramen, udon e pratos orientais.",                   preview:["Salmão Nigiri","Hot Roll Filadélfia","Temaki Atum","Ramen Tonkotsu","Gyoza","Yakisoba","Edamame","Maki Califórnia"],                                      cats:["Sushis & Sashimis","Hot Rolls","Temakis","Ramen & Pratos Quentes"],   count:35 },
    { id:"indiano",     emoji:"🍛", name:"Indiana & Árabe",               desc:"Curries, esfihas, quibes, kebabs, hummus e pratos do Oriente Médio e Índia.",             preview:["Esfiha Aberta","Quibe Frito","Hummus","Frango Tikka Masala","Curry de Grão-de-Bico","Kebab","Pão Sírio"],                                                  cats:["Árabes","Indianos","Acompanhamentos"],                                 count:20 },
    { id:"fit",         emoji:"🥗", name:"Saudável & Fit",                desc:"Bowls, saladas, wraps, pratos low carb e opções para dietas especiais.",                  preview:["Bowl Proteico","Salada Caesar","Wrap de Frango","Açaí Tigela","Omelete Fit","Tapioca Recheada"],                                                          cats:["Bowls","Saladas","Low Carb","Vegano"],                                 count:20 },
    { id:"selfservice", emoji:"🥘", name:"Por Quilo / Self-Service",      desc:"Pratos típicos de restaurante executivo e self-service com peso variável.",               preview:["Arroz","Feijão","Carne Assada","Frango Ensopado","Legumes Refogados","Farofa","Saladas diversas"],                                                          cats:["Pratos Quentes","Saladas","Proteínas","Guarnições"],                   count:30 },
    { id:"brasileira",  emoji:"🍲", name:"Comida Brasileira Caseira",     desc:"Pratos típicos brasileiros, marmitas e comida do dia a dia.",                             preview:["Feijoada","Coxinha","Pão de Queijo","Caldo Verde","Tutu de Feijão","Rabada","Mocotó"],                                                                    cats:["Pratos do Dia","Caldos & Sopas","Salgados"],                           count:22 },
  ]},
  { group: "Petiscos, Entradas & Fast Food", items: [
    { id:"petiscos",    emoji:"🍟", name:"Petiscos, Porções & Entradas",  desc:"Porções para compartilhar, frituras, tábuas e entradas.",                                preview:["Batata Frita","Onion Rings","Isca de Frango","Tábua de Frios","Bolinhos de Bacalhau","Dadinho de Tapioca"],                                              cats:["Porções","Frituras","Tábuas","Entradas"],                              count:18 },
    { id:"lanches",     emoji:"🌭", name:"Lanches & Cachorros-Quentes",   desc:"Cachorros-quentes, bauru, misto quente e lanches rápidos.",                               preview:["Cachorro-Quente Tradicional","Bauru","Club Sandwich","Misto Quente","Beirute"],                                                                          cats:["Cachorros-Quentes","Sanduíches","Especiais"],                          count:14 },
    { id:"combinado",   emoji:"🍕🍔", name:"Cardápio Combinado (Pizzaria + Burger)", desc:"Combinação de pizzas e hambúrgueres — ideal para estabelecimentos híbridos.",   preview:["Inclui modelos Pizza + Hambúrguer em carga integrada"],                                                                                                cats:["Pizzas","Hambúrgueres","Combos"],                                      count:40 },
  ]},
  { group: "Bebidas", items: [
    { id:"bebidas-na",  emoji:"🥤", name:"Bebidas Não Alcoólicas",        desc:"Refrigerantes, sucos naturais, vitaminas, chás e água.",                                  preview:["Coca-Cola","Guaraná","Suco de Laranja","Vitamina de Banana","Chá Gelado","Limonada Suíça"],                                                            cats:["Refrigerantes","Sucos","Vitaminas","Chás"],                            count:20 },
    { id:"bar",         emoji:"🍺", name:"Bebidas Alcoólicas & Bar",      desc:"Cervejas, drinques, coquetéis, doses e carta de vinhos.",                                  preview:["Heineken","Brahma","Skol","Caipirinha","Mojito","Gin Tônica","Aperol Spritz","Vinho Tinto/Branco"],                                                    cats:["Cervejas","Drinques","Doses","Vinhos"],                                count:25 },
    { id:"cafe",        emoji:"☕", name:"Cafeteria & Café",              desc:"Espressos, cappuccinos, cafés especiais, chás e bebidas quentes.",                         preview:["Espresso","Cappuccino","Flat White","Latte","Cold Brew","Matcha Latte","Chocolate Quente"],                                                             cats:["Espressos","Especiais","Frios","Chás"],                                count:18 },
    { id:"bubbletea",   emoji:"🧋", name:"Bubble Tea & Bebidas Asiáticas",desc:"Bubble teas, matcha, chás gelados asiáticos e drinks com pérolas.",                        preview:["Taro Bubble Tea","Matcha Latte","Brown Sugar Boba","Kumquat Lemon Tea","Thai Tea"],                                                                    cats:["Bubble Tea","Matcha","Chás Asiáticos"],                                count:15 },
    { id:"drinks",      emoji:"🥂", name:"Drinks & Coquetéis Autorais",  desc:"Coquetéis assinatura, mocktails e drinks especiais da casa.",                               preview:["Mojito Autoral","Margarita da Casa","Negroni","Aperol Spritz","Hugo","White Russian"],                                                                  cats:["Autorais","Mocktails","Clássicos"],                                    count:16 },
  ]},
  { group: "Sobremesas", items: [
    { id:"sobremesas",  emoji:"🍰", name:"Sobremesas & Doces",            desc:"Bolos, tortas, pudins, sorvetes e sobremesas clássicas.",                                  preview:["Petit Gâteau","Pudim","Cheesecake","Brownie","Mousse de Maracujá","Sorvete 3 bolas","Crepe Doce"],                                                    cats:["Tortas & Bolos","Gelados","Clássicas"],                                count:18 },
    { id:"confeitaria", emoji:"🧁", name:"Cafeteria & Confeitaria",       desc:"Cupcakes, muffins, croissants, pães artesanais e vitrine de confeitaria.",                 preview:["Cupcake Red Velvet","Muffin de Blueberry","Croissant de Amêndoas","Pão de Fermentação Natural","Bolo de Cenoura"],                                   cats:["Bolos","Pães","Salgados de Forno"],                                    count:20 },
    { id:"sorveteria",  emoji:"🍦", name:"Sorveteria & Açaí",             desc:"Sorvetes artesanais, açaí com complementos, milkshakes e sundaes.",                        preview:["Açaí 300ml/500ml","Sorvete Artesanal (sabores variados)","Milkshake","Sundae","Casquinha"],                                                            cats:["Açaí","Sorvetes","Shakes"],                                            count:20 },
  ]},
  { group: "Especiais", items: [
    { id:"marmitas",    emoji:"🍱", name:"Marmitas & Delivery",           desc:"Marmitas executivas, quentinhas e combos para delivery.",                                  preview:["Marmita P/M/G com proteína + acompanhamento","Combo Executivo","Marmita Fit","Marmita Kids"],                                                          cats:["Marmitas","Combos","Fit"],                                             count:15 },
    { id:"kids",        emoji:"👶", name:"Menu Kids",                     desc:"Pratos infantis com porções menores e apresentação lúdica.",                               preview:["Mini Burguer Kids","Nuggets com Fritas","Macarrão com Molho","Pizza Mini","Suco de Caixinha"],                                                          cats:["Pratos","Lanches","Bebidas Kids"],                                     count:12 },
    { id:"vegano",      emoji:"🌱", name:"Vegano & Vegetariano",          desc:"Pratos 100% veganos e vegetarianos com proteínas vegetais.",                               preview:["Hambúrguer de Grão-de-Bico","Wrap Vegano","Bowl de Quinoa","Strogonoff de Cogumelos","Falafel"],                                                      cats:["Vegano","Vegetariano","Sem Glúten"],                                   count:18 },
    { id:"eventos",     emoji:"🎂", name:"Cardápio de Eventos & Buffet",  desc:"Itens para eventos, bandejas, finger foods e buffet por pessoa.",                         preview:["Bandeja de Salgados","Coxinhas por dúzia","Espetinhos","Mini Quiches","Bolo Decorado"],                                                                cats:["Salgados","Doces","Pacotes"],                                          count:20 },
    { id:"zero",        emoji:"⚙️", name:"Começar do zero",               desc:"Prefiro montar meu cardápio completamente do zero, sem modelos pré-criados.",             preview:[], cats:[], count:0, exclusive:true },
  ]},
];

const DAYS = [
  {id:'dom', short:'Dom', label:'Domingo'},
  {id:'seg', short:'Seg', label:'Segunda'},
  {id:'ter', short:'Ter', label:'Terça'},
  {id:'qua', short:'Qua', label:'Quarta'},
  {id:'qui', short:'Qui', label:'Quinta'},
  {id:'sex', short:'Sex', label:'Sexta'},
  {id:'sab', short:'Sáb', label:'Sábado'},
];

const ROLES = ['Gerente','Caixa','Garçom','Cozinheiro','Entregador','Atendente'];

const BRAND_COLORS = [
  {val:'#d97706',label:'Âmbar'},
  {val:'#dc2626',label:'Vermelho'},
  {val:'#059669',label:'Verde'},
  {val:'#2563eb',label:'Azul'},
  {val:'#7c3aed',label:'Violeta'},
  {val:'#db2777',label:'Rosa'},
  {val:'#ea580c',label:'Laranja'},
];
