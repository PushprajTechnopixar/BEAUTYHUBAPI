using BeautyHubAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using BeautyHubAPI.Repository.IRepository;
using BeautyHubAPI.Data;

namespace BeautyHubAPI.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        internal DbSet<T> dbSet;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            this.dbSet = _context.Set<T>();
        }
        public async Task CreateEntity(T entity)
        {
            await dbSet.AddAsync(entity);
            await SaveEntity();
        }
        public async Task RemoveEntity(T entity)
        {
            dbSet.Remove(entity);
            await SaveEntity();
        }

        // save entity
        public async Task SaveEntity()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) { query = query.Where(filter); }
            return await query.ToListAsync();
        }
        public async Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true)
        {
            IQueryable<T> query = dbSet;
            if (!tracked)
            { query = query.AsNoTracking(); }
            if (filter != null) { query = query.Where(filter); }
            return await query.FirstOrDefaultAsync();
        }
    }
}
