using GenericRepository.Context;
using GenericRepository.Models.AutoMigration.Creation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Text.Json;

namespace GenericRepository.AutoMigration.SQLMigration
{
    public sealed class GenericAutoMigrationCommandDb
    {
        private const string SnapshotSchema = "dbo";
        private const string SnapshotTable =
            "__GenericRepositorySchemaSnapshot";

        private readonly string DbName;

        private readonly GenericCommandDbContext _dbContext;

        private readonly IMigrationsSqlGenerator _sqlGenerator;

        public GenericAutoMigrationCommandDb(
            GenericCommandDbContext dbContext)
        {
            _dbContext = dbContext;

            _sqlGenerator =
                dbContext
                    .GetInfrastructure()
                    .GetRequiredService<IMigrationsSqlGenerator>();

            DbName =
                _dbContext
                    .Database
                    .GetDbConnection()
                    .Database;
        }


        public async Task SynchronizeAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await SyncAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SyncAsync(
            CancellationToken cancellationToken = default)
        {
            
            if (!await _dbContext.Database.CanConnectAsync(
                    cancellationToken))
            {
                await _dbContext.Database.EnsureCreatedAsync(
                    cancellationToken);
            }

            
            var currentSnapshot =
                CreateSnapshot();

            
            var previousSnapshot =
                await LoadSnapshotAsync(
                    cancellationToken);


            if (previousSnapshot == null)
            {
                await SaveSnapshotAsync(
                    currentSnapshot,
                    cancellationToken);

                return;
            }


            var operations =
                BuildOperations(
                    previousSnapshot,
                    currentSnapshot);

            if (operations.Count == 0)
            {
                return;
            }



            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {


                await AcquireApplicationLockAsync(
                    cancellationToken);


                var commands =
                    _sqlGenerator.Generate(
                        operations,
                        _dbContext.Model);


                foreach (var command in commands)
                {
                    if (string.IsNullOrWhiteSpace(
                            command.CommandText))
                    {
                        continue;
                    }

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
            var operations =
                new List<MigrationOperation>();

            AddDeletedForeignKeys(
                oldSnapshot,
                newSnapshot,
                ref operations);


            operations.AddRange(AddChangedForeignKeys(
                oldSnapshot,
                newSnapshot,
                operations));

            // 2. Indexes
            operations.AddRange(AddDeletedIndexes(
                oldSnapshot,
                newSnapshot,
                operations));

            operations.AddRange(AddChangedIndexes(
                oldSnapshot,
                newSnapshot,
                operations));

            // 3. Primary Keys
            operations.AddRange(AddDeletedPrimaryKeys(
                oldSnapshot,
                newSnapshot,
                operations));

            operations.AddRange(AddChangedPrimaryKeys(
                oldSnapshot,
                newSnapshot,
                operations));


            // 4. Columns
            AddDeletedColumns(
                oldSnapshot,
                newSnapshot,
                ref operations);

            operations.AddRange(AddModifiedColumns(
                oldSnapshot,
                newSnapshot,
                operations));

            // 5. Tables
            AddDeletedTables(
                oldSnapshot,
                newSnapshot,
                ref operations);


            // CREATE PHASE

            // 6. Tables
            operations.AddRange(AddNewTables(
                oldSnapshot,
                newSnapshot,
                operations));

            // 7. Columns
            operations.AddRange(AddNewColumns(
                oldSnapshot,
                newSnapshot,
                operations));

            // 8. Primary Keys
            operations.AddRange(AddNewPrimaryKeys(
                oldSnapshot,
                newSnapshot,
                operations));

            // 9. Indexes
            operations.AddRange(AddNewIndexes(
                oldSnapshot,
                newSnapshot,
                operations));

            // 10. Foreign Keys
            operations.AddRange(AddNewForeignKeys(
                oldSnapshot,
                newSnapshot,
                operations));

            return operations
                .Distinct()
                .ToList();
        }



        private List<MigrationOperation> AddNewTables(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var table in newSnapshot.Tables)
            {
                var oldTable =
                    oldSnapshot.Tables.FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Schema,
                                table.Schema,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            string.Equals(
                                x.Name,
                                table.Name,
                                StringComparison.OrdinalIgnoreCase));

                if (oldTable != null)
                    continue;

                var createTable =
                    new CreateTableOperation
                    {
                        Name = table.Name,
                        Schema = table.Schema
                    };

                foreach (var column in table.Columns)
                {
                    var addColumn =
                        CreateColumnOperation(
                            table,
                            column);

                    createTable.Columns.Add(
                        addColumn);
                }


                if (table.PrimaryKey != null &&
                    table.PrimaryKey.Columns.Count > 0)
                {
                    createTable.PrimaryKey =
                        new AddPrimaryKeyOperation
                        {
                            Name =
                                table.PrimaryKey.Name,

                            Schema =
                                table.Schema,

                            Table =
                                table.Name,

                            Columns =
                                table.PrimaryKey.Columns.ToArray()
                        };
                }

                operations.Add(
                    createTable);
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
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        table.Schema,
                        table.Name);

                // Entirely new table
                if (oldTable == null)
                    continue;

                foreach (var column in table.Columns)
                {
                    var exists =
                        oldTable.Columns.Any(
                            x =>
                                string.Equals(
                                    x.Name,
                                    column.Name,
                                    StringComparison.OrdinalIgnoreCase));

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


        private void AddDeletedTables(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            ref List<MigrationOperation> operations)
        {

            SchemaSnapshot oldSnapshotWithOrderByDescending = new()
            {
                Tables = oldSnapshot.Tables.Where(x => x.ForeignKeys != null && x.ForeignKeys.Count() > 0)
                .OrderByDescending(x => x.ForeignKeys.Count())
                .ToList()
            };

            oldSnapshotWithOrderByDescending.Tables.AddRange(oldSnapshot.Tables
                .Where(x => x.ForeignKeys == null || x.ForeignKeys.Count() == 0).ToList());


            foreach (var oldTable in oldSnapshotWithOrderByDescending.Tables)
            {
                var currentTable =
                    FindTable(
                        newSnapshot,
                        oldTable.Schema,
                        oldTable.Name);

                if (currentTable != null)
                    continue;

                operations.Add(
                    new DropTableOperation
                    {
                        Name =
                            oldTable.Name,

                        Schema =
                            oldTable.Schema
                    });
            }

        }



        private void AddDeletedColumns(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            ref List<MigrationOperation> operations)
        {
            foreach (var oldTable in oldSnapshot.Tables)
            {
                var currentTable =
                    FindTable(
                        newSnapshot,
                        oldTable.Schema,
                        oldTable.Name);

                if (currentTable == null)
                    continue;

                foreach (var oldColumn in oldTable.Columns)
                {
                    var currentColumn =
                        currentTable.Columns.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    oldColumn.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (currentColumn != null)
                        continue;

                    operations.Add(
                        new DropColumnOperation
                        {
                            Name =
                                oldColumn.Name,

                            Table =
                                oldTable.Name,

                            Schema =
                                oldTable.Schema
                        });
                }
            }

        }


        private List<MigrationOperation> AddModifiedColumns(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var currentTable in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        currentTable.Schema,
                        currentTable.Name);

                if (oldTable == null)
                    continue;

                foreach (var newColumn in currentTable.Columns)
                {
                    var oldColumn =
                        oldTable.Columns.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    newColumn.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (oldColumn == null)
                        continue;

                    if (AreColumnsEqual(
                            oldColumn,
                            newColumn))
                    {
                        continue;
                    }

                    var operation =
                        new AlterColumnOperation
                        {
                            Name =
                                newColumn.Name,

                            Table =
                                currentTable.Name,

                            Schema =
                                currentTable.Schema,

                            ClrType =
                                GetClrType(
                                    newColumn.ClrType),

                            ColumnType =
                                newColumn.ColumnType,

                            IsNullable =
                                newColumn.IsNullable,

                            MaxLength =
                                newColumn.MaxLength,

                            IsUnicode =
                                newColumn.IsUnicode,

                            IsFixedLength =
                                newColumn.IsFixedLength,

                            Precision =
                                newColumn.Precision,

                            Scale =
                                newColumn.Scale,

                            DefaultValue =
                                NormalizeJsonValue(
                                    newColumn.DefaultValue),

                            DefaultValueSql =
                                newColumn.DefaultValueSql,

                            ComputedColumnSql =
                                newColumn.ComputedColumnSql,

                            IsStored =
                                newColumn.IsStored,

                            IsRowVersion =
                                newColumn.IsRowVersion,

                            OldColumn =
                                CreateColumnOperation(
                                    oldTable,
                                    oldColumn)
                        };

                    foreach (var annotation in
                             newColumn.Annotations)
                    {
                        operation[annotation.Key] =
                            annotation.Value;
                    }

                    operations.Add(
                        operation);
                }
            }

            return operations;
        }


