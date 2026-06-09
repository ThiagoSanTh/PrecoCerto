// Espelha o enum CategoriaProduto do backend (valor inteiro -> rótulo).
export const CATEGORIAS_PRODUTO = [
  { valor: 0, label: 'Outros' },
  { valor: 1, label: 'Alimentos' },
  { valor: 2, label: 'Bebidas' },
  { valor: 3, label: 'Hortifruti' },
  { valor: 4, label: 'Padaria' },
  { valor: 5, label: 'Açougue' },
  { valor: 6, label: 'Frios' },
  { valor: 7, label: 'Congelados' },
  { valor: 8, label: 'Limpeza' },
  { valor: 9, label: 'Higiene Pessoal' },
  { valor: 10, label: 'Farmácia' },
  { valor: 11, label: 'Bebês' },
  { valor: 12, label: 'Petshop' },
  { valor: 13, label: 'Casa' },
  { valor: 14, label: 'Eletrônicos' },
  { valor: 15, label: 'Vestuário' },
];

export function labelCategoria(valor) {
  const item = CATEGORIAS_PRODUTO.find((c) => c.valor === Number(valor));
  return item ? item.label : 'Outros';
}
