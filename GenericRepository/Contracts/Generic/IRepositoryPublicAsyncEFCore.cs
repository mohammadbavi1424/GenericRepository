using GenericRepository.Filters;
using System.Linq.Expressions;

namespace GenericRepository.Contracts.Generic
{
    public interface IRepositoryPublicAsyncEFCore<TEntity> 
        where TEntity : class
    {
        IQueryable<TEntity> TableDeleted { get; }
        IQueryable<TEntity> TableNoTrackingDeleted { get; }
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default, bool saveNow = true);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default, bool saveNow = true);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default, bool saveNow = true);
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default, bool saveNow = true);
        Task<TEntity> GetByIdAsync(CancellationToken cancellationToken = default, params object[] ids);
        Task<GreadData<TEntity>> GetListAsync(GreadData<TEntity> data,CancellationToken cancellationToken = default);
        Task<TEntity> GetByIdDeletedAsync(CancellationToken cancellationToken = default, params object[] ids);
        Task<GreadData<TEntity>> GetDeletedAsync(GreadData<TEntity> data,CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default, bool saveNow = true);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default, bool saveNow = true);
        Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty, CancellationToken cancellationToken = default) where TProperty : class;
        Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty, CancellationToken cancellationToken = default) where TProperty : class;
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        //Task AddBulkAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        //Task UpdateBulkAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        //Task DeleteBulkAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);




    }
}
