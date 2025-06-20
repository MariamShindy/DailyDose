namespace News.Core.Contracts.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<IReadOnlyList<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? include = null);

        IEnumerable<T> Find(Func<T, bool> predicate); 

        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);

        void Update(T entity); 
        void Delete(T entity); 

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null);
    }
}
