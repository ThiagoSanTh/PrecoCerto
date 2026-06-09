import { createContext, useContext, useState, useCallback, useEffect } from 'react';
import {
  obterCarrinho,
  adicionarItemCarrinho,
  atualizarItemCarrinho,
  removerItemCarrinho,
  limparCarrinho,
} from '../services/carrinhoService';
import { useAuth } from './AuthContext';

const CarrinhoContext = createContext(null);

export function CarrinhoProvider({ children }) {
  const { session, isCliente } = useAuth();
  const clienteId = isCliente ? session?.perfil?.id : null;

  const [carrinho, setCarrinho] = useState(null);
  const [loading, setLoading] = useState(false);

  const recarregar = useCallback(async () => {
    if (!clienteId) {
      setCarrinho(null);
      return;
    }
    setLoading(true);
    try {
      const data = await obterCarrinho(clienteId);
      setCarrinho(data);
    } catch {
      setCarrinho(null);
    } finally {
      setLoading(false);
    }
  }, [clienteId]);

  useEffect(() => {
    recarregar();
  }, [recarregar]);

  async function adicionar(item) {
    if (!clienteId) return;
    const data = await adicionarItemCarrinho(clienteId, item);
    setCarrinho(data);
  }

  async function atualizarQuantidade(itemId, quantidade) {
    if (!clienteId) return;
    const data = await atualizarItemCarrinho(clienteId, itemId, quantidade);
    setCarrinho(data);
  }

  async function remover(itemId) {
    if (!clienteId) return;
    const data = await removerItemCarrinho(clienteId, itemId);
    setCarrinho(data);
  }

  async function limpar() {
    if (!clienteId) return;
    await limparCarrinho(clienteId);
    await recarregar();
  }

  const quantidadeItens = carrinho?.quantidadeItens ?? 0;

  return (
    <CarrinhoContext.Provider
      value={{
        carrinho,
        loading,
        quantidadeItens,
        recarregar,
        adicionar,
        atualizarQuantidade,
        remover,
        limpar,
      }}
    >
      {children}
    </CarrinhoContext.Provider>
  );
}

export function useCarrinho() {
  const ctx = useContext(CarrinhoContext);
  if (!ctx) throw new Error('useCarrinho deve ser usado dentro de CarrinhoProvider');
  return ctx;
}
