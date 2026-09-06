using GenericRepositories.ParentEntities;
using System.Linq.Expressions;

namespace GenericRepositories.Contracts.Generic
{
    public interface IRepositoryPublicAsyncDtoEFCore<TEntity, TDto> :
        IRepositoryPublicAsyncEFCore<TEntity> 
        where TEntity : class
        where TDto : class
    {
       
        Task AddDtoAsync(TDto dto, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateDtoAsync(TDto dto, CancellationToken cancellationToken, bool saveNow = true);
        Task<TDto> GetDtoById( CancellationToken cancellationToken, params object[] ids);
        Task<IEnumerable<TDto>> GetDtos(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);

    }



}
