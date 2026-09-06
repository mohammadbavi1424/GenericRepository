using AutoMapper;
using GenericRepositories.Context;
using GenericRepositories.Contracts.GenericCleanArchitecture;
using GenericRepositories.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepositories.Repositories.GenericCleanArchitecture
{
    public class RepositoryDelete<TEntity> :
        IRepositoryDelete<TEntity>
        where TEntity : class
    {

        public DbSet<TEntity> Entities { get; }

        private readonly GenericCommandDbContext DbCommandContext;
        private readonly IMapper mapper;

        public RepositoryDelete(GenericCommandDbContext dbCommandContext ,IMapper mapper )
        {
            DbCommandContext = dbCommandContext;
            this.mapper = mapper;
            Entities = DbCommandContext.Set<TEntity>();

        }

        public virtual void Delete(TEntity entity, bool saveNow = true)
        {
            Assert.NotNull(entity, nameof(entity));
            Entities.Remove(entity);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true)
        {
            Assert.NotNull(entities, nameof(entities));
            Entities.RemoveRange(entities);
            if (saveNow)
                DbCommandContext.SaveChanges();
        }

        public void DeleteDto<TDtoDelete>(TDtoDelete dto, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDtoDelete>(mapper);
            Delete(entity, saveNow);
        }

        public void DeleteDtoRange<TDtoDelete>(IEnumerable<TDtoDelete> dtos, bool saveNow = true)
        {
            var entities = dtos.ToList().ConvertListObject<TEntity,TDtoDelete>(mapper);
            DeleteRange(entities, saveNow);
        }
     
        public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
        {
                Assert.NotNull(entity, nameof(entity));
                Entities.Remove(entity);
                if (saveNow)
                    await DbCommandContext.SaveChangesAsync(cancellationToken);

            
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
        {
                Assert.NotNull(entities, nameof(entities));
                Entities.RemoveRange(entities);
                if (saveNow)
                    await DbCommandContext.SaveChangesAsync(cancellationToken);

          
        }

        public async Task DeleteDtoAsync<TDtoDelete>(TDtoDelete dto, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDtoDelete>(mapper);
            await DeleteAsync(entity, cancellationToken, saveNow);
        }

        public async Task DeleteDtoRangeAsync<TDtoDelete>(IEnumerable<TDtoDelete> dtos, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entities = dtos.ToList().ConvertListObject<TEntity, TDtoDelete>(mapper);
            await DeleteRangeAsync(entities, cancellationToken, saveNow);
        }

    }
}
