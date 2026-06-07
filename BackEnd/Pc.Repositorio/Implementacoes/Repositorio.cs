using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class Repositorio<T> : IRepositorio<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repositorio(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<T> AdicionarAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T?> ObterPorIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<List<T>> ListarAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task AtualizarAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task RemoverAsync(Guid id)
        {
            var entity = await ObterPorIdAsync(id);

            if (entity is null)
                return;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}