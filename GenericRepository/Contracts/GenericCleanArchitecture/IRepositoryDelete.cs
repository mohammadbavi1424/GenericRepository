using System;
using System.Collections.Generic;
using System.Text;
using static Dapper.SqlMapper;

namespace GenericRepositories.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryDelete<TEntity> 
        where TEntity : class
    {
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteDtoAsync<TDtoDelete>(TDtoDelete dto, CancellationToken cancellationToken, bool saveNow = true);
        Task DeleteDtoRangeAsync<TDtoDelete>(IEnumerable<TDtoDelete> dtos, CancellationToken cancellationToken, bool saveNow = true);
        void Delete(TEntity entity, bool saveNow = true);
        void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void DeleteDto<TDtoDelete>(TDtoDelete dto, bool saveNow = true);
        void DeleteDtoRange<TDtoDelete>(IEnumerable<TDtoDelete> dtos, bool saveNow = true);
    }
}
