using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Usuarios
{
    /// <summary>
    /// Entidade Cliente: usuário final que busca produtos e compara preços
    /// Consolidada com dados de autenticação (sem intermediário Usuario)
    /// Tipo implícito: sempre TipoUsuario.Cliente
    /// </summary>
    public class Cliente : BaseEntity
    {
        // 🔐 Autenticação e Perfil
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public TipoUsuario Tipo { get; set; } = TipoUsuario.Cliente; // Implícito: sempre Cliente
        
        // 📍 Geolocalização
        public decimal? LatitudeAtual { get; set; }
        public decimal? LongitudeAtual { get; set; }
        
        // 🔗 Relacionamentos
        public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
        public ICollection<HistoricoPesquisa> HistoricosPesquisa { get; set; } = new List<HistoricoPesquisa>();
        public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
        public ICollection<PreferenciaCliente> Preferencias { get; set; } = new List<PreferenciaCliente>();
    }
}
