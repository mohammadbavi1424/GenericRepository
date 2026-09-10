namespace GenericRepository.Contracts.Generic
{
    public interface IRepositoryBulk<TEntity> where TEntity : class
    {
        Task BulkDeleteAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task BulkInsertAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task BulkSoftDeleteAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task BulkUpdateAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
    }
}