        private List<MigrationOperation> AddDeletedPrimaryKeys(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var oldTable in oldSnapshot.Tables)
            {
                var currentTable =
                    FindTable(
                        newSnapshot,
                        oldTable.Schema,
                        oldTable.Name);

                if (currentTable == null)
                    continue;

                if (oldTable.PrimaryKey == null)
                    continue;

                if (currentTable.PrimaryKey != null)
                    continue;

                operations.Add(
                    new DropPrimaryKeyOperation
                    {
                        Name =
                            oldTable.PrimaryKey.Name,

                        Schema =
                            oldTable.Schema,

                        Table =
                            oldTable.Name
                    });
            }

            return operations;
        }


        private List<MigrationOperation> AddChangedPrimaryKeys(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var currentTable in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        currentTable.Schema,
                        currentTable.Name);

                if (oldTable == null)
                    continue;

                var oldPk =
                    oldTable.PrimaryKey;

                var newPk =
                    currentTable.PrimaryKey;

                if (oldPk == null ||
                    newPk == null)
                {
                    continue;
                }

                if (ArePrimaryKeysEqual(
                        oldPk,
                        newPk))
                {
                    continue;
                }

                operations.Add(
                    new DropPrimaryKeyOperation
                    {
                        Name =
                            oldPk.Name,

                        Schema =
                            currentTable.Schema,

                        Table =
                            currentTable.Name
                    });

