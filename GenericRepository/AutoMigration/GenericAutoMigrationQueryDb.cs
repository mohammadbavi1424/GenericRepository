using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using GenericRepository.Models.AutoMigration;
using System.Collections.Generic;
using System.Text.Json;
using GenericRepository.Context;

//using Newtonsoft.Json;

namespace GenericRepository.AutoMigration
{
    public sealed class GenericAutoMigrationQueryDb
    {
        private const string SnapshotSchema = "dbo";
        private const string SnapshotTable = "__GenericRepositorySchemaSnapshot";
        private readonly string DbName;


        private readonly GenericQueryDbContext _dbContext;
        private readonly IMigrationsSqlGenerator _sqlGenerator;

        public GenericAutoMigrationQueryDb(
             GenericQueryDbContext dbContext)
        {
            _dbContext = dbContext;

            _sqlGenerator = dbContext
                .GetInfrastructure()
                .GetRequiredService<IMigrationsSqlGenerator>();

            DbName = _dbContext.Database.GetDbConnection().Database;

        }

        public async Task SynchronizeAsync(
    CancellationToken cancellationToken = default)
        {
            Console.WriteLine("===== AUTO MIGRATION START =====");

            try
            {
                Console.WriteLine("STEP 1 - Before SyncAsync");

                await SyncAsync(cancellationToken);

                Console.WriteLine("STEP 2 - After SyncAsync");
            }
            catch (Exception ex)
            {
                Console.WriteLine("===== AUTO MIGRATION ERROR =====");
                Console.WriteLine(ex.ToString());

                throw;
            }

            Console.WriteLine("===== AUTO MIGRATION END =====");
        }

        public async Task SyncAsync(
            CancellationToken cancellationToken = default)
        {
            //await _dbContext.Database.EnsureCreatedAsync(
            //    cancellationToken);

            if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                await _dbContext.Database.EnsureCreatedAsync(
                    cancellationToken);
            }


            var currentSnapshot = CreateSnapshot();

            var previousSnapshot =
                await LoadSnapshotAsync(cancellationToken);

            if (previousSnapshot == null)
            {
                await SaveSnapshotAsync(
                    currentSnapshot,
                    cancellationToken);

                return;
            }

            var operations = BuildOperations(
                previousSnapshot,
                currentSnapshot);

            if (operations.Count == 0)
                return;

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                await AcquireApplicationLockAsync(
                    cancellationToken);

                var commands = _sqlGenerator.Generate(
                    operations,
                    _dbContext.Model);

                foreach (var command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command.CommandText))
                        continue;

                    await _dbContext.Database.ExecuteSqlRawAsync(
                        command.CommandText,
                        cancellationToken);

