using Pc.Dominio.Entities.Base;
using Pc.Dominio.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Usuarios
{
    /// <summary>
    /// Entidade Admin: usuário administrativo com permissões de gerenciamento sistema
    /// Consolidada com dados de autenticação (sem intermediário Usuario)
    /// Tipo implícito: sempre TipoUsuario.Admin
    /// </summary>
    public class Admin : BaseEntity
    {
        // 🔐 Autenticação e Perfil
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public TipoUsuario Tipo { get; set; } = TipoUsuario.Admin; // Implícito: sempre Admin
        
        // 🛡️ Permissões e Níveis de Acesso
        /// <summary>
        /// Nivel de acesso: 1=SuperAdmin (total), 2=Admin (gerenciar usuários/lojas), 3=Moderador (suporte)
        /// </summary>
        public int NivelAcesso { get; set; } = 2; // Padrão: Admin

        /// <summary>
        /// Permissões customizadas em JSON ou flag separadas
        /// Ex: "gerenciar_usuarios,gerenciar_lojas,visualizar_relatorios"
        /// </summary>
        public string? Permissoes { get; set; }
    }
}
