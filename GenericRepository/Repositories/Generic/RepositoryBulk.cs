using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;
using System.Data.Common;

namespace GenericRepository.Repositories.Generic
{
    public class RepositoryBulk<TEntity> : IRepositoryBulk<TEntity>
        where TEntity : class
    {
        private readonly GenericCommandDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public RepositoryBulk(GenericCommandDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// BULK INSERT
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="batchSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddBulkAsync(IEnumerable<TEntity> entities,
            int batchSize = 10_000, CancellationToken cancellationToken = default)
        {
            var entityList = entities?.ToList();

            if (entityList == null || entityList.Count == 0)
                return;

            var metadata = GetEntityMetadata();

            await using var connection = new SqlConnection(
                _context.Database.GetConnectionString());

            await connection.OpenAsync(cancellationToken);

            for (int i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var table = CreateDataTable(
                    batch,
                    metadata.Properties);

                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName =
                        BuildTableName(metadata),

                    BatchSize = batchSize,

                    BulkCopyTimeout = 0
                };

                foreach (var property in metadata.Properties)
                {
                    bulkCopy.ColumnMappings.Add(
                        property.Name,
                        property.GetColumnName());
                }

                await bulkCopy.WriteToServerAsync(
                    table,
                    cancellationToken);
            }
        }


