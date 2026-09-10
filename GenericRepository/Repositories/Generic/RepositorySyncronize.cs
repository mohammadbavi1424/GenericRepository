using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using GenericRepository.ParentEntities;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace GenericRepository.Repositories.Generic
{
    public class RepositorySyncronize<TEntity> : 
        IRepositorySyncronize<TEntity> where TEntity : class, 
        IBaseEntity
    {
        private readonly GenericCommandDbContext DbCommandContext;
        private readonly GenericQueryDbContext DbQueryContext;
        private IDbContextTransaction? _transaction;


        public DbSet<TEntity> EntitiesCommand { get; }
        public DbSet<TEntity> EntitiesQuery { get; }
        public virtual IQueryable<TEntity> TableDeleted =>
            EntitiesQuery.Where(p => EF.Property<bool?>(p, "IsDeleted") == true);
        public virtual IQueryable<TEntity> TableNoTrackingDeleted =>
            EntitiesQuery.Where(p => EF.Property<bool?>(p, "IsDeleted") == true).AsNoTracking();
        public virtual IQueryable<TEntity> Table =>
            EntitiesQuery.Where(p => EF.Property<bool?>(p, "IsDeleted") != true);
        public virtual IQueryable<TEntity> TableNoTracking =>
            EntitiesQuery.Where(p => EF.Property<bool?>(p, "IsDeleted") != true).AsNoTracking();

        public RepositorySyncronize(GenericCommandDbContext dbCommandContext,
            GenericQueryDbContext dbQueryContext,
            IConfiguration configuration)
        {
            DbCommandContext = dbCommandContext;
            DbQueryContext = dbQueryContext;
            EntitiesCommand = DbCommandContext.Set<TEntity>(); 
            EntitiesQuery = DbQueryContext.Set<TEntity>();
        }


        private string GetIdProperty()
        => DbCommandContext.Model.FindEntityType(typeof(TEntity))?
                .FindPrimaryKey()?.Properties
                .FirstOrDefault()?.Name;



        #region Sync Methods
        public TEntity GetByIdDeleted(params object[] ids)
        =>  TableNoTrackingDeleted
                .FirstOrDefault(p =>
                EF.Property<long>(p, GetIdProperty()) == (long)ids[0]);
        

        public virtual TEntity GetById(params object[] ids)
        => TableNoTracking
                .FirstOrDefault(p =>
                EF.Property<long>(p, GetIdProperty()) == (long)ids[0]);
        

        public virtual void Add(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            entity = SetAddProperty(entity);
            EntitiesCommand.Add(entity);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void AddRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            List<TEntity> entitiesChanged = new List<TEntity>();
            foreach (TEntity entity in entities.ToList())
                entitiesChanged.Add(SetAddProperty(entity));
            EntitiesCommand.AddRange(entitiesChanged);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void Update(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            entity = SetUpdateProperty(entity);
            EntitiesCommand.Update(entity);
            DbCommandContext.SaveChanges();
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            List<TEntity> entitiesChanged = new List<TEntity>();
            foreach (TEntity entity in entities.ToList())
                entitiesChanged.Add(SetAddProperty(entity));
            EntitiesCommand.UpdateRange(entitiesChanged);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void Delete(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            entity = SetDeleteProperty(entity);
            EntitiesCommand.Update(entity);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            List<TEntity> entitiesChanged = new List<TEntity>();
            foreach (TEntity entity in entities.ToList())
                entitiesChanged.Add(SetAddProperty(entity));
            EntitiesCommand.UpdateRange(entitiesChanged);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }
        #endregion

        #region Attach & Detach
        public virtual void Detach(TEntity entity)
        {
            Assert.NotNull(entity, nameof(entity));
            var entry = DbCommandContext.Entry(entity);
            if (entry != null)
                entry.State = EntityState.Detached;
        }

        public virtual void Attach(TEntity entity)
        {
            Assert.NotNull(entity, nameof(entity));
            if (DbCommandContext.Entry(entity).State == EntityState.Detached)
                EntitiesCommand.Attach(entity);
        }
        #endregion

        #region Explicit Loading

        public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty)
            where TProperty : class
        {
            Attach(entity);
            var collection = DbCommandContext.Entry(entity).Collection(collectionProperty);
            if (!collection.IsLoaded)
                collection.Load();
        }


        public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty)
            where TProperty : class
        {
            Attach(entity);
            var reference = DbCommandContext.Entry(entity).Reference(referenceProperty);
            if (!reference.IsLoaded)
                reference.Load();
        }
        #endregion


        #region Set Subscriber Properties

        private TEntity SetAddProperty(TEntity entity)
        {
            var createAtProp = entity.GetType().GetProperty("CreateAt");
            if (createAtProp != null && createAtProp.CanWrite)
                createAtProp.SetValue(entity, DateTime.UtcNow);

            // Set CreateByUserId
            var createByProp = entity.GetType().GetProperty("CreateByUserId");
            if (createByProp != null && createByProp.CanWrite)
            {
                // اگر کاربر لاگین شده باشد
                long? userId = 0;// _userContextService?.UserId; // هرجایی که UserId را می‌گیری
                createByProp.SetValue(entity, userId);
            }

            return entity;
        }

        private TEntity SetUpdateProperty(TEntity entity)
        {
            var updateAt = entity.GetType().GetProperty("UpdateAt");
            if (updateAt != null && updateAt.CanWrite)
                updateAt.SetValue(entity, DateTime.UtcNow);

            var updateUserId = entity.GetType().GetProperty("UpdateByUserId");
            if (updateUserId != null && updateUserId.CanWrite)
            {
                long? userId = 0;// _userContextService?.UserId; // هرجایی که UserId را می‌گیری
                updateUserId.SetValue(entity, userId);
            }

            //var updateRowVersion = entity.GetType().GetProperty("RowVersion");
            //if (updateRowVersion != null && updateRowVersion.CanWrite)
            //{
            //    byte[] RowVersion = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            //    updateRowVersion.SetValue(entity, RowVersion);
            //}
            return entity;
        }

        private TEntity SetDeleteProperty(TEntity entity)
        {
            var deletedAt = entity.GetType().GetProperty("DeletedAt");
            if (deletedAt != null && deletedAt.CanWrite)
                deletedAt.SetValue(entity, DateTime.UtcNow);


            var isDeleted = entity.GetType().GetProperty("IsDeleted");
            if (isDeleted != null && isDeleted.CanWrite)
                isDeleted.SetValue(entity, true);


            var deletedByUserId = entity.GetType().GetProperty("DeletedByUserId");
            if (deletedByUserId != null && deletedByUserId.CanWrite)
            {
                long? userId = 0;// _userContextService?.UserId; // هرجایی که UserId را می‌گیری
                deletedByUserId.SetValue(entity, userId);
            }
            return entity;
        }

        #endregion

        
    }

}
