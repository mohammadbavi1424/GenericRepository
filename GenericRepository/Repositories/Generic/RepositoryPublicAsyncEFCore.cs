using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using GenericRepository.Filters;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;

namespace GenericRepository.Repositories.Generic
{
    public class RepositoryPublicAsyncEFCore<TEntity> :
        IRepositoryPublicAsyncEFCore<TEntity> where TEntity : class
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


        public RepositoryPublicAsyncEFCore(GenericCommandDbContext dbCommandContext,
            GenericQueryDbContext dbQueryContext)
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



        #region Async Method

        public virtual async Task<TEntity> GetByIdDeletedAsync(CancellationToken cancellationToken, params object[] ids)
        {
           
            return await TableNoTrackingDeleted
                .FirstOrDefaultAsync(p =>
                EF.Property<long>(p, GetIdProperty()) == (long)ids[0], cancellationToken);
        }

        public virtual async Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        => await TableNoTracking.FirstOrDefaultAsync(p =>
                EF.Property<long>(p, GetIdProperty()) == (long)ids[0], cancellationToken);
        

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));

            entity = SetAddProperty(entity);


            await EntitiesCommand.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            List<TEntity> entitiesChanged = new List<TEntity>();
            foreach (TEntity entity in entities.ToList())
                entitiesChanged.Add(SetAddProperty(entity));
            await EntitiesCommand.AddRangeAsync(entitiesChanged, cancellationToken).ConfigureAwait(false);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));

            var rowVersionValue = (byte[])entity.GetType().GetProperty("RowVersion").GetValue(entity);

            EntitiesCommand.Attach(entity);

            DbCommandContext.Entry(entity).Property("RowVersion").OriginalValue = rowVersionValue;

            SetUpdateProperty(entity);

            DbCommandContext.Entry(entity).State = EntityState.Modified;

            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));

            foreach (var entity in entities)
            {
                var rowVersionValue = (byte[])entity.GetType().GetProperty("RowVersion").GetValue(entity);

                EntitiesCommand.Attach(entity);

                DbCommandContext.Entry(entity).Property("RowVersion").OriginalValue = rowVersionValue;

                SetUpdateProperty(entity);

                DbCommandContext.Entry(entity).State = EntityState.Modified;
            }

            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            entity = SetDeleteProperty(entity);
            EntitiesCommand.Update(entity);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            List<TEntity> entitiesChanged = new List<TEntity>();
            foreach (TEntity entity in entities.ToList())
                entitiesChanged.Add(SetAddProperty(entity));
            EntitiesCommand.UpdateRange(entitiesChanged);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> referenceProperty, CancellationToken cancellationToken)
            where TProperty : class
        {
            Attach(entity);
            var reference = DbCommandContext.Entry(entity).Reference(referenceProperty);
            if (!reference.IsLoaded)
                await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty, CancellationToken cancellationToken)
            where TProperty : class
        {
            Attach(entity);

            var collection = DbCommandContext.Entry(entity).Collection(collectionProperty);
            if (!collection.IsLoaded)
                await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
        }


        public async Task<GreadData<TEntity>> GetListAsync(CancellationToken cancellationToken, GreadData<TEntity> data)
        {
            IQueryable<TEntity> query = TableNoTracking;

            foreach (var filter in data.Filter)
            {
                query = query.Where(c =>
                    EF.Property<object>(c, filter.Property).Equals(filter.Value));
            }

            data.Data = await query
                .Skip((data.Page - 1) * data.PageSize)
                .Take(data.PageSize)
                .ToListAsync(cancellationToken);
            data.PageCount = data.PageSize;
            data.Count = data.Data.Count();

            return data;

        }



        public Task<GreadData<TEntity>> GetDeletedAsync(CancellationToken cancellationToken, GreadData<TEntity> data)
        {
            throw new NotImplementedException();
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


        #region Transaction
        public async Task BeginTransactionAsync(
            CancellationToken cancellationToken)
        {
            _transaction = await DbCommandContext.Database
                .BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(
            CancellationToken cancellationToken)
        {
            if (_transaction is null)
                return;

            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync(
            CancellationToken cancellationToken)
        {
            if (_transaction is null)
                return;

            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
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
