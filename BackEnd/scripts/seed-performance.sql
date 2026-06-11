-- Seed para benchmarks de desempenho (NÃO executar em produção).
-- Gera lojas, produtos e ofertas sintéticos para testar paginação e índices.

-- Exemplo: 100 lojas, 5000 produtos (~50 por loja), 5000 ofertas
-- Ajuste os loops conforme necessário.

DO $$
DECLARE
  i INT;
  j INT;
  loja_id UUID;
  endereco_id UUID;
  prod_id UUID;
BEGIN
  FOR i IN 1..100 LOOP
    endereco_id := gen_random_uuid();
    loja_id := gen_random_uuid();

    INSERT INTO "Enderecos" ("Id", "Cep", "Logradouro", "Cidade", "Estado", "Latitude", "Longitude", "DataCriacao", "DataAtualizacao", "Ativo")
    VALUES (
      endereco_id,
      '01310-100',
      'Rua Benchmark ' || i,
      'São Paulo',
      'SP',
      -23.55 + (random() * 0.1),
      -46.63 + (random() * 0.1),
      NOW(), NOW(), true
    );

    INSERT INTO "Lojas" ("Id", "NomeFantasia", "EnderecoId", "DataCriacao", "DataAtualizacao", "Ativo")
    VALUES (
      loja_id,
      'Loja Benchmark ' || i,
      endereco_id,
      NOW(), NOW(), true
    );

    FOR j IN 1..50 LOOP
      prod_id := gen_random_uuid();
      INSERT INTO "Produtos" ("Id", "NomeProduto", "Preco", "LojaId", "Categoria", "DataCriacao", "DataAtualizacao", "Ativo")
      VALUES (
        prod_id,
        'Produto ' || i || '-' || j || ' arroz feijão',
        (random() * 50 + 1)::numeric(10,2),
        loja_id,
        0,
        NOW(), NOW(), true
      );

      INSERT INTO "Ofertas" ("Id", "ProdutoId", "LojaId", "Preco", "Disponivel", "EmPromocao", "DataAtualizacaoPreco", "DataCriacao", "DataAtualizacao", "Ativo")
      VALUES (
        gen_random_uuid(),
        prod_id,
        loja_id,
        (random() * 45 + 1)::numeric(10,2),
        true,
        (random() > 0.7),
        NOW(),
        NOW(), NOW(), true
      );
    END LOOP;
  END LOOP;
END $$;
