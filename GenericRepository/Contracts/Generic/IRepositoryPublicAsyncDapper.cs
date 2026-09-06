using GenericRepositories.Filters;

namespace GenericRepositories.Contracts.Generic
{
    public interface IRepositoryPublicAsyncDapper<TEntity> where TEntity : class
    {
        
        Task<TEntity> GetByIdQueryAsync(params object[] Id);
        Task<GreadData<TEntity>> GetByIdDeletedItemQueryAsync(params object[] Id);
        Task<GreadData<TEntity>> GetByQueryAsync(CancellationToken cancellationToken, GreadData<TEntity> data);
        Task<GreadData<TEntity>> GetByQueryDeletedItemsAsync(CancellationToken cancellationToken, GreadData<TEntity> data);
        Task<GreadData<TEntity>> GetByRangIdQuerAsync(params object[] Ids);
        Task<bool> AddByDapperAsync(TEntity entity);
        Task<bool> UpdateByDapperAsync(TEntity entity);

    }
}
