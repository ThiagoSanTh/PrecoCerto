using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using System;
using System.Collections.Generic;

namespace Pc.Dominio.Entities.Usuarios
{
    /// <summary>
    /// Entidade Usuario: representa qualquer pessoa autenticada no app.
    /// Substitui as antigas entidades Cliente e Lojista (hierarquia unificada).
    /// O <see cref="Papel"/> define se é apenas Cliente, Lojista (abriu loja com CNPJ válido)
    /// ou Vendedor (cliente promovido por um lojista para gerenciar estoque).
    /// </summary>
    public class Usuario : BaseEntity
    {
        // 🔐 Autenticação e Perfil
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime? UltimoLogin { get; set; }

        // Mantido por compatibilidade com o JWT/role (Cliente/Lojista/Admin).
        public TipoUsuario Tipo { get; set; } = TipoUsuario.Cliente;

        // 🎭 Papel derivado das ações do usuário.
        public PapelUsuario Papel { get; set; } = PapelUsuario.Cliente;

        // ✉️ Confirmação de e-mail
        public bool EmailConfirmado { get; set; } = false;
        public string? TokenConfirmacao { get; set; }

        // 🔑 Recuperação de senha
        public string? TokenRecuperacaoSenha { get; set; }
        public DateTime? TokenRecuperacaoExpira { get; set; }

        // 📍 Geolocalização
        public decimal? LatitudeAtual { get; set; }
        public decimal? LongitudeAtual { get; set; }

        // 🏪 Dados de Vendedor/Lojista
        // Cargo é usado para vendedores (ex.: vendedor, estoquista).
        public string? Cargo { get; set; }
        // Loja na qual o usuário atua como Vendedor (controle de estoque).
        public Guid? LojaVinculadaId { get; set; }
        public Loja? LojaVinculada { get; set; }

        // 🔗 Relacionamentos
        // Loja da qual este usuário é proprietário (quando Papel == Lojista).
        public Loja? LojaPropria { get; set; }
        public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
        public ICollection<HistoricoPesquisa> HistoricosPesquisa { get; set; } = new List<HistoricoPesquisa>();
        public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
        public ICollection<PreferenciaCliente> Preferencias { get; set; } = new List<PreferenciaCliente>();
    }
}