        /// <summary>
        /// BULK UPDATE
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="batchSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task UpdateBulkAsync(IEnumerable<TEntity> entities,
            int batchSize = 10_000, CancellationToken cancellationToken = default)
        {
            var entityList = entities?.ToList();

            if (entityList == null || entityList.Count == 0)
                return;

            var metadata = GetEntityMetadata();

            var keyProperty = metadata.PrimaryKey;

            var updateProperties = metadata.Properties
                .Where(x =>
                    !x.IsPrimaryKey() &&
                    !IsRowVersion(x))
                .ToList();

            await using var connection = new SqlConnection(
                _context.Database.GetConnectionString());

            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                for (int i = 0;
                     i < entityList.Count;
                     i += batchSize)
                {
                    var batch = entityList
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();

                    var tempTableName =
                        $"#BulkUpdate_{Guid.NewGuid():N}";

                    await CreateTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        metadata);

                    var table = CreateDataTable(
                        batch,
                        metadata.Properties);

                    using var bulkCopy = new SqlBulkCopy(
                        connection,
                        SqlBulkCopyOptions.Default,
                        (SqlTransaction)transaction);

                    bulkCopy.DestinationTableName =
                        tempTableName;

                    bulkCopy.BatchSize = batchSize;

                    bulkCopy.BulkCopyTimeout = 0;

                    foreach (var property in metadata.Properties)
                    {
                        bulkCopy.ColumnMappings.Add(
                            property.Name,
                            property.Name);
                    }

                    await bulkCopy.WriteToServerAsync(
                        table,
                        cancellationToken);

                    var updateSql = BuildUpdateSql(
                        metadata,
                        tempTableName,
                        updateProperties);

                    await ExecuteSqlAsync(
                        connection,
                        transaction,
                        updateSql,
                        cancellationToken);

                    await DropTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        cancellationToken);
                }

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }


        /// <summary>
        /// BULK DELETE
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="batchSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task DeleteBulkAsync(IEnumerable<TEntity> entities,
            int batchSize = 10_000, CancellationToken cancellationToken = default)
        {
            var entityList = entities?.ToList();

            if (entityList == null || entityList.Count == 0)
                return;

            var metadata = GetEntityMetadata();

            var keyProperty = metadata.PrimaryKey;

            await using var connection = new SqlConnection(
                _context.Database.GetConnectionString());

            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                for (int i = 0;
                     i < entityList.Count;
                     i += batchSize)
                {
                    var batch = entityList
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();

                    var tempTableName =
                        $"#BulkDelete_{Guid.NewGuid():N}";

                    await CreateKeyTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        keyProperty);

                    var table = CreateDataTable(
                        batch,
                        new[] { keyProperty });

                    using var bulkCopy = new SqlBulkCopy(
                        connection,
                        SqlBulkCopyOptions.Default,
                        (SqlTransaction)transaction);

                    bulkCopy.DestinationTableName =
                        tempTableName;

                    bulkCopy.BatchSize = batchSize;

                    bulkCopy.BulkCopyTimeout = 0;

                    bulkCopy.ColumnMappings.Add(
                        keyProperty.Name,
                        keyProperty.Name);

                    await bulkCopy.WriteToServerAsync(
                        table,
                        cancellationToken);

                    var sql = $"""
                        DELETE T
                        FROM {BuildTableName(metadata)} AS T
                        INNER JOIN {tempTableName} AS B
                            ON T.[{keyProperty.GetColumnName()}]
                             = B.[{keyProperty.GetColumnName()}];
                        """;

                    await ExecuteSqlAsync(
                        connection,
                        transaction,
                        sql,
                        cancellationToken);

                    await DropTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        cancellationToken);
                }

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }


        /// <summary>
        /// BULK SOFT DELETE
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="batchSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        public async Task SoftDeleteBulkAsync(IEnumerable<TEntity> entities,
            int batchSize = 10_000, CancellationToken cancellationToken = default)
        {
            var entityList = entities?.ToList();

            if (entityList == null || entityList.Count == 0)
                return;

            var metadata = GetEntityMetadata();

            var isDeletedProperty =
                metadata.Properties.FirstOrDefault(
                    x => x.Name == "IsDeleted");

            if (isDeletedProperty == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TEntity).Name} does not contain IsDeleted property.");
            }

            var deletedDateProperty =
                metadata.Properties.FirstOrDefault(
                    x => x.Name == "DeletedDate");

            var deletedUserIdProperty =
                metadata.Properties.FirstOrDefault(
                    x => x.Name == "DeletedUserId");

            var keyProperty = metadata.PrimaryKey;

            await using var connection = new SqlConnection(
                _context.Database.GetConnectionString());

            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                for (int i = 0;
                     i < entityList.Count;
                     i += batchSize)
                {
                    var batch = entityList
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();

                    var tempTableName =
                        $"#BulkSoftDelete_{Guid.NewGuid():N}";

                    await CreateKeyTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        keyProperty);

                    var table = CreateDataTable(
                        batch,
                        new[] { keyProperty });

                    using var bulkCopy = new SqlBulkCopy(
                        connection,
                        SqlBulkCopyOptions.Default,
                        (SqlTransaction)transaction);

                    bulkCopy.DestinationTableName =
                        tempTableName;

                    bulkCopy.BatchSize = batchSize;

                    bulkCopy.BulkCopyTimeout = 0;

                    bulkCopy.ColumnMappings.Add(
                        keyProperty.Name,
                        keyProperty.Name);

                    await bulkCopy.WriteToServerAsync(
                        table,
                        cancellationToken);

                    var setClauses = new List<string>
                    {
                        $"T.[{isDeletedProperty.GetColumnName()}] = 1"
                    };

                    if (deletedDateProperty != null)
                    {
                        setClauses.Add(
                            $"T.[{deletedDateProperty.GetColumnName()}] = SYSUTCDATETIME()");
                    }

                    if (deletedUserIdProperty != null)
                    {
                        setClauses.Add(
                            $"T.[{deletedUserIdProperty.GetColumnName()}] = NULL");
                    }

                    var sql = $"""
                        UPDATE T
                        SET {string.Join(", ", setClauses)}
                        FROM {BuildTableName(metadata)} AS T
                        INNER JOIN {tempTableName} AS B
                            ON T.[{keyProperty.GetColumnName()}]
                             = B.[{keyProperty.GetColumnName()}];
                        """;

                    await ExecuteSqlAsync(
                        connection,
                        transaction,
                        sql,
                        cancellationToken);

                    await DropTempTableAsync(
                        connection,
                        transaction,
                        tempTableName,
                        cancellationToken);
                }

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }


        /// <summary>
        /// METADATA
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        private EntityMetadata GetEntityMetadata()
        {
            var entityType = _context.Model
                .FindEntityType(typeof(TEntity));

            if (entityType == null)
            {
                throw new InvalidOperationException(
                    $"Entity {typeof(TEntity).Name} was not found in EF Core model.");
            }

            var primaryKey =
                entityType.FindPrimaryKey()?
                    .Properties
                    .FirstOrDefault();

            if (primaryKey == null)
            {
                throw new InvalidOperationException(
                    $"Entity {typeof(TEntity).Name} does not have a primary key.");
            }

            return new EntityMetadata
            {
                EntityType = entityType,
                PrimaryKey = primaryKey,
                Properties = entityType
                    .GetProperties()
                    .Where(x =>
                        x.PropertyInfo != null &&
                        !x.IsShadowProperty())
                    .ToList()
            };
        }


        /// <summary>
        /// DATATABLE
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="properties"></param>
        /// <returns></returns>

        private DataTable CreateDataTable(IEnumerable<TEntity> entities,
            IEnumerable<IProperty> properties)
        {
            var table = new DataTable();

            var propertyList = properties.ToList();

            foreach (var property in propertyList)
            {
                table.Columns.Add(
                    property.Name,
                    Nullable.GetUnderlyingType(
                        property.ClrType)
                    ?? property.ClrType);
            }

            foreach (var entity in entities)
            {
                var row = table.NewRow();

                foreach (var property in propertyList)
                {
                    var value =
                        property.PropertyInfo!
                            .GetValue(entity);

                    row[property.Name] =
                        value ?? DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table;
        }


        /// <summary>
        /// TEMP TABLE
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tempTableName"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>

        private async Task CreateTempTableAsync(SqlConnection connection,
            DbTransaction transaction, string tempTableName, EntityMetadata metadata)
        {
            var columns = metadata.Properties
                .Select(x =>
                    $"[{x.Name}] {GetSqlType(x)}");

            var sql = $"""
                CREATE TABLE {tempTableName}
                (
                    {string.Join(", ", columns)}
                );
                """;

            await ExecuteSqlAsync(
                connection,
                transaction,
                sql,
                CancellationToken.None);
        }


        private async Task CreateKeyTempTableAsync(SqlConnection connection,
            DbTransaction transaction, string tempTableName, IProperty keyProperty)
        => await ExecuteSqlAsync(
                connection,
                transaction,
                 $"""
                CREATE TABLE {tempTableName}
                (
                    [{keyProperty.Name}] {GetSqlType(keyProperty)}
                );
                """,
                CancellationToken.None);



        /// <summary>
        /// UPDATE SQL
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="tempTableName"></param>
        /// <param name="updateProperties"></param>
        /// <returns></returns>

        private string BuildUpdateSql(EntityMetadata metadata, string tempTableName,
            List<IProperty> updateProperties)
        {
            var tableName = BuildTableName(metadata);

            var keyColumn =
                metadata.PrimaryKey.GetColumnName();

            var setClauses = updateProperties
                .Select(x =>
                    $"T.[{x.GetColumnName()}] = " +
                    $"B.[{x.GetColumnName()}]");

            return $"""
                UPDATE T
                SET
                    {string.Join(",\n", setClauses)}
                FROM {tableName} AS T
                INNER JOIN {tempTableName} AS B
                    ON T.[{keyColumn}]
                     = B.[{keyColumn}];
                """;
        }


        /// <summary>
        /// SQL EXECUTION
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="sql"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        private async Task ExecuteSqlAsync(SqlConnection connection,
            DbTransaction transaction,string sql, CancellationToken cancellationToken)
        {
            await using var command =
                new SqlCommand(
                    sql,
                    connection,
                    (SqlTransaction)transaction);

            command.CommandTimeout = 0;

            await command.ExecuteNonQueryAsync(
                cancellationToken);
        }


        private async Task DropTempTableAsync(SqlConnection connection,
            DbTransaction transaction, string tempTableName, CancellationToken cancellationToken)
        => await ExecuteSqlAsync(
                connection,
                transaction,
                $"DROP TABLE IF EXISTS {tempTableName};",
                cancellationToken);



        /// <summary>
        /// TABLE NAME
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        private string BuildTableName(EntityMetadata metadata)
        {
            var schema =
                metadata.EntityType.GetSchema()
                ?? "dbo";

            var table =
                metadata.EntityType.GetTableName();

            if (string.IsNullOrWhiteSpace(table))
            {
                throw new InvalidOperationException(
                    $"Table name for {typeof(TEntity).Name} was not found.");
            }

            return $"[{schema}].[{table}]";
        }


        /// <summary>
        /// SQL TYPE
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        private string GetSqlType(IProperty property)
        {
            var storeType =
                property.GetColumnType();

            if (!string.IsNullOrWhiteSpace(storeType))
                return storeType;

            throw new InvalidOperationException(
                $"SQL type for property '{property.Name}' " +
                $"of entity '{typeof(TEntity).Name}' " +
                "could not be determined.");
        }


        /// <summary>
        /// ROW VERSION
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>

        private bool IsRowVersion(IProperty property)
        => property.IsConcurrencyToken &&
                   property.ValueGenerated ==
                   Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;


        private sealed class EntityMetadata
        {
            public IEntityType EntityType { get; init; } = null!;

            public IProperty PrimaryKey { get; init; } = null!;

            public List<IProperty> Properties { get; init; } = null!;
        }
    }
}