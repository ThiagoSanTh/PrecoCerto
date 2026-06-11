using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Interacoes;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Infraestrutura
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Admin> Admins { get; set; }

        public DbSet<Loja> Lojas { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Oferta> Ofertas { get; set; }

        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<HistoricoPesquisa> HistoricosPesquisa { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<PreferenciaCliente> PreferenciasClientes { get; set; }

        public DbSet<Conversa> Conversas { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Loja>()
                .HasOne(l => l.Usuario)
                .WithOne(u => u.LojaPropria)
                .HasForeignKey<Loja>(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.LojaVinculada)
                .WithMany()
                .HasForeignKey(u => u.LojaVinculadaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Loja)
                .WithMany()
                .HasForeignKey(p => p.LojaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Produto>()
                .HasIndex(p => p.LojaId);

            modelBuilder.Entity<Oferta>()
                .HasIndex(o => o.ProdutoId);

            modelBuilder.Entity<Mensagem>()
                .HasIndex(m => new { m.ConversaId, m.EnviadaEm });

            modelBuilder.Entity<HistoricoPesquisa>()
                .HasOne(h => h.Produto)
                .WithMany()
                .HasForeignKey(h => h.ProdutoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HistoricoPesquisa>()
                .HasOne(h => h.Loja)
                .WithMany()
                .HasForeignKey(h => h.LojaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conversa>()
                .HasIndex(c => new { c.ClienteId, c.LojaId })
                .IsUnique();

            modelBuilder.Entity<Conversa>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversa>()
                .HasOne(c => c.Loja)
                .WithMany()
                .HasForeignKey(c => c.LojaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Mensagem>()
                .HasOne(m => m.Conversa)
                .WithMany(c => c.Mensagens)
                .HasForeignKey(m => m.ConversaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
