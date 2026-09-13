using AutoMapper;
using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using GenericRepository.Filters;
using GenericRepository.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenericRepository.Repositories.Generic
{
    public class RepositoryPublicAsyncDtoEFCore<TEntity, TDto> :
        RepositoryPublicAsyncEFCore<TEntity>,
        IRepositoryPublicAsyncDtoEFCore<TEntity, TDto>
        where TEntity : class
        where TDto : class
        
    {
        private readonly GenericCommandDbContext DbCommandContext;
        private readonly IMapper mapper;

        public RepositoryPublicAsyncDtoEFCore(GenericCommandDbContext dbCommandContext,
            GenericQueryDbContext dbQueryContext,IMapper mapper) : base(dbCommandContext,dbQueryContext)
        {
            DbCommandContext = dbCommandContext;
            this.mapper = mapper;
        }


        private string GetIdProperty()
        => DbCommandContext.Model.FindEntityType(typeof(TEntity))?
                .FindPrimaryKey()?.Properties
                .FirstOrDefault()?.Name;




        public virtual async Task<GreadData<TDto>> GetDtos(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken)
        {
            var list = await TableNoTracking.Where(predicate)
                .ToListAsync(cancellationToken);

            var dtoList = list.ConvertListObject<TDto, TEntity>(mapper);
            GreadData<TDto> data = new()
            {
                Data = dtoList,
                Count = dtoList.Count,
            };
            return data;
        }

        public virtual async Task AddDtoAsync(TDto dto, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDto>(mapper);
            await base.AddAsync(entity, cancellationToken, saveNow);
        }


        public virtual async Task UpdateDtoAsync(TDto dto, CancellationToken cancellationToken, bool saveNow = true)
        {
            var entity = dto.ConvertObject<TEntity, TDto>(mapper);
            await base.UpdateAsync(entity, cancellationToken, saveNow);
        }

        public virtual async Task<TDto> GetDtoById(CancellationToken cancellationToken, params object[] ids)
        {
            var Item = await TableNoTracking.FirstOrDefaultAsync(e =>
            EF.Property<object>(e, GetIdProperty()).Equals(ids[0]), cancellationToken);
            var dto = Item.ConvertObject<TDto, TEntity>(mapper);
            return dto;
        }

    }


}
