namespace GenericRepository.Contracts.Generic
{
    public interface IRepositoryBulk<TEntity> where TEntity : class
    {
        Task AddBulkAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task SoftDeleteBulkAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task DeleteBulkAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
        Task UpdateBulkAsync(IEnumerable<TEntity> entities, int batchSize = 10000, CancellationToken cancellationToken = default);
    }
}