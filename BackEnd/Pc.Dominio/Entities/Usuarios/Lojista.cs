using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Usuarios
{
    /// <summary>
    /// Entidade Lojista: funcionário/gerente de loja que gerencia produtos e preços
    /// Consolidada com dados de autenticação (sem intermediário Usuario)
    /// Tipo implícito: sempre TipoUsuario.Lojista
    /// </summary>
    public class Lojista : BaseEntity
    {
        // 🔐 Autenticação e Perfil
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public TipoUsuario Tipo { get; set; } = TipoUsuario.Lojista; // Implícito: sempre Lojista
        
        // 🏪 Dados de Funcionário
        // O lojista pode se cadastrar antes de criar/vincular uma loja.
        public Guid? LojaId { get; set; }
        public Loja? Loja { get; set; }
        public string? Cargo { get; set; } // Ex: gerente, vendedor, estoquista
    }
}
