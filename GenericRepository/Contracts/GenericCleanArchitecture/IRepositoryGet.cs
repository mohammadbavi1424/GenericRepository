using GenericRepository.Filters;
using System.Linq.Expressions;

namespace GenericRepository.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryGet<TEntity> 
        where TEntity : class 
    {
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        Task<TEntity> GetByIdAsync(CancellationToken cancellationToken = default, params object[] ids);
        Task<GreadData<TEntity>> GetListAsync(GreadData<TEntity> data, CancellationToken cancellationToken = default);
        Task<GreadData<TEntity>> GetByExpressionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<TDtoGetById> GetDtoByIdAsync<TDtoGetById>(CancellationToken cancellationToken = default, params object[] ids);
        Task<GreadData<TDtoGet>> GetDtosAsync<TDtoGet>(GreadData<TDtoGet> data,CancellationToken cancellationToken = default) where TDtoGet : class;
        Task<GreadData<TDtoGet>> GetDtosByExpressionAsync<TDtoGet>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TDtoGet : class;
        TEntity GetById(params object[] ids);
        GreadData<TEntity> GetList(GreadData<TEntity> data);
        GreadData<TEntity> GetDtosByExpression(Expression<Func<TEntity, bool>> predicate);

    }
}
