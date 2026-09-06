using System.Linq.Expressions;

namespace GenericRepositories.Contracts.Generic
{
    public interface IRepositorySyncronize<TEntity> where TEntity : class
    {
        IQueryable<TEntity> TableDeleted { get; }
        IQueryable<TEntity> TableNoTrackingDeleted { get; }
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        void Add(TEntity entity, bool saveNow = true);
        void AddRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void Attach(TEntity entity);
        void Delete(TEntity entity, bool saveNow = true);
        //Task DeleteByIdAsync(CancellationToken cancellationToken, bool saveNow = true, params object[] ids);
        void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void Detach(TEntity entity);
        TEntity GetById(params object[] ids);
        TEntity GetByIdDeleted(params object[] ids);
        void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty) where TProperty : class;
        void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty) where TProperty : class;
        void Update(TEntity entity, bool saveNow = true);
        void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true);
        
    }
}
