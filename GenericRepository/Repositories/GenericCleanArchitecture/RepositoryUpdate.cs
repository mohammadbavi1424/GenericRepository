using AutoMapper;
using GenericRepository.Context;
using GenericRepository.Contracts.GenericCleanArchitecture;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Repositories.GenericCleanArchitecture
{
    public class RepositoryUpdate<TEntity> :
        IRepositoryUpdate<TEntity>
        where TEntity : class
    {
        public DbSet<TEntity> Entities { get; }

        private readonly GenericCommandDbContext DbCommandContext;
        private readonly IMapper mapper;

        public RepositoryUpdate(GenericCommandDbContext dbCommandContext, IMapper mapper)
        {
            DbCommandContext = dbCommandContext;
            this.mapper = mapper;
            Entities = DbCommandContext.Set<TEntity>();

        }

        public virtual void Update(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            Entities.Update(entity);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            Entities.UpdateRange(entities);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public void UpdateDto<TDtoUpdate>(TDtoUpdate dto, bool saveNow = true)
            => Update(dto.ConvertObject<TEntity, TDtoUpdate>(mapper), saveNow);
        

        public void UpdateDtoRange<TDtoUpdate>(IEnumerable<TDtoUpdate> dtos, bool saveNow = true)
        => UpdateRange(dtos.ToList().ConvertListObject<TEntity, TDtoUpdate>(mapper));
        


        public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));

            var rowVersionValue = (byte[])entity.GetType().GetProperty("RowVersion").GetValue(entity);

            Entities.Attach(entity);

            DbCommandContext.Entry(entity).Property("RowVersion").OriginalValue = rowVersionValue;

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

                Entities.Attach(entity);

                DbCommandContext.Entry(entity).Property("RowVersion").OriginalValue = rowVersionValue;

                DbCommandContext.Entry(entity).State = EntityState.Modified;
            }

            if (saveNow)
                await DbCommandContext.SaveChangesAsync(cancellationToken);

        }

        public async Task UpdateDtoAsync<TDtoUpdate>(TDtoUpdate dto, CancellationToken cancellationToken, bool saveNow = true)
        => await UpdateAsync(dto.ConvertObject<TEntity, TDtoUpdate>(mapper), cancellationToken, saveNow);
        

        public async Task UpdateDtoRangeAsync<TDtoUpdate>(IEnumerable<TDtoUpdate> dtos, CancellationToken cancellationToken, bool saveNow = true)
        => await UpdateRangeAsync(dtos.ToList().ConvertListObject<TEntity, TDtoUpdate>(mapper), cancellationToken, saveNow);
        

    }
}
