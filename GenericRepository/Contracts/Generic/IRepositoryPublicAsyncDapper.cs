using GenericRepository.Filters;

namespace GenericRepository.Contracts.Generic
{
    public interface IRepositoryPublicAsyncDapper<TEntity> where TEntity : class
    {
        
        Task<TEntity> GetByIdQueryAsync(params object[] Id);
        Task<GreadData<TEntity>> GetByIdDeletedItemQueryAsync(params object[] Id);
        Task<GreadData<TEntity>> GetByQueryAsync(GreadData<TEntity> data,CancellationToken cancellationToken = default);
        Task<GreadData<TEntity>> GetByQueryDeletedItemsAsync(GreadData<TEntity> data, CancellationToken cancellationToken = default);
        Task<GreadData<TEntity>> GetByRangIdQuerAsync(params object[] Ids);
        Task<bool> AddByDapperAsync(TEntity entity);
        Task<bool> UpdateByDapperAsync(TEntity entity);

    }
}
