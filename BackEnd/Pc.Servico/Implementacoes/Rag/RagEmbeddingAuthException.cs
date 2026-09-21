namespace Pc.Servico.Implementacoes.Rag
{
    /// <summary>
    /// Falha de autenticação no provider de embeddings (401/403) — aborta reindex em lote.
    /// </summary>
    public sealed class RagEmbeddingAuthException : Exception
    {
        public int StatusCode { get; }

        public RagEmbeddingAuthException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
