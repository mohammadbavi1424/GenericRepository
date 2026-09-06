using AutoMapper;
using GenericRepositories.Context;
using GenericRepositories.Contracts.GenericCleanArchitecture;
using GenericRepositories.Utilities;
using Microsoft.EntityFrameworkCore;
using static Dapper.SqlMapper;

namespace GenericRepositories.Repositories.GenericCleanArchitecture
{
    public class RepositoryAdd<TEntity> : 
        IRepositoryAdd<TEntity>
        where TEntity : class
    {

        public DbSet<TEntity> Entities { get; }


        private readonly GenericCommandDbContext DbCommandContext;
        private readonly IMapper mapper;

        public RepositoryAdd(GenericCommandDbContext dbCommandContext, IMapper mapper)
        {
            DbCommandContext = dbCommandContext;
            this.mapper = mapper;
            Entities = DbCommandContext.Set<TEntity>();
        }



        public virtual void Add(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            Entities.Add(entity);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void AddRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {

            Assert.NotNull(entities, nameof(entities));
            Entities.AddRange(entities);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public void AddDto<TDtoCreate>(TDtoCreate dto, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDtoCreate>(mapper);
            Add(entity, saveNow);
        }

        public void AddDtoRange<TDtoCreate>(IEnumerable<TDtoCreate> dtos, bool saveNow = true)
        {
            var entities = dtos.ToList().ConvertListObject<TEntity, TDtoCreate>(mapper);
            AddRange(entities, saveNow);
        }

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {

            Assert.NotNull(entity, nameof(entity));

            await Entities.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);


        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));

            await Entities.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        }

        public async Task AddDtoAsync<TDtoCreate>(TDtoCreate dto, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDtoCreate>(mapper);
            await AddAsync(entity, cancellationToken, saveNow);
        }

        public async Task AddDtoRangeAsync<TDtoCreate>(IEnumerable<TDtoCreate> dtos, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entities = dtos.ToList().ConvertListObject<TEntity, TDtoCreate>(mapper);
            await AddRangeAsync(entities, cancellationToken, saveNow);
        }

    }
}
