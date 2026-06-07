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

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Lojista> Lojistas { get; set; }
        public DbSet<Admin> Admins { get; set; }

        public DbSet<Loja> Lojas { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Oferta> Ofertas { get; set; }

        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<HistoricoPesquisa> HistoricosPesquisa { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<PreferenciaCliente> PreferenciasClientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Loja>()
                .HasOne(l => l.Lojista)
                .WithOne(lo => lo.Loja)
                // A loja é criada depois do cadastro do lojista, então o vínculo é opcional no início.
                .HasForeignKey<Loja>(l => l.LojistaId);

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Loja)
                .WithMany()
                .HasForeignKey(p => p.LojaId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
