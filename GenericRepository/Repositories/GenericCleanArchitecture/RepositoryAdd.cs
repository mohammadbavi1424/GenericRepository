using AutoMapper;
using GenericRepository.Context;
using GenericRepository.Contracts.GenericCleanArchitecture;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Repositories.GenericCleanArchitecture
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
            => Add(dto.ConvertObject<TEntity, TDtoCreate>(mapper), saveNow);
        

        public void AddDtoRange<TDtoCreate>(IEnumerable<TDtoCreate> dtos, bool saveNow = true)
            => AddRange(dtos.ToList().ConvertListObject<TEntity, TDtoCreate>(mapper), saveNow);
        

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
            => await AddAsync(dto.ConvertObject<TEntity, TDtoCreate>(mapper), cancellationToken, saveNow);
        

        public async Task AddDtoRangeAsync<TDtoCreate>(IEnumerable<TDtoCreate> dtos, CancellationToken cancellationToken, bool saveNow = true)
            => await AddRangeAsync(dtos.ToList().ConvertListObject<TEntity, TDtoCreate>(mapper), cancellationToken, saveNow);
        

    }
}
