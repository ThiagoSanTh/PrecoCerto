---
trigger: always_on
description: Sempre criar teste automatizado de regressão ao corrigir qualquer bug.
---

## Testes automatizados obrigatórios em correções de bug

Sempre que um bug, erro ou issue for reportado e corrigido neste projeto:

1. **Criar teste automatizado em cima do bug** — nunca corrigir apenas o código de produção sem cobertura automatizada que reproduza o cenário exato da falha.
2. **Ciclo RED → GREEN** — o teste deve falhar contra o comportamento bugado e passar após a correção, protegendo contra regressões futuras.
3. **Onde colocar o teste:**
   - **Mobile / Web (`Mobile/`):** teste unitário/regressão em `Mobile/src/**/__tests__/*.test.mjs` (executável via `npm run test:unit`) e, quando o bug envolver fluxo de tela/navegação Web, teste E2E Playwright em `Mobile/e2e/*.spec.mjs`.
   - **BackEnd (`BackEnd/`):** teste automatizado em `BackEnd/Pc.Servico.Tests/` (executável via `dotnet test`).
4. **Verificação obrigatória** — rodar os testes automatizados antes de considerar a correção concluída ou abrir/atualizar PR.
