using GenericRepositories.Filters;
using System.Linq.Expressions;

namespace GenericRepositories.Contracts.Generic
{
    public interface IRepositoryPublicAsyncEFCore<TEntity> 
        where TEntity : class
    {
        IQueryable<TEntity> TableDeleted { get; }
        IQueryable<TEntity> TableNoTrackingDeleted { get; }
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);
        Task<GreadData<TEntity>> GetListAsync(CancellationToken cancellationToken, GreadData<TEntity> data);
        Task<TEntity> GetByIdDeletedAsync(CancellationToken cancellationToken, params object[] ids);
        Task<GreadData<TEntity>> GetDeletedAsync(CancellationToken cancellationToken, GreadData<TEntity> data);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty, CancellationToken cancellationToken) where TProperty : class;
        Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty, CancellationToken cancellationToken) where TProperty : class;
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitTransactionAsync(CancellationToken cancellationToken);
        Task RollbackTransactionAsync(CancellationToken cancellationToken);



    }
}
