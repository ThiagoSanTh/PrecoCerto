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

        public DbSet<Carrinho> Carrinhos { get; set; }
        public DbSet<ItemCarrinho> ItensCarrinho { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Loja>()
                .HasOne(l => l.Lojista)
                .WithOne(lo => lo.Loja)
                // FK em Loja.LojistaId: lojista pode existir antes da loja ser criada.
                .HasForeignKey<Loja>(l => l.LojistaId);

            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Loja)
                .WithMany()
                .HasForeignKey(p => p.LojaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Histórico de pesquisa: vínculos opcionais com produto/loja (BI).
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

            // Carrinho 1:N ItemCarrinho (remoção em cascata dos itens).
            modelBuilder.Entity<Carrinho>()
                .HasMany(c => c.Itens)
                .WithOne(i => i.Carrinho)
                .HasForeignKey(i => i.CarrinhoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemCarrinho>()
                .HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemCarrinho>()
                .HasOne(i => i.Oferta)
                .WithMany()
                .HasForeignKey(i => i.OfertaId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
