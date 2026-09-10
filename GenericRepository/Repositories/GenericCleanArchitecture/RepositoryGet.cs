using AutoMapper;
using GenericRepository.Context;
using GenericRepository.Contracts.GenericCleanArchitecture;
using GenericRepository.Filters;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenericRepository.Repositories.GenericCleanArchitecture
{
    public class RepositoryGet<TEntity> :
        IRepositoryGet<TEntity>
        where TEntity : class
    {

        public DbSet<TEntity> Entities { get; }
        private readonly GenericQueryDbContext DbQueryContext;
        private readonly IMapper mapper;

        public virtual IQueryable<TEntity> Table =>
            Entities.Where(p => EF.Property<bool?>(p, "IsDeleted") != true);
        public virtual IQueryable<TEntity> TableNoTracking =>
            Entities.Where(p => EF.Property<bool?>(p, "IsDeleted") != true).AsNoTracking();

        private string GetIdProperty()
        =>  DbQueryContext.Model.FindEntityType(typeof(TEntity))
                .FindPrimaryKey()?
                .Properties.FirstOrDefault()?.Name;


        public RepositoryGet(GenericQueryDbContext dbQueryContext, IMapper mapper)
        {
            DbQueryContext = dbQueryContext;
            this.mapper = mapper;
            Entities = DbQueryContext.Set<TEntity>();
        }




        public virtual TEntity GetById(params object[] ids)
        => TableNoTracking
                .FirstOrDefault(p =>
                EF.Property<object>(p, GetIdProperty()) == (ids[0]));

        


        public virtual async Task<GreadData<TEntity>> GetByExpressionAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken)
        {
            IQueryable<TEntity> query = TableNoTracking.Where(predicate);

            GreadData<TEntity> data = new();

            data.Data = await query
                .ToListAsync(cancellationToken);
            data.Count = data.Data.Count();

            return data;
        }


        public async virtual Task<TEntity> GetByIdAsync(CancellationToken cancellationToken,
            params object[] ids)
        => await TableNoTracking.FirstOrDefaultAsync(c =>
            EF.Property<object>(c, GetIdProperty()) == ids[0]);


        public virtual async Task<TDtoGetById> GetDtoByIdAsync<TDtoGetById>(CancellationToken cancellationToken,
            params object[] ids)
        => (await TableNoTracking.FirstOrDefaultAsync(c =>
            EF.Property<object>(c, GetIdProperty()) == ids[0]))
                .ConvertObject<TDtoGetById, TEntity>(mapper);
        

        public async virtual Task<GreadData<TDtoGet>> GetDtosAsync<TDtoGet>(CancellationToken cancellationToken,
            GreadData<TDtoGet> data)
            where TDtoGet : class
        {
            IQueryable<TEntity> query = TableNoTracking;

            foreach (var filter in data.Filter)
            {
                query = query.Where(c =>
                    EF.Property<object>(c, filter.Property).Equals(filter.Value));
            }

            data.Data = (await query
                .Skip((data.Page - 1) * data.PageSize)
                .Take(data.PageSize)
                .ToListAsync(cancellationToken)).ConvertListObject<TDtoGet, TEntity>(mapper);
            data.PageCount = data.PageSize;
            data.Count = data.Data.Count();

            return data;
        }

        public virtual GreadData<TEntity> GetDtosByExpression(Expression<Func<TEntity, bool>> predicate)
        {
            IQueryable<TEntity> query = TableNoTracking.Where(predicate);

            GreadData<TEntity> data = new();

            data.Data = query
                .ToList();
            data.Count = data.Data.Count();

            return data;
        }

        public async virtual Task<GreadData<TDtoGet>> GetDtosByExpressionAsync<TDtoGet>
            (Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
            where TDtoGet : class
        {
            IQueryable<TEntity> query = TableNoTracking.Where(predicate);
            GreadData<TDtoGet> data = new();


            data.Data = (await query
                .ToListAsync(cancellationToken))
                .ConvertListObject<TDtoGet, TEntity>(mapper);
            data.Count = data.Data.Count();

            return data;
        }

        public virtual GreadData<TEntity> GetList(GreadData<TEntity> data)
        {
            IQueryable<TEntity> query = TableNoTracking;

            foreach (var filter in data.Filter)
            {
                query = query.Where(c =>
                    EF.Property<object>(c, filter.Property).Equals(filter.Value));
            }

            data.Data = query
                .Skip((data.Page - 1) * data.PageSize)
                .Take(data.PageSize)
                .ToList();
            data.PageCount = data.PageSize;
            data.Count = data.Data.Count();

            return data;
        }

        public async virtual Task<GreadData<TEntity>> GetListAsync(CancellationToken cancellationToken,
            GreadData<TEntity> data)
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
    }
}
