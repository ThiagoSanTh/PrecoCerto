-- Remove tabelas de carrinho (issue #47). Execute antes da migration se necessário.
DROP TABLE IF EXISTS "ItensCarrinho";
DROP TABLE IF EXISTS "Carrinhos";
