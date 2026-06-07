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
        // Vínculo com a loja é definido em Loja.LojistaId (relação 1:1); use a navegação Loja.
        public Loja? Loja { get; set; }
        public string? Cargo { get; set; } // Ex: gerente, vendedor, estoquista
    }
}