                operations.Add(
                    new AddPrimaryKeyOperation
                    {
                        Name =
                            newPk.Name,

                        Schema =
                            currentTable.Schema,

                        Table =
                            currentTable.Name,

                        Columns =
                            newPk.Columns.ToArray()
                    });
            }

            return operations;
        }

        private List<MigrationOperation> AddNewPrimaryKeys(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var currentTable in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        currentTable.Schema,
                        currentTable.Name);

                if (oldTable == null)
                    continue;

                if (oldTable.PrimaryKey != null)
                    continue;

                var newPk =
                    currentTable.PrimaryKey;

                if (newPk == null ||
                    newPk.Columns.Count == 0)
                {
                    continue;
                }

                operations.Add(
                    new AddPrimaryKeyOperation
                    {
                        Name =
                            newPk.Name,

                        Schema =
                            currentTable.Schema,

                        Table =
                            currentTable.Name,

                        Columns =
                            newPk.Columns.ToArray()
                    });
            }
            return operations;
        }


        private List<MigrationOperation> AddDeletedIndexes(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var oldTable in oldSnapshot.Tables)
            {
                var currentTable =
                    FindTable(
                        newSnapshot,
                        oldTable.Schema,
                        oldTable.Name);

                if (currentTable == null)
                    continue;

                foreach (var oldIndex in oldTable.Indexes)
                {
                    var currentIndex =
                        currentTable.Indexes.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    oldIndex.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (currentIndex != null)
                        continue;

                    operations.Add(
                        new DropIndexOperation
                        {
                            Name =
                                oldIndex.Name,

                            Schema =
                                oldTable.Schema,

                            Table =
                                oldTable.Name
                        });
                }
            }

            return operations;
        }


        private List<MigrationOperation> AddChangedIndexes(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var currentTable in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        currentTable.Schema,
                        currentTable.Name);

                if (oldTable == null)
                    continue;

                foreach (var newIndex in currentTable.Indexes)
                {
                    var oldIndex =
                        oldTable.Indexes.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    newIndex.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (oldIndex == null)
                        continue;

                    if (AreIndexesEqual(
                            oldIndex,
                            newIndex))
                    {
                        continue;
                    }

                    // Drop old
                    operations.Add(
                        new DropIndexOperation
                        {
                            Name =
                                oldIndex.Name,

                            Schema =
                                currentTable.Schema,

                            Table =
                                currentTable.Name
                        });

                    // Create new
                    operations.Add(
                        CreateIndexOperation(
                            currentTable,
                            newIndex));
                }
            }

            return operations;
        }


        private List<MigrationOperation> AddNewIndexes(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var table in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        table.Schema,
                        table.Name);


                if (oldTable == null)
                {
                    oldTable =
                        new TableSnapshot
                        {
                            Name =
                                table.Name,

                            Schema =
                                table.Schema
                        };
                }

                foreach (var index in table.Indexes)
                {
                    var exists =
                        oldTable.Indexes.Any(
                            x =>
                                string.Equals(
                                    x.Name,
                                    index.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (exists)
                        continue;

                    operations.Add(
                        CreateIndexOperation(
                            table,
                            index));
                }
            }

            return operations;
        }

        

        private void AddDeletedForeignKeys(
    SchemaSnapshot oldSnapshot,
    SchemaSnapshot newSnapshot,
    ref List<MigrationOperation> operations)
        {
            SchemaSnapshot oldSnapshotWithOrderByDescending = new()
            {
                Tables = oldSnapshot.Tables.Where(x => x.ForeignKeys != null && x.ForeignKeys.Count() > 0)
                .OrderByDescending(x => x.ForeignKeys.Count())
                .ToList()
            };



            foreach (var oldTable in oldSnapshotWithOrderByDescending.Tables)
            {

                foreach (var oldForeignKey in oldTable.ForeignKeys)
                {

                    var currentTable =
                        FindTable(
                            newSnapshot,
                            oldTable.Schema,
                            oldTable.Name);



                    if (currentTable == null)
                        continue;


                    var currentForeignKey =
                        currentTable.ForeignKeys.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    oldForeignKey.Name,
                                    StringComparison.OrdinalIgnoreCase));


                    if (currentForeignKey != null)
                        continue;


                    operations.Add(
                        new DropForeignKeyOperation
                        {
                            Name =
                                oldForeignKey.Name,

                            Schema =
                                oldTable.Schema,

                            Table =
                                oldTable.Name
                        });
                }
            }


            foreach (var oldTable in oldSnapshotWithOrderByDescending.Tables)
            {
                var currentDependentTable =
                    FindTable(
                        newSnapshot,
                        oldTable.Schema,
                        oldTable.Name);

                if (currentDependentTable == null)
                    continue;

                foreach (var oldForeignKey in oldTable.ForeignKeys)
                {

                    var principalTable =
                        FindTable(
                            newSnapshot,
                            oldForeignKey.PrincipalSchema,
                            oldForeignKey.PrincipalTable);

                    if (principalTable != null)
                        continue;


                    var alreadyAdded =
                        operations
                            .OfType<DropForeignKeyOperation>()
                            .Any(
                                x =>
                                    string.Equals(
                                        x.Name,
                                        oldForeignKey.Name,
                                        StringComparison.OrdinalIgnoreCase)
                                    &&
                                    string.Equals(
                                        x.Table,
                                        oldTable.Name,
                                        StringComparison.OrdinalIgnoreCase)
                                    &&
                                    string.Equals(
                                        x.Schema,
                                        oldTable.Schema,
                                        StringComparison.OrdinalIgnoreCase));

                    if (alreadyAdded)
                        continue;

                    operations.Add(
                        new DropForeignKeyOperation
                        {
                            Name =
                                oldForeignKey.Name,

                            Schema =
                                oldTable.Schema,

                            Table =
                                oldTable.Name
                        });
                }
            }

        }
        



        private List<MigrationOperation> AddChangedForeignKeys(
            SchemaSnapshot oldSnapshot,
            SchemaSnapshot newSnapshot,
            List<MigrationOperation> operations)
        {
            foreach (var currentTable in newSnapshot.Tables)
            {
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        currentTable.Schema,
                        currentTable.Name);

                if (oldTable == null)
                    continue;

                foreach (var newForeignKey in
                         currentTable.ForeignKeys)
                {
                    var oldForeignKey =
                        oldTable.ForeignKeys.FirstOrDefault(
                            x =>
                                string.Equals(
                                    x.Name,
                                    newForeignKey.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (oldForeignKey == null)
                        continue;

                    if (AreForeignKeysEqual(
                            oldForeignKey,
                            newForeignKey))
                    {
                        continue;
                    }

                    operations.Add(
                        new DropForeignKeyOperation
                        {
                            Name =
                                oldForeignKey.Name,

                            Schema =
                                currentTable.Schema,

                            Table =
                                currentTable.Name
                        });

                    operations.Add(
                        CreateForeignKeyOperation(
                            currentTable,
                            newForeignKey));
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
                var oldTable =
                    FindTable(
                        oldSnapshot,
                        table.Schema,
                        table.Name);

                foreach (var foreignKey in
                         table.ForeignKeys)
                {
                    var exists =
                        oldTable?.ForeignKeys.Any(
                            x =>
                                string.Equals(
                                    x.Name,
                                    foreignKey.Name,
                                    StringComparison.OrdinalIgnoreCase))
                        == true;

                    if (exists)
                        continue;

                    operations.Add(
                        CreateForeignKeyOperation(
                            table,
                            foreignKey));
                }
            }

            return operations;
        }



        private AddColumnOperation CreateColumnOperation(
            TableSnapshot table,
            ColumnSnapshot column)
        {
            var operation =
                new AddColumnOperation
                {
                    Name =
                        column.Name,

                    Table =
                        table.Name,

                    Schema =
                        table.Schema,

                    ClrType =
                        GetClrType(
                            column.ClrType),

                    ColumnType =
                        column.ColumnType,

                    IsNullable =
                        column.IsNullable,

                    MaxLength =
                        column.MaxLength,

                    IsUnicode =
                        column.IsUnicode,

                    IsFixedLength =
                        column.IsFixedLength,

                    Precision =
                        column.Precision,

                    Scale =
                        column.Scale,

                    DefaultValue =
                        NormalizeJsonValue(
                            column.DefaultValue),

                    DefaultValueSql =
                        column.DefaultValueSql,

                    ComputedColumnSql =
                        column.ComputedColumnSql,

                    IsStored =
                        column.IsStored,

                    IsRowVersion =
                        column.IsRowVersion
                };

            foreach (var annotation in
                     column.Annotations)
            {
                operation[annotation.Key] =
                    annotation.Value;
            }

            return operation;
        }


        private CreateIndexOperation CreateIndexOperation(
            TableSnapshot table,
            IndexSnapshot index)
        {
            return new CreateIndexOperation
            {
                Name =
                    index.Name,

                Schema =
                    table.Schema,

                Table =
                    table.Name,

                Columns =
                    index.Columns.ToArray(),

                IsUnique =
                    index.IsUnique ?? false,

                IsDescending =
                    index.IsDescending?.ToArray()
                    ?? Array.Empty<bool>(),

                Filter =
                    index.Filter
            };
        }


        private AddForeignKeyOperation CreateForeignKeyOperation(
            TableSnapshot table,
            ForeignKeySnapshot foreignKey)
        {
            return new AddForeignKeyOperation
            {
                Name =
                    foreignKey.Name,

                Schema =
                    table.Schema,

                Table =
                    table.Name,

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
            };
        }


        private static bool AreColumnsEqual(
            ColumnSnapshot oldColumn,
            ColumnSnapshot newColumn)
        {
            if (!string.Equals(
                    oldColumn.Name,
                    newColumn.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(
                    oldColumn.ClrType,
                    newColumn.ClrType,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(
                    oldColumn.ColumnType,
                    newColumn.ColumnType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (oldColumn.IsNullable !=
                newColumn.IsNullable)
            {
                return false;
            }

            if (oldColumn.MaxLength !=
                newColumn.MaxLength)
            {
                return false;
            }

            if (oldColumn.IsUnicode !=
                newColumn.IsUnicode)
            {
                return false;
            }

            if (oldColumn.IsFixedLength !=
                newColumn.IsFixedLength)
            {
                return false;
            }

            if (oldColumn.Precision !=
                newColumn.Precision)
            {
                return false;
            }

            if (oldColumn.Scale !=
                newColumn.Scale)
            {
                return false;
            }

            if (oldColumn.IsIdentity !=
                newColumn.IsIdentity)
            {
                return false;
            }

            if (oldColumn.IsRowVersion !=
                newColumn.IsRowVersion)
            {
                return false;
            }

            if (!string.Equals(
                    oldColumn.ComputedColumnSql,
                    newColumn.ComputedColumnSql,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(
                    oldColumn.DefaultValueSql,
                    newColumn.DefaultValueSql,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return JsonValuesEqual(
                oldColumn.DefaultValue,
                newColumn.DefaultValue);
        }


        private static bool AreIndexesEqual(
            IndexSnapshot oldIndex,
            IndexSnapshot newIndex)
        {
            if (!string.Equals(
                    oldIndex.Name,
                    newIndex.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (oldIndex.IsUnique !=
                newIndex.IsUnique)
            {
                return false;
            }

            if (!StringListEqual(
                    oldIndex.Columns,
                    newIndex.Columns))
            {
                return false;
            }

            if (!BoolListEqual(
                    oldIndex.IsDescending,
                    newIndex.IsDescending))
            {
                return false;
            }

            return string.Equals(
                oldIndex.Filter,
                newIndex.Filter,
                StringComparison.OrdinalIgnoreCase);
        }


        private static bool AreForeignKeysEqual(
            ForeignKeySnapshot oldForeignKey,
            ForeignKeySnapshot newForeignKey)
        {
            if (!string.Equals(
                    oldForeignKey.Name,
                    newForeignKey.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!StringListEqual(
                    oldForeignKey.Columns,
                    newForeignKey.Columns))
            {
                return false;
            }

            if (!string.Equals(
                    oldForeignKey.PrincipalSchema,
                    newForeignKey.PrincipalSchema,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(
                    oldForeignKey.PrincipalTable,
                    newForeignKey.PrincipalTable,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!StringListEqual(
                    oldForeignKey.PrincipalColumns,
                    newForeignKey.PrincipalColumns))
            {
                return false;
            }

            return oldForeignKey.DeleteBehavior ==
                   newForeignKey.DeleteBehavior;
        }


        private static bool ArePrimaryKeysEqual(
            PrimaryKeySnapshot oldPrimaryKey,
            PrimaryKeySnapshot newPrimaryKey)
        {
            if (!string.Equals(
                    oldPrimaryKey.Name,
                    newPrimaryKey.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return StringListEqual(
                oldPrimaryKey.Columns,
                newPrimaryKey.Columns);
        }


        private static bool StringListEqual(
            IEnumerable<string>? first,
            IEnumerable<string>? second)
        {
            var firstList =
                first?.ToList()
                ?? new List<string>();

            var secondList =
                second?.ToList()
                ?? new List<string>();

            if (firstList.Count !=
                secondList.Count)
            {
                return false;
            }

            for (var i = 0;
                 i < firstList.Count;
                 i++)
            {
                if (!string.Equals(
                        firstList[i],
                        secondList[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool BoolListEqual(
            IEnumerable<bool>? first,
            IEnumerable<bool>? second)
        {
            var firstList =
                first?.ToList()
                ?? new List<bool>();

            var secondList =
                second?.ToList()
                ?? new List<bool>();

            if (firstList.Count !=
                secondList.Count)
            {
                return false;
            }

            for (var i = 0;
                 i < firstList.Count;
                 i++)
            {
                if (firstList[i] !=
                    secondList[i])
                {
                    return false;
                }
            }

            return true;
        }


        private static bool JsonValuesEqual(
            object? first,
            object? second)
        {
            var firstValue =
                NormalizeJsonValue(first);

            var secondValue =
                NormalizeJsonValue(second);

            if (firstValue == null &&
                secondValue == null)
            {
                return true;
            }

            if (firstValue == null ||
                secondValue == null)
            {
                return false;
            }

            return string.Equals(
                firstValue.ToString(),
                secondValue.ToString(),
                StringComparison.Ordinal);
        }


        private static object? NormalizeJsonValue(
            object? value)
        {
            if (value is not JsonElement element)
                return value;

            return element.ValueKind switch
            {
                JsonValueKind.Null =>
                    null,

                JsonValueKind.String =>
                    element.GetString(),

                JsonValueKind.True =>
                    true,

                JsonValueKind.False =>
                    false,

                JsonValueKind.Number when
                    element.TryGetInt32(
                        out var intValue) =>
                    intValue,

                JsonValueKind.Number when
                    element.TryGetInt64(
                        out var longValue) =>
                    longValue,

                JsonValueKind.Number when
                    element.TryGetDecimal(
                        out var decimalValue) =>
                    decimalValue,

                JsonValueKind.Number when
                    element.TryGetDouble(
                        out var doubleValue) =>
                    doubleValue,

                _ =>
                    element.ToString()
            };
        }


        private static TableSnapshot? FindTable(
            SchemaSnapshot snapshot,
            string? schema,
            string? tableName)
        {
            return snapshot.Tables.FirstOrDefault(
                x =>
                    string.Equals(
                        x.Schema,
                        schema,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        x.Name,
                        tableName,
                        StringComparison.OrdinalIgnoreCase));
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
            var model =
                _dbContext
                    .GetService<IDesignTimeModel>()
                    .Model;

            var snapshot =
                new SchemaSnapshot();

            foreach (var entity in model.GetEntityTypes())
            {
                var tableMapping =
                    entity
                        .GetTableMappings()
                        .FirstOrDefault();

                if (tableMapping == null)
                    continue;

                var tableName =
                    tableMapping.Table.Name;

                if (string.IsNullOrWhiteSpace(
                        tableName))
                {
                    continue;
                }

                var schema =
                    tableMapping.Table.Schema
                    ?? "dbo";

                var table =
                    new TableSnapshot
                    {
                        Name =
                            tableName,

                        Schema =
                            schema
                    };


                foreach (var property in
                         entity.GetProperties())
                {
                    var columnMapping =
                        tableMapping.ColumnMappings
                            .FirstOrDefault(
                                x =>
                                    x.Property == property);

                    if (columnMapping == null)
                    {
                        columnMapping =
                            tableMapping.ColumnMappings
                                .FirstOrDefault(
                                    x =>
                                        x.Property.Name ==
                                        property.Name);
                    }

                    if (columnMapping == null)
                        continue;

                    var columnName =
                        columnMapping.Column.Name;

                    if (string.IsNullOrWhiteSpace(
                            columnName))
                    {
                        continue;
                    }

                    var annotations =
                        new Dictionary<string, object?>();

                    foreach (var annotation in
                             property.GetAnnotations())
                    {
                        annotations[
                            annotation.Name] =
                            annotation.Value;
                    }

                    var isIdentity =
                        property.GetValueGenerationStrategy()
                        ==
                        SqlServerValueGenerationStrategy
                            .IdentityColumn;

                    var computedColumnSql =
                        property.GetComputedColumnSql();

                    var isComputed =
                        !string.IsNullOrWhiteSpace(
                            computedColumnSql);

                    var isRowVersion =
                        property.IsConcurrencyToken
                        &&
                        property.ValueGenerated ==
                        ValueGenerated.OnAddOrUpdate;

                    var column =
                        new ColumnSnapshot
                        {
                            IsIdentity =
                                isIdentity,

                            Name =
                                columnName,

                            ClrType =
                                property.ClrType
                                    .AssemblyQualifiedName
                                ??
                                property.ClrType.FullName
                                ??
                                property.ClrType.Name,

                            ColumnType =
                                columnMapping.Column.StoreType
                                ??
                                property
                                    .GetRelationalTypeMapping()
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
                                !isIdentity
                                &&
                                !isComputed
                                &&
                                !isRowVersion
                                    ? property.GetDefaultValue()
                                    : null,

                            DefaultValueSql =
                                !isIdentity
                                &&
                                !isComputed
                                &&
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

                    table.Columns.Add(
                        column);
                }


                var primaryKey =
                    entity.FindPrimaryKey();

                if (primaryKey != null)
                {
                    var primaryKeyColumns =
                        primaryKey.Properties
                            .Select(
                                property =>
                                {
                                    var mapping =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property ==
                                                    property);

                                    if (mapping != null)
                                        return mapping.Column.Name;

                                    var fallback =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property.Name ==
                                                    property.Name);

                                    return fallback?.Column.Name
                                           ??
                                           property.Name;
                                })
                            .Where(
                                x =>
                                    !string.IsNullOrWhiteSpace(
                                        x))
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


                foreach (var index in
                         entity.GetIndexes())
                {
                    var indexName =
                        index.GetDatabaseName()
                        ??
                        index.Name;

                    if (string.IsNullOrWhiteSpace(
                            indexName))
                    {
                        continue;
                    }

                    var indexColumns =
                        index.Properties
                            .Select(
                                property =>
                                {
                                    var mapping =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property ==
                                                    property);

                                    if (mapping != null)
                                        return mapping.Column.Name;

                                    mapping =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property.Name ==
                                                    property.Name);

                                    return mapping?.Column.Name
                                           ??
                                           property.Name;
                                })
                            .Where(
                                x =>
                                    !string.IsNullOrWhiteSpace(
                                        x))
                            .ToList();

                    if (index.IsDescending != null)
                    {


                        var isDescending =
                            index.IsDescending?
                                .Select(x => x != null ? x : false)
                                .ToArray()
                            ??
                            Array.Empty<bool>();

                        table.Indexes.Add(
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
                            });
                    }

                }



                foreach (var foreignKey in
                         entity.GetForeignKeys())
                {
                    var principalEntity =
                        foreignKey.PrincipalEntityType;

                    var principalTableMapping =
                        principalEntity
                            .GetTableMappings()
                            .FirstOrDefault();

                    if (principalTableMapping == null)
                        continue;

                    var principalTableName =
                        principalTableMapping.Table.Name;

                    if (string.IsNullOrWhiteSpace(
                            principalTableName))
                    {
                        continue;
                    }

                    var principalSchema =
                        principalTableMapping.Table.Schema
                        ??
                        "dbo";

                    var constraintName =
                        foreignKey.GetConstraintName();

                    if (string.IsNullOrWhiteSpace(
                            constraintName))
                    {
                        continue;
                    }

                    var foreignKeyColumns =
                        foreignKey.Properties
                            .Select(
                                property =>
                                {
                                    var mapping =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property ==
                                                    property);

                                    if (mapping != null)
                                        return mapping.Column.Name;

                                    mapping =
                                        tableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property.Name ==
                                                    property.Name);

                                    return mapping?.Column.Name
                                           ??
                                           property.Name;
                                })
                            .Where(
                                x =>
                                    !string.IsNullOrWhiteSpace(
                                        x))
                            .ToList();

                    var principalColumns =
                        foreignKey
                            .PrincipalKey
                            .Properties
                            .Select(
                                property =>
                                {
                                    var mapping =
                                        principalTableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property ==
                                                    property);

                                    if (mapping != null)
                                        return mapping.Column.Name;

                                    mapping =
                                        principalTableMapping
                                            .ColumnMappings
                                            .FirstOrDefault(
                                                x =>
                                                    x.Property.Name ==
                                                    property.Name);

                                    return mapping?.Column.Name
                                           ??
                                           property.Name;
                                })
                            .Where(
                                x =>
                                    !string.IsNullOrWhiteSpace(
                                        x))
                            .ToList();

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

                snapshot.Tables.Add(
                    table);
            }

            return snapshot;
        }


        private async Task<SchemaSnapshot?>
            LoadSnapshotAsync(
                CancellationToken cancellationToken)
        {
            var result =
                new List<TableSnapshotSerialized>();

            var connection =
                _dbContext
                    .Database
                    .GetDbConnection();

            var shouldClose =
                connection.State !=
                ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    $"""
                    IF OBJECT_ID(
                        N'[{SnapshotSchema}].[{SnapshotTable}]',
                        'U'
                    ) IS NOT NULL
                    BEGIN
                        SELECT
                            Id,
                            TableName,
                            SchemaName,
                            JsonColumns,
                            JsonPrimaryKey,
                            JsonIndexes,
                            JsonForeignKeys,
                            UpdatedAt
                        FROM
                            [{SnapshotSchema}].[{SnapshotTable}];
                    END
                    """;

                await using var reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken);

                while (await reader.ReadAsync(
                           cancellationToken))
                {
                    result.Add(
                        new TableSnapshotSerialized
                        {
                            Id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "Id")),

                            TableName =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "TableName"))
                                ? string.Empty
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "TableName")),

                            SchemaName =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "SchemaName"))
                                ? "dbo"
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "SchemaName")),

                            JsonColumns =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "JsonColumns"))
                                ? null
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "JsonColumns")),

                            JsonPrimaryKey =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "JsonPrimaryKey"))
                                ? null
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "JsonPrimaryKey")),

                            JsonIndexes =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "JsonIndexes"))
                                ? null
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "JsonIndexes")),

                            JsonForeignKeys =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "JsonForeignKeys"))
                                ? null
                                : reader.GetString(
                                    reader.GetOrdinal(
                                        "JsonForeignKeys")),

                            UpdatedAt =
                                reader.IsDBNull(
                                    reader.GetOrdinal(
                                        "UpdatedAt"))
                                ? null
                                : reader.GetDateTime(
                                    reader.GetOrdinal(
                                        "UpdatedAt"))
                        });
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            if (result.Count == 0)
                return null;

            var snapshot =
                new SchemaSnapshot();

            foreach (var item in result)
            {
                var table =
                    new TableSnapshot
                    {
                        Name =
                            item.TableName,

                        Schema =
                            item.SchemaName,

                        Columns =
                            DeserializeList<ColumnSnapshot>(
                                item.JsonColumns),

                        PrimaryKey =
                            DeserializeObject<PrimaryKeySnapshot>(
                                item.JsonPrimaryKey),

                        Indexes =
                            DeserializeList<IndexSnapshot>(
                                item.JsonIndexes),

                        ForeignKeys =
                            DeserializeList<ForeignKeySnapshot>(
                                item.JsonForeignKeys)
                    };

                snapshot.Tables.Add(
                    table);
            }

            return snapshot;
        }


        private static List<T> DeserializeList<T>(
            string? json)
        {
            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(
                       json)
                   ??
                   new List<T>();
        }


        private static T? DeserializeObject<T>(
            string? json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(
                json);
        }



        private async Task SaveSnapshotAsync(
            SchemaSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            var qualifiedTable =
                $"[{SnapshotSchema.Replace("]", "]]")}].[{SnapshotTable.Replace("]", "]]")}]";


            var transaction =
                _dbContext.Database
                    .CurrentTransaction?
                    .GetDbTransaction();


            await _dbContext.Database.ExecuteSqlRawAsync(
                $"""
        IF OBJECT_ID(
            N'{SnapshotSchema}.{SnapshotTable}',
            'U'
        ) IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable}
            (
                Id INT IDENTITY(1,1) NOT NULL
                    CONSTRAINT PK_{SnapshotTable}
                    PRIMARY KEY,

                TableName NVARCHAR(128) NOT NULL,

                SchemaName NVARCHAR(128) NOT NULL,

                JsonColumns NVARCHAR(MAX) NULL,

                JsonPrimaryKey NVARCHAR(MAX) NULL,

                JsonIndexes NVARCHAR(MAX) NULL,

                JsonForeignKeys NVARCHAR(MAX) NULL,

                UpdatedAt DATETIME2 NOT NULL
            );

            CREATE UNIQUE INDEX
                UX_{SnapshotTable}_Schema_Table
            ON {qualifiedTable}
            (
                SchemaName,
                TableName
            );
        END
        """,
                cancellationToken);


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

            ON Target.SchemaName =
                Source.SchemaName

            AND Target.TableName =
                Source.TableName

            WHEN MATCHED THEN
                UPDATE SET
                    JsonColumns =
                        Source.JsonColumns,

                    JsonPrimaryKey =
                        Source.JsonPrimaryKey,

                    JsonIndexes =
                        Source.JsonIndexes,

                    JsonForeignKeys =
                        Source.JsonForeignKeys,

                    UpdatedAt =
                        Source.UpdatedAt

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
                new SqlParameter(
                    "@TableName",
                    item.Name),

                new SqlParameter(
                    "@SchemaName",
                    item.Schema),

                new SqlParameter(
                    "@JsonColumns",
                    jsonColumns),

                new SqlParameter(
                    "@JsonPrimaryKey",
                    jsonPrimaryKey),

                new SqlParameter(
                    "@JsonIndexes",
                    jsonIndexes),

                new SqlParameter(
                    "@JsonForeignKeys",
                    jsonForeignKeys)
                    },
                    cancellationToken);
            }


            var currentTables =
                snapshot.Tables
                    .Select(
                        x =>
                            $"{x.Schema}|{x.Name}")
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var connection =
                _dbContext.Database.GetDbConnection();

            var shouldClose =
                connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {

                var deletedIds =
                    new List<int>();



                await using (var command = connection.CreateCommand())
                {
                    if (transaction != null)
                    {
                        command.Transaction = transaction;
                    }

                    command.CommandText =
                        $"""
        SELECT
            Id,
            SchemaName,
            TableName
        FROM
            [{DbName}].[{SnapshotSchema}].[{SnapshotTable}];
        """;

                    await using var reader =
                        await command.ExecuteReaderAsync(
                            cancellationToken);

                    var idOrdinal =
                        reader.GetOrdinal("Id");

                    var schemaOrdinal =
                        reader.GetOrdinal("SchemaName");

                    var tableNameOrdinal =
                        reader.GetOrdinal("TableName");

                    while (await reader.ReadAsync(
                               cancellationToken))
                    {
                        var id =
                            reader.GetInt32(idOrdinal);

                        var schema =
                            reader.GetString(schemaOrdinal);

                        var tableName =
                            reader.GetString(tableNameOrdinal);

                        var key =
                            $"{schema}|{tableName}";

                        if (!currentTables.Contains(key))
                        {
                            deletedIds.Add(id);
                        }
                    }
                }


                foreach (var id in deletedIds)
                {
                    await using var deleteCommand =
                        connection.CreateCommand();

                    if (transaction != null)
                    {
                        deleteCommand.Transaction =
                            transaction;
                    }

                    deleteCommand.CommandText =
                        $"""
        DELETE FROM
            [{DbName}].[{SnapshotSchema}].[{SnapshotTable}]
        WHERE
            Id = @Id;
        """;

                    var parameter =
                        deleteCommand.CreateParameter();

                    parameter.ParameterName =
                        "@Id";

                    parameter.DbType =
                        DbType.Int32;

                    parameter.Value =
                        id;

                    deleteCommand.Parameters.Add(
                        parameter);

                    await deleteCommand.ExecuteNonQueryAsync(
                        cancellationToken);
                }


            }
            catch
            {
                throw;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }



        private async Task AcquireApplicationLockAsync(
                CancellationToken cancellationToken)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                """
                DECLARE @Result INT;

                EXEC @Result = sp_getapplock
                    @Resource =
                        'GenericRepository_AutoMigration',
                    @LockMode =
                        'Exclusive',
                    @LockOwner =
                        'Transaction',
                    @LockTimeout =
                        60000;

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
            return Type.GetType(
                       clrType)
                   ??
                   typeof(string);
        }
    }



}