                    //ExecuteSqlRawAsync


                }

                await SaveSnapshotAsync(
                    currentSnapshot,
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch (Exception e)
            {
                var ee = e.Message;
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }



        private List<MigrationOperation> BuildOperations(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot)
        {
            var operations = new List<MigrationOperation>();

            operations.AddRange(AddNewTables(oldSnapshot, newSnapshot, operations));

            operations.AddRange(AddNewColumns(oldSnapshot, newSnapshot, operations));

            operations.AddRange(AddNewIndexes(oldSnapshot, newSnapshot, operations));

            operations.AddRange(AddNewForeignKeys(oldSnapshot, newSnapshot, operations));

            return operations.Distinct().ToList();
        }



        private List<MigrationOperation> AddNewTables(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var table in newSnapshot.Tables)
            {
                var oldTable = oldSnapshot.Tables
                    .FirstOrDefault(x =>
                        x.Schema == table.Schema &&
                        x.Name == table.Name);

                if (oldTable != null)
                    continue;

                var createTable = new CreateTableOperation
                {
                    Name = table.Name,
                    Schema = table.Schema
                };

                foreach (var column in table.Columns)
                {
                    var addColumn = CreateColumnOperation(
                        table,
                        column);

                    createTable.Columns.Add(addColumn);
                }

                // Primary Key
                if (table.PrimaryKey != null &&
                    table.PrimaryKey.Columns.Count > 0)
                {
                    createTable.PrimaryKey =
                        new AddPrimaryKeyOperation
                        {
                            Name = table.PrimaryKey.Name,
                            Schema = table.Schema,
                            Table = table.Name,
                            Columns = table.PrimaryKey.Columns.ToArray()
                        };
                }

                operations.Add(createTable);
            }

            return operations;

        }


        private List<MigrationOperation> AddNewColumns(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var table in newSnapshot.Tables)
            {
                var oldTable = oldSnapshot.Tables
                    .FirstOrDefault(x =>
                        x.Schema == table.Schema &&
                        x.Name == table.Name);

                if (oldTable == null)
                    continue;

                foreach (var column in table.Columns)
                {
                    var exists = oldTable.Columns.Any(x =>
                        x.Name == column.Name);

                    if (exists)
                        continue;

                    operations.Add(
                        CreateColumnOperation(
                            table,
                            column));
                }
            }

            return operations;
        }

        private AddColumnOperation CreateColumnOperation(
            TableSnapshot table,
            ColumnSnapshot column)
        {
            var operation = new AddColumnOperation
            {
                Name = column.Name,
                Table = table.Name,
                Schema = table.Schema,

                ClrType = GetClrType(column.ClrType),

                ColumnType = column.ColumnType,

                IsNullable = column.IsNullable,

                MaxLength = column.MaxLength,

                IsUnicode = column.IsUnicode,

                IsFixedLength = column.IsFixedLength,

                Precision = column.Precision,

                Scale = column.Scale,

                DefaultValue = column.DefaultValue,

                DefaultValueSql = column.DefaultValueSql,

                ComputedColumnSql = column.ComputedColumnSql,

                IsStored = column.IsStored,

                IsRowVersion = column.IsRowVersion
            };

            foreach (var annotation in column.Annotations)
            {
                operation[annotation.Key] =
                    annotation.Value;
            }

            return operation;
        }


        private List<MigrationOperation> AddNewIndexes(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var table in newSnapshot.Tables)
            {
                var oldTable = oldSnapshot.Tables
                    .FirstOrDefault(x =>
                        x.Schema == table.Schema &&
                        x.Name == table.Name);

                if (oldTable == null)
                {
                    oldTable = new TableSnapshot
                    {
                        Name = table.Name,
                        Schema = table.Schema
                    };
                }

                foreach (var index in table.Indexes)
                {
                    var exists = oldTable.Indexes.Any(x =>
                        x.Name == index.Name);

                    if (exists)
                        continue;

                    operations.Add(
                        new CreateIndexOperation
                        {
                            Name = index.Name,
                            Schema = table.Schema,
                            Table = table.Name,

                            Columns = index.Columns.ToArray(),

                            IsUnique = index.IsUnique.Value,

                            IsDescending =
                                index.IsDescending.ToArray(),

                            Filter = index.Filter
                        });
                }
            }


            return operations;
        }


        private List<MigrationOperation> AddNewForeignKeys(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {

            foreach (var table in newSnapshot.Tables)
            {
                var oldTable = oldSnapshot.Tables
                    .FirstOrDefault(x =>
                        x.Schema == table.Schema &&
                        x.Name == table.Name);

                foreach (var foreignKey in table.ForeignKeys)
                {
                    var exists =
                        oldTable?.ForeignKeys.Any(x =>
                            x.Name == foreignKey.Name) == true;

                    if (exists)
                        continue;

                    operations.Add(
                        new AddForeignKeyOperation
                        {
                            Name = foreignKey.Name,

                            Schema = table.Schema,

                            Table = table.Name,

                            Columns =
                                foreignKey.Columns.ToArray(),

                            PrincipalSchema =
                                foreignKey.PrincipalSchema,

                            PrincipalTable =
                                foreignKey.PrincipalTable,

                            PrincipalColumns =
                                foreignKey.PrincipalColumns.ToArray(),

                            OnDelete =
                                ConvertDeleteBehavior(
                                    foreignKey.DeleteBehavior)
                        });
                }
            }


            return operations;
        }

        private static ReferentialAction ConvertDeleteBehavior(
            DeleteBehavior behavior)
        {
            return behavior switch
            {
                DeleteBehavior.Cascade =>
                    ReferentialAction.Cascade,

                DeleteBehavior.SetNull =>
                    ReferentialAction.SetNull,

                DeleteBehavior.Restrict =>
                    ReferentialAction.Restrict,

                DeleteBehavior.NoAction =>
                    ReferentialAction.NoAction,

                _ =>
                    ReferentialAction.NoAction
            };
        }




        //***************************************************************************************************************
        //***************************************************************************************************************
        //***************************************************************************************************************


        private SchemaSnapshot CreateSnapshot()
        {

            var model =
                _dbContext
                    .GetService<IDesignTimeModel>()
                    .Model;

            var snapshot = new SchemaSnapshot();

            foreach (var entity in model.GetEntityTypes())
            {
                // ============================================================
                // 1. Table Mapping
                // ============================================================

                var tableMapping =
                    entity
                        .GetTableMappings()
                        .FirstOrDefault();

                if (tableMapping == null)
                    continue;

                var tableName =
                    tableMapping.Table.Name;

                if (string.IsNullOrWhiteSpace(tableName))
                    continue;


                var schema =
                    tableMapping.Table.Schema ?? "dbo";


                var table = new TableSnapshot
                {
                    Name = tableName,
                    Schema = schema
                };


                // ============================================================
                // Local helper:
                // Property -> Column Name
                // ============================================================

                string? GetColumnName(
                    IEntityType entityType,
                    IProperty property,
                    StoreObjectIdentifier table)
                {
                    var mapping =
                        entityType
                            .GetTableMappings()
                            .FirstOrDefault(x =>
                                x.Table.Name == table.Name &&
                                x.Table.Schema == table.Schema);

                    if (mapping == null)
                        return null;

                    var columnMapping =
                        mapping.ColumnMappings
                            .FirstOrDefault(x =>
                                x.Property == property);

                    if (columnMapping != null)
                        return columnMapping.Column.Name;

                    columnMapping =
                        mapping.ColumnMappings
                            .FirstOrDefault(x =>
                                x.Property.Name == property.Name);

                    return columnMapping?.Column.Name;
                }


                // ============================================================
                // 2. Columns
                // ============================================================

                foreach (var property in entity.GetProperties())
                {
                    var columnMapping =
                        tableMapping.ColumnMappings
                            .FirstOrDefault(x =>
                                x.Property == property);


                    if (columnMapping == null)
                    {
                        columnMapping =
                            tableMapping.ColumnMappings
                                .FirstOrDefault(x =>
                                    x.Property.Name == property.Name);
                    }

                    if (columnMapping == null)
                        continue;


                    var columnName =
                        columnMapping.Column.Name;

                    if (string.IsNullOrWhiteSpace(columnName))
                        continue;


                    // --------------------------------------------------------
                    // Annotations
                    // --------------------------------------------------------

                    var annotations =
                        new Dictionary<string, object?>();

                    foreach (var annotation in property.GetAnnotations())
                    {
                        annotations[annotation.Name] =
                            annotation.Value;
                    }


                    // --------------------------------------------------------
                    // Identity
                    // --------------------------------------------------------

                    var isIdentity =
                        property.GetValueGenerationStrategy()
                        == SqlServerValueGenerationStrategy.IdentityColumn;


                    // --------------------------------------------------------
                    // Computed
                    // --------------------------------------------------------

                    var computedColumnSql =
                        property.GetComputedColumnSql();

                    var isComputed =
                        !string.IsNullOrWhiteSpace(computedColumnSql);


                    // --------------------------------------------------------
                    // RowVersion
                    // --------------------------------------------------------

                    var isRowVersion =
                        property.IsConcurrencyToken &&
                        property.ValueGenerated ==
                            ValueGenerated.OnAddOrUpdate;


                    // --------------------------------------------------------
                    // Column
                    // --------------------------------------------------------

                    var column =
                        new ColumnSnapshot
                        {
                            IsIdentity =
                                isIdentity,

                            Name =
                                columnName,

                            ClrType =
                                property.ClrType.AssemblyQualifiedName
                                ?? property.ClrType.FullName
                                ?? property.ClrType.Name,


                            ColumnType =
                                columnMapping.Column.StoreType
                                ?? property.GetRelationalTypeMapping().StoreType,

                            IsNullable =
                                property.IsNullable,

                            MaxLength =
                                property.GetMaxLength(),

                            IsUnicode =
                                property.IsUnicode(),

                            IsFixedLength =
                                property.IsFixedLength(),

                            Precision =
                                property.GetPrecision(),

                            Scale =
                                property.GetScale(),

                            DefaultValue =
                                !isIdentity &&
                                !isComputed &&
                                !isRowVersion
                                    ? property.GetDefaultValue()
                                    : null,

                            DefaultValueSql =
                                !isIdentity &&
                                !isComputed &&
                                !isRowVersion
                                    ? property.GetDefaultValueSql()
                                    : null,

                            ComputedColumnSql =
                                computedColumnSql,

                            IsStored =
                                property.GetIsStored(),

                            IsRowVersion =
                                isRowVersion,

                            Annotations =
                                annotations
                        };


                    table.Columns.Add(column);
                }


                // ============================================================
                // 3. Primary Key
                // ============================================================

                var primaryKey =
    entity.FindPrimaryKey();

                if (primaryKey != null)
                {
                    var primaryKeyColumns =
                        primaryKey.Properties
                            .Select(property =>
                            {
                                var mapping =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property == property);

                                if (mapping != null)
                                    return mapping.Column.Name;

                                var fallback =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property.Name == property.Name);

                                return fallback?.Column.Name
                                    ?? property.Name;
                            })
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .ToList();

                    table.PrimaryKey =
                        new PrimaryKeySnapshot
                        {
                            Name =
                                primaryKey.GetName(),

                            Columns =
                                primaryKeyColumns
                        };
                }



                // ============================================================
                // 4. Indexes
                // ============================================================

                foreach (var index in entity.GetIndexes())
                {
                    var indexName =
                        index.GetDatabaseName()
                        ?? index.Name;

                    if (string.IsNullOrWhiteSpace(indexName))
                        continue;


                    var indexColumns =
                        index.Properties
                            .Select(property =>
                            {
                                var mapping =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property == property);

                                if (mapping != null)
                                    return mapping.Column.Name;


                                mapping =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property.Name ==
                                            property.Name);

                                return mapping?.Column.Name
                                    ?? property.Name;
                            })
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .ToList();



                    var isDescending =
                        index.IsDescending?
                            .Select(x => x != null ? x : false)
                            .ToArray()
                        ?? Array.Empty<bool>();


                    var indexItem =
                        new IndexSnapshot
                        {
                            Name =
                                indexName,

                            Columns =
                                indexColumns,

                            IsUnique =
                                index.IsUnique,

                            IsDescending =
                                isDescending,

                            Filter =
                                index.GetFilter()
                        };


                    table.Indexes.Add(indexItem);
                }


                // ============================================================
                // 5. Foreign Keys
                // ============================================================

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    var principalEntity =
                        foreignKey.PrincipalEntityType;

                    // --------------------------------------------------------
                    // Principal Table Mapping
                    // --------------------------------------------------------

                    var principalTableMapping =
                        principalEntity
                            .GetTableMappings()
                            .FirstOrDefault();

                    if (principalTableMapping == null)
                        continue;


                    var principalTableName =
                        principalTableMapping.Table.Name;

                    if (string.IsNullOrWhiteSpace(principalTableName))
                        continue;


                    var principalSchema =
                        principalTableMapping.Table.Schema
                        ?? "dbo";


                    // --------------------------------------------------------
                    // FK Constraint Name
                    // --------------------------------------------------------

                    var constraintName =
                        foreignKey.GetConstraintName();

                    if (string.IsNullOrWhiteSpace(constraintName))
                        continue;


                    // --------------------------------------------------------
                    // Dependent Columns
                    //
                    // UserRole.RoleId
                    // --------------------------------------------------------

                    var foreignKeyColumns =
                        foreignKey.Properties
                            .Select(property =>
                            {
                                var mapping =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property == property);

                                if (mapping != null)
                                    return mapping.Column.Name;


                                mapping =
                                    tableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property.Name ==
                                            property.Name);

                                return mapping?.Column.Name
                                    ?? property.Name;
                            })
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .ToList();



                    var principalColumns =
                        foreignKey.PrincipalKey.Properties
                            .Select(property =>
                            {
                                var mapping =
                                    principalTableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property == property);

                                if (mapping != null)
                                    return mapping.Column.Name;


                                mapping =
                                    principalTableMapping.ColumnMappings
                                        .FirstOrDefault(x =>
                                            x.Property.Name ==
                                            property.Name);

                                return mapping?.Column.Name
                                    ?? property.Name;
                            })
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .ToList();


                    // --------------------------------------------------------
                    // Foreign Key Snapshot
                    // --------------------------------------------------------

                    table.ForeignKeys.Add(
                        new ForeignKeySnapshot
                        {
                            Name =
                                constraintName,

                            Columns =
                                foreignKeyColumns,

                            PrincipalSchema =
                                principalSchema,

                            PrincipalTable =
                                principalTableName,

                            PrincipalColumns =
                                principalColumns,

                            DeleteBehavior =
                                foreignKey.DeleteBehavior
                        });
                }


                // ============================================================
                // 6. Add Table To Snapshot
                // ============================================================

                snapshot.Tables.Add(table);
            }


            return snapshot;
        }



        private async Task<SchemaSnapshot?>
            LoadSnapshotAsync(
                CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Database
                .SqlQueryRaw<int>(
                    $"""
                    SELECT COUNT(*) As Value
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = '{SnapshotSchema}'
                      AND TABLE_NAME = '{SnapshotTable}'
                    """)
                .FirstAsync(cancellationToken);

            if (exists == 0)
                return null;

            var tableSnapshot = await _dbContext.Database
                .SqlQueryRaw<TableSnapshotSerialized>(
                    $"""
                    SELECT *
                    FROM [{SnapshotSchema}].[{SnapshotTable}]
                    """)
                .ToListAsync(cancellationToken);

            var result = new SchemaSnapshot()
            {
                Tables = tableSnapshot.Select(c => new TableSnapshot()
                {
                    Name = c.TableName,
                    Schema = c.SchemaName,
                    Columns = JsonSerializer.Deserialize<List<ColumnSnapshot>>(c.JsonColumns),
                    PrimaryKey = JsonSerializer.Deserialize<PrimaryKeySnapshot>(c.JsonPrimaryKey),
                    ForeignKeys = JsonSerializer.Deserialize<List<ForeignKeySnapshot>>(c.JsonForeignKeys),
                    Indexes = JsonSerializer.Deserialize<List<IndexSnapshot>>(c.JsonIndexes)

                }).ToList()

            };
            if (tableSnapshot == null || tableSnapshot.Count() == 0)
                return null;

            return result;
        }




        private async Task SaveSnapshotAsync(
    SchemaSnapshot snapshot,
    CancellationToken cancellationToken = default)
        {
            var qualifiedTable =
                $"[{SnapshotSchema.Replace("]", "]]")}].[{SnapshotTable.Replace("]", "]]")}]";

            // Create snapshot table if it does not exist
            await _dbContext.Database.ExecuteSqlRawAsync(
                $"""
        IF OBJECT_ID(N'{SnapshotSchema}.{SnapshotTable}', 'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable}
            (
                Id INT IDENTITY(1,1) NOT NULL
                    CONSTRAINT PK_{SnapshotTable} PRIMARY KEY,

                TableName NVARCHAR(128) NOT NULL,
                SchemaName NVARCHAR(128) NOT NULL,

                JsonColumns NVARCHAR(MAX) NULL,
                JsonPrimaryKey NVARCHAR(MAX) NULL,
                JsonIndexes NVARCHAR(MAX) NULL,
                JsonForeignKeys NVARCHAR(MAX) NULL,

                UpdatedAt DATETIME2 NOT NULL
            );

            CREATE UNIQUE INDEX UX_{SnapshotTable}_Schema_Table
            ON {qualifiedTable}
            (
                SchemaName,
                TableName
            );
        END
        """,
                cancellationToken);

            //List<(string SchemaName, string TableName)> deleteTable = new();

            //foreach (var checkDeletedItem in snapshot.Tables)
            //{
            //    var deletedItem = await _dbContext.Database.ExecuteSqlRawAsync<(string SchemaName, string TableName)>(
            //    @$" SELECT TableName,SchemaName 
            //    FROM [{SnapshotSchema}].[{SnapshotTable}] 
            //    where SchemaName = {checkDeletedItem.Schema} 
            //    AND TableName = {checkDeletedItem.Name}"
            //    , cancellationToken);

            //    deleteTable.Add(());

            //}

            foreach (var item in snapshot.Tables)
            {
                var jsonColumns =
                    JsonSerializer.Serialize(
                        item.Columns,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });

                var jsonPrimaryKey =
                    JsonSerializer.Serialize(
                        item.PrimaryKey,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });

                var jsonIndexes =
                    JsonSerializer.Serialize(
                        item.Indexes,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });

                var jsonForeignKeys =
                    JsonSerializer.Serialize(
                        item.ForeignKeys,
                        new JsonSerializerOptions
                        {
                            WriteIndented = false
                        });

                await _dbContext.Database.ExecuteSqlRawAsync(
                    $"""
            MERGE {qualifiedTable} AS Target
            USING
            (
                SELECT
                    @TableName AS TableName,
                    @SchemaName AS SchemaName,
                    @JsonColumns AS JsonColumns,
                    @JsonPrimaryKey AS JsonPrimaryKey,
                    @JsonIndexes AS JsonIndexes,
                    @JsonForeignKeys AS JsonForeignKeys,
                    SYSUTCDATETIME() AS UpdatedAt
            ) AS Source

            ON Target.SchemaName = Source.SchemaName
            AND Target.TableName = Source.TableName

            WHEN MATCHED THEN
                UPDATE SET
                    JsonColumns = Source.JsonColumns,
                    JsonPrimaryKey = Source.JsonPrimaryKey,
                    JsonIndexes = Source.JsonIndexes,
                    JsonForeignKeys = Source.JsonForeignKeys,
                    UpdatedAt = Source.UpdatedAt

            WHEN NOT MATCHED THEN
                INSERT
                (
                    TableName,
                    SchemaName,
                    JsonColumns,
                    JsonPrimaryKey,
                    JsonIndexes,
                    JsonForeignKeys,
                    UpdatedAt
                )
                VALUES
                (
                    Source.TableName,
                    Source.SchemaName,
                    Source.JsonColumns,
                    Source.JsonPrimaryKey,
                    Source.JsonIndexes,
                    Source.JsonForeignKeys,
                    Source.UpdatedAt
                );
            """,
                    new object[]
                    {
                new SqlParameter("@TableName", item.Name),
                new SqlParameter("@SchemaName", item.Schema),
                new SqlParameter("@JsonColumns", jsonColumns),
                new SqlParameter("@JsonPrimaryKey", jsonPrimaryKey),
                new SqlParameter("@JsonIndexes", jsonIndexes),
                new SqlParameter("@JsonForeignKeys", jsonForeignKeys)
                    },
                    cancellationToken);
            }
        }




        private async Task AcquireApplicationLockAsync(
            CancellationToken cancellationToken)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                """
                DECLARE @Result INT;

                EXEC @Result = sp_getapplock
                    @Resource = 'GenericRepository_AutoMigration',
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 60000;

                IF @Result < 0
                    THROW 51000,
                          'Could not acquire GenericRepository migration lock.',
                          1;
                """,
                cancellationToken);
        }

        private static Type GetClrType(
            string clrType)
        {
            return Type.GetType(clrType)
                   ?? typeof(string);
        }
    }

}
