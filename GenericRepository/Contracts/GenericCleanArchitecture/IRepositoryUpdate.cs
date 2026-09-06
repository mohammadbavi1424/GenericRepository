using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepositories.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryUpdate<TEntity>
        where TEntity : class
    {
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateDtoAsync<TDtoUpdate>(TDtoUpdate dto, CancellationToken cancellationToken, bool saveNow = true);
        Task UpdateDtoRangeAsync<TDtoUpdate>(IEnumerable<TDtoUpdate> dtos, CancellationToken cancellationToken, bool saveNow = true);
        void Update(TEntity entity, bool saveNow = true);
        void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true);
        void UpdateDto<TDtoUpdate>(TDtoUpdate dto, bool saveNow = true);
        void UpdateDtoRange<TDtoUpdate>(IEnumerable<TDtoUpdate> dtos, bool saveNow = true);

    }
}
