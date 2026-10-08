using Dapper;
using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using GenericRepository.Filters;
using GenericRepository.ParentEntities;
using GenericRepository.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GenericRepository.Repositories.Generic
{
    public class RepositoryPublicAsyncDapper<TEntity> :
        IRepositoryPublicAsyncDapper<TEntity>
        where TEntity : class, IBaseEntity
    {
        private readonly GenericCommandDbContext DbCommandContext;
        private readonly DbConnectionSetting setting;

        #region GetTableInfo

        private readonly string TableName =
            typeof(TEntity).Name;


        private string GetSchema()
        => DbCommandContext.Model.FindEntityType(typeof(TEntity)) != null ?
            DbCommandContext.Model.FindEntityType(typeof(TEntity))
            .GetSchema()
            ?? DbCommandContext.Model.GetDefaultSchema()
            ?? "dbo" : throw new InvalidOperationException(
             $"Entity {typeof(TEntity).Name} in Model EF Core Not Found.");

        private string GetIdProperty()
        => DbCommandContext.Model.FindEntityType(typeof(TEntity))?
                .FindPrimaryKey()?.Properties
                .FirstOrDefault()?.Name;



        #endregion


        private readonly HashSet<string> PropertyNames =
            typeof(TEntity).GetProperties()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);




        public RepositoryPublicAsyncDapper(GenericCommandDbContext dbCommandContext,
            DbConnectionSetting setting)
        {
            DbCommandContext = dbCommandContext;
            this.setting = setting;
        }


        #region Get By Id

        public async Task<TEntity?> GetByIdQueryAsync( params object[] ids)
        {
            var sql = $"""
                SELECT *
                FROM [{GetSchema()}].[{TableName}]
                WHERE IsDeleted = 0
                  AND {GetIdProperty()} = @Id
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, new { Id = ids[0] });
        }

        #endregion


        #region Get By Range Id

        public async Task<GreadData<TEntity>> GetByRangIdQuerAsync(params object[] ids)
        {
            var data = new GreadData<TEntity>();

            if (ids == null || ids.Count() == 0)
            {
                data.Data = Enumerable.Empty<TEntity>();
                return data;
            }

            var sql = $"""
                SELECT *
                FROM [{GetSchema()}].[{TableName}]
                WHERE IsDeleted = 0
                  AND {GetIdProperty()} IN @Ids
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            var result = await connection.QueryAsync<TEntity>(sql, new { Ids = ids });

            data.Data = result;

            return data;
        }

        #endregion


        #region Get Query

        public virtual async Task<GreadData<TEntity>> GetByQueryAsync(
            CancellationToken cancellationToken, GreadData<TEntity> data)
        => await GetPagedDataAsync(data, isDeleted: false, cancellationToken);
        

        #endregion


        #region Get Deleted Items

        public virtual async Task<GreadData<TEntity>> GetByQueryDeletedItemsAsync(
                CancellationToken cancellationToken, GreadData<TEntity> data)
        => await GetPagedDataAsync(data, isDeleted: true, cancellationToken);
        

        #endregion


        #region Get Deleted By Id

        public async Task<GreadData<TEntity>> GetByIdDeletedItemQueryAsync(params object[] ids)
        {
            var data = new GreadData<TEntity>();


            var sql = $"""
                SELECT *
                FROM [{GetSchema()}].[{TableName}]
                WHERE IsDeleted = 1
                  AND {GetIdProperty()} = @Id
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            data.Entity = await connection.QueryFirstOrDefaultAsync<TEntity>(
                sql, new { Id = ids[0] });

            return data;
        }

        #endregion


        #region Insert

        public async Task<bool> AddByDapperAsync(TEntity entity)
        {
            var properties = typeof(TEntity)
                .GetProperties()
                .Where(p => p.Name != GetIdProperty())
                .ToList();

            var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));

            var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var sql = $"""
                INSERT INTO [{GetSchema()}].[{TableName}]
                ({columns})
                VALUES
                ({parameters})
                """;

            using var connection = new SqlConnection(setting.CommandConnectionString);

            var affectedRows = await connection.ExecuteAsync(sql, entity);

            return affectedRows > 0;
        }

        #endregion


        #region Update

        public async Task<bool> UpdateByDapperAsync(TEntity entity)
        {
            var properties = typeof(TEntity)
                .GetProperties()
                .Where(p => p.Name != GetIdProperty())
                .ToList();

            var setClause = string.Join(", ", properties.Select(p =>
                    $"[{p.Name}] = @{p.Name}"));

            var sql = $"""
                UPDATE [{GetSchema()}].[{TableName}]
                SET {setClause}
                WHERE {GetIdProperty()} = @Id
                """;

            using var connection = new SqlConnection(setting.CommandConnectionString);

            var affectedRows = await connection.ExecuteAsync(sql, entity);

            return affectedRows > 0;
        }

        #endregion


        #region Soft Delete

        public async Task<bool> DeleteAsync(params object[] ids)
        {

            var sql = $"""
                UPDATE [{GetSchema()}].[{TableName}]
                SET IsDeleted = 1
                WHERE {GetIdProperty()} = @Id
                  AND IsDeleted = 0
                """;

            using var connection = new SqlConnection(setting.CommandConnectionString);

            var affectedRows = await connection.ExecuteAsync(sql, new { Id = ids[0] });

            return affectedRows > 0;
        }

        #endregion


        #region Restore

        public async Task<bool> RestoreAsync(params object[] ids)
        {
            var sql = $"""
                UPDATE [{GetSchema()}].[{TableName}]
                SET IsDeleted = 0
                WHERE {GetIdProperty()} = @Id
                  AND IsDeleted = 1
                """;

            using var connection = new SqlConnection(setting.CommandConnectionString);

            var affectedRows = await connection.ExecuteAsync(sql, new { Id = ids[0] });

            return affectedRows > 0;
        }

        #endregion


        #region Exists

        public async Task<bool> ExistsAsync(params object[] ids)
        {
            var sql = $"""
                SELECT CAST(
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM [{GetSchema()}].[{TableName}]
                            WHERE {GetIdProperty()} = @Id
                              AND IsDeleted = 0
                        )
                        THEN 1
                        ELSE 0
                    END
                AS BIT)
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Id = ids[0] });
        }

        #endregion


        #region Count

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var sql = $"""
                SELECT COUNT(1)
                FROM [{GetSchema()}].[{TableName}]
                WHERE IsDeleted = 0
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            var command = new CommandDefinition(sql, cancellationToken: cancellationToken);

            return await connection.ExecuteScalarAsync<int>(command);
        }

        #endregion


        #region Private Pagination

        private async Task<GreadData<TEntity>> GetPagedDataAsync(GreadData<TEntity> data,
            bool isDeleted, CancellationToken cancellationToken)
        {
            if (data.Page <= 0)
                data.Page = 1;

            if (data.PageSize <= 10)
                data.PageSize = 10;

            var parameters = new DynamicParameters();

            var sql = $"""
                FROM [{GetSchema()}].[{TableName}]
                WHERE IsDeleted = @IsDeleted
                """;

            parameters.Add(
                "@IsDeleted",
                isDeleted,
                DbType.Boolean);


            if (data.Filter != null)
            {
                var filterIndex = 0;

                foreach (var filter in data.Filter)
                {
                    if (!PropertyNames.Contains(filter.Property))
                    {
                        throw new ArgumentException(
                            $"Invalid filter property: {filter.Property}");
                    }

                    var parameterName =
                        $"FilterValue{filterIndex}";

                    sql +=
                        $" AND [{filter.Property}] LIKE @{parameterName}";

                    parameters.Add(
                        parameterName,
                        $"%{filter.Value}%");

                    filterIndex++;
                }
            }

            var countSql = $"""
                SELECT COUNT(1)
                {sql};
                """;

            var offset =
                (data.Page - 1) * data.PageSize;

            parameters.Add("Offset", offset);
            parameters.Add("PageSize", data.PageSize);

            var dataSql = $"""
                SELECT *
                {sql}
                ORDER BY {GetIdProperty()}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
                """;

            using var connection = new SqlConnection(setting.QueryConnectionString);

            await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition($"{countSql}\n{dataSql}",
                parameters, cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            data.Count = await multi.ReadFirstAsync<int>();

            data.Data = (await multi.ReadAsync<TEntity>()).ToList();

            data.PageCount = data.Count == 0 ? 0 : (int)Math.Ceiling(
                        (double)data.Count / data.PageSize);

            return data;
        }

        #endregion



    }
}

