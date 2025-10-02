using Common.PagedResult;
using System.Linq.Expressions;

namespace Common.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllNoTrackingAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);

        // Include-capable helpers
        Task<IEnumerable<T>> GetAllIncludingAsync(params Expression<Func<T, object>>[] includeProperties);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);

        // Server-side pagination (orderBy is required for stable paging)
        Task<PagedResult<T>> FindPagedAsync(
            Expression<Func<T, bool>> predicate,
            int page,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
            params Expression<Func<T, object>>[] includeProperties);
    }
}