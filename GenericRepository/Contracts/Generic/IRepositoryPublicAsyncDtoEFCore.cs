using GenericRepository.Filters;
using System.Linq.Expressions;

namespace GenericRepository.Contracts.Generic
{
    public interface IRepositoryPublicAsyncDtoEFCore<TEntity, TDto> :
        IRepositoryPublicAsyncEFCore<TEntity> 
        where TEntity : class
        where TDto : class
    {
       
        Task AddDtoAsync(TDto dto, CancellationToken cancellationToken = default, bool saveNow = true);
        Task UpdateDtoAsync(TDto dto, CancellationToken cancellationToken = default, bool saveNow = true);
        Task<TDto> GetDtoById( CancellationToken cancellationToken = default, params object[] ids);
        Task<GreadData<TDto>> GetDtos(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<GreadData<TDto>> GetDtos(CancellationToken cancellationToken = default);

    }



}
