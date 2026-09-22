using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;


namespace GenericRepository.Context.AutoMigration
{


    public sealed class GenericAutoMigrationCommandDb
    {
        private const string SnapshotSchema = "dbo";
        private const string SnapshotTable = "__GenericRepositorySchemaSnapshot";
        private readonly string DbName;


        private readonly GenericCommandDbContext _dbContext;
        private readonly IMigrationsSqlGenerator _sqlGenerator;

        public GenericAutoMigrationCommandDb(
             GenericCommandDbContext dbContext)
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
            await _dbContext.Database.EnsureCreatedAsync(
                cancellationToken);

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
                }

                await SaveSnapshotAsync(
                    currentSnapshot,
                    cancellationToken);

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



        private List<MigrationOperation> BuildOperations(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot)
        {
            var operations = new List<MigrationOperation>();

            AddNewTables(
                oldSnapshot,
                newSnapshot,
                operations);

            AddNewColumns(
                oldSnapshot,
                newSnapshot,
                operations);

            AddNewIndexes(
                oldSnapshot,
                newSnapshot,
                operations);

            AddNewForeignKeys(
                oldSnapshot,
                newSnapshot,
                operations);

            return operations;
        }



        private void AddNewTables(
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
        }


        private void AddNewColumns(
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


        private void AddNewIndexes(
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

                            IsUnique = index.IsUnique,

                            IsDescending =
                                index.IsDescending.ToArray(),

                            Filter = index.Filter
                        });
                }
            }
        }


        private void AddNewForeignKeys(
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



        private SchemaSnapshot CreateSnapshot()
        {
            var model = _dbContext.Model;

            var snapshot = new SchemaSnapshot();

            foreach (var entity in model.GetEntityTypes())
            {
                var tableName =
                    entity.GetTableName();

                if (string.IsNullOrWhiteSpace(tableName))
                    continue;

                var schema =
                    entity.GetSchema()
                    ?? SnapshotSchema;

                var table = new TableSnapshot
                {
                    Name = tableName,
                    Schema = schema
                };



                var tableIdentifier =
                    StoreObjectIdentifier.Table(
                        tableName,
                        schema);

                foreach (var property in entity.GetProperties())
                {
                    var columnName =
                        property.GetColumnName(
                            tableIdentifier);

                    if (string.IsNullOrWhiteSpace(columnName))
                        continue;

                    var annotations =
                        new Dictionary<string, object?>();

                    foreach (var annotation in property.GetAnnotations())
                    {
                        annotations[annotation.Name] =
                            annotation.Value;
                    }

                    var column = new ColumnSnapshot
                    {
                        Name = columnName,

                        ClrType =
                            property.ClrType.AssemblyQualifiedName
                            ?? property.ClrType.FullName
                            ?? property.ClrType.Name,

                        ColumnType =
                            property.GetColumnType(
                                tableIdentifier)
                            ?? property.GetRelationalTypeMapping()
                                .StoreType,

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
                            property.GetDefaultValue(),

                        DefaultValueSql =
                            property.GetDefaultValueSql(),

                        ComputedColumnSql =
                            property.GetComputedColumnSql(),

                        IsStored =
                            property.GetIsStored(),

                        IsRowVersion =
                            property.IsConcurrencyToken &&
                            property.ValueGenerated ==
                                ValueGenerated.OnAddOrUpdate,

                        Annotations = annotations
                    };

                    table.Columns.Add(column);
                }



                var primaryKey =
                    entity.FindPrimaryKey();

                if (primaryKey != null)
                {
                    table.PrimaryKey =
                        new PrimaryKeySnapshot
                        {
                            Name =
                                primaryKey
                                    .GetName(),

                            Columns =
                                primaryKey.Properties
                                    .Select(x =>
                                        x.GetColumnName(
                                            tableIdentifier))
                                    .Where(x =>
                                        !string.IsNullOrWhiteSpace(x))
                                    .Cast<string>()
                                    .ToList()
                        };
                }



                foreach (var index in entity.GetIndexes())
                {
                    var indexName =
                        index.GetDatabaseName(
                            tableIdentifier);

                    if (string.IsNullOrWhiteSpace(indexName))
                        continue;

                    var indexColumns =
                        index.Properties
                            .Select(x =>
                                x.GetColumnName(
                                    tableIdentifier))
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .Cast<string>()
                            .ToList();

                    table.Indexes.Add(
                        new IndexSnapshot
                        {
                            Name = indexName,

                            Columns = indexColumns,

                            IsUnique =
                                index.IsUnique,

                            IsDescending =
                                index.IsDescending.ToArray(),

                            Filter =
                                index.GetFilter()
                        });
                }



                foreach (var foreignKey
                    in entity.GetForeignKeys())
                {
                    var principalEntity =
                        foreignKey.PrincipalEntityType;

                    var principalTableName =
                        principalEntity.GetTableName();

                    if (string.IsNullOrWhiteSpace(
                        principalTableName))
                    {
                        continue;
                    }

                    var principalSchema =
                        principalEntity.GetSchema()
                        ?? SnapshotSchema;

                    var constraintName =
                        foreignKey.GetConstraintName();

                    if (string.IsNullOrWhiteSpace(
                        constraintName))
                    {
                        continue;
                    }

                    table.ForeignKeys.Add(
                        new ForeignKeySnapshot
                        {
                            Name =
                                constraintName,

                            Columns =
                                foreignKey.Properties
                                    .Select(x =>
                                        x.GetColumnName(
                                            tableIdentifier))
                                    .Where(x =>
                                        !string.IsNullOrWhiteSpace(x))
                                    .Cast<string>()
                                    .ToList(),

                            PrincipalSchema =
                                principalSchema,

                            PrincipalTable =
                                principalTableName,

                            PrincipalColumns =
                                foreignKey.PrincipalKey
                                    .Properties
                                    .Select(x =>
                                        x.GetColumnName(
                                            StoreObjectIdentifier.Table(
                                                principalTableName,
                                                principalSchema)))
                                    .Where(x =>
                                        !string.IsNullOrWhiteSpace(x))
                                    .Cast<string>()
                                    .ToList(),

                            DeleteBehavior =
                                foreignKey.DeleteBehavior
                        });
                }

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

            var json = await _dbContext.Database
                .SqlQueryRaw<string>(
                    $"""
                    SELECT SnapshotJson As Value
                    FROM [{SnapshotSchema}].[{SnapshotTable}]
                    WHERE Id = 1
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<SchemaSnapshot>(
                json);
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




    public sealed class SchemaSnapshot
    {
        public List<TableSnapshot> Tables { get; set; } = new();
    }

    public sealed class TableSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public string Schema { get; set; } = "dbo";

        public List<ColumnSnapshot> Columns { get; set; } = new();

        public PrimaryKeySnapshot? PrimaryKey { get; set; }

        public List<IndexSnapshot> Indexes { get; set; } = new();

        public List<ForeignKeySnapshot> ForeignKeys { get; set; } = new();
    }

    public sealed class ColumnSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public string ClrType { get; set; } = string.Empty;

        public string? ColumnType { get; set; }

        public bool IsNullable { get; set; }

        public int? MaxLength { get; set; }

        public bool? IsUnicode { get; set; }

        public bool? IsFixedLength { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public object? DefaultValue { get; set; }

        public string? DefaultValueSql { get; set; }

        public string? ComputedColumnSql { get; set; }

        public bool? IsStored { get; set; }

        public bool IsRowVersion { get; set; }

        public Dictionary<string, object?> Annotations { get; set; }
            = new();
    }

    public sealed class PrimaryKeySnapshot
    {
        public string? Name { get; set; }

        public List<string> Columns { get; set; } = new();
    }

    public sealed class IndexSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Columns { get; set; } = new();

        public bool IsUnique { get; set; }

        public bool[] IsDescending { get; set; } = Array.Empty<bool>();

        public string? Filter { get; set; }
    }

    public sealed class ForeignKeySnapshot
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Columns { get; set; } = new();

        public string PrincipalSchema { get; set; } = "dbo";

        public string PrincipalTable { get; set; } = string.Empty;

        public List<string> PrincipalColumns { get; set; } = new();

        public DeleteBehavior DeleteBehavior { get; set; }
    }
}

