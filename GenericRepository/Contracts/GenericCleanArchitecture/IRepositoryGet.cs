using GenericRepositories.Filters;
using System.Linq.Expressions;

namespace GenericRepositories.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryGet<TEntity> 
        where TEntity : class 
    {
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);
        Task<GreadData<TEntity>> GetListAsync(CancellationToken cancellationToken, GreadData<TEntity> data);
        Task<GreadData<TEntity>> GetByExpressionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
        Task<TDtoGetById> GetDtoByIdAsync<TDtoGetById>(CancellationToken cancellationToken, params object[] ids);
        Task<GreadData<TDtoGet>> GetDtosAsync<TDtoGet>(CancellationToken cancellationToken, GreadData<TDtoGet> data) where TDtoGet : class;
        Task<GreadData<TDtoGet>> GetDtosByExpressionAsync<TDtoGet>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TDtoGet : class;
        TEntity GetById(params object[] ids);
        GreadData<TEntity> GetList(GreadData<TEntity> data);
        GreadData<TEntity> GetDtosByExpression(Expression<Func<TEntity, bool>> predicate);

    }
}
