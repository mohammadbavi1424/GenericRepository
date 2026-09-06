namespace GenericRepositories.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryAdd<TEntity>
        where TEntity : class
    {
        Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task AddDtoAsync<TDtoCreate>(TDtoCreate dto, CancellationToken cancellationToken, bool saveNow = true);
        Task AddDtoRangeAsync<TDtoCreate>(IEnumerable<TDtoCreate> dtos, CancellationToken cancellationToken, bool saveNow = true);
        void Add(TEntity entity, bool saveNow = true);
        void AddRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void AddDto<TDtoCreate>(TDtoCreate dto, bool saveNow = true);
        void AddDtoRange<TDtoCreate>(IEnumerable<TDtoCreate> dtos, bool saveNow = true);

    }
}
