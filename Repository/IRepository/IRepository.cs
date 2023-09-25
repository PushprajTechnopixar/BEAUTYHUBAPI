using BeautyHubAPI.Models;
using System.Linq.Expressions;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task CreateEntity(T entity);
        Task RemoveEntity(T entity);
        Task SaveEntity();
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
        Task<T> GetAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true);
    }
}
