# MHB.GenericRepositories

A reusable and extensible Generic Repository Framework for .NET applications.

`MHBavi.GenericRepositories` provides a complete repository infrastructure based on **Entity Framework Core**, **Dapper**, and **AutoMapper** to reduce repetitive data access code and provide a clean, scalable approach for enterprise applications.

The framework is designed for modern .NET applications that require:

* Generic repository patterns
* Clean Architecture compatibility
* Command and Query Separation (CQRS)
* EF Core and Dapper integration
* Transaction management
* Soft delete support
* Pagination
* Bulk operations
* Concurrency handling

---

# Features

## Entity Framework Core Repository

The package provides a generic EF Core repository implementation with support for:

* CRUD operations
* Expression-based filtering
* Tracking and NoTracking queries
* Soft delete queries
* Pagination
* Transactions
* Optimistic concurrency

Example:

```csharp
public interface IRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    Task<TEntity?> GetByIdAsync(params object[] id);

    Task<IEnumerable<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> filter);

    Task AddAsync(TEntity entity);

    Task UpdateAsync(TEntity entity);

    Task DeleteAsync(TEntity entity);
}
```

---

# Dapper Repository

For high-performance read and write operations, the framework provides a Dapper-based repository.

Features:

* Dynamic table mapping
* Automatic schema detection
* Primary key discovery using EF metadata
* SQL injection protection
* Query optimization

Example:

```csharp
var users = await repository
    .GetListAsync<User>();
```

The repository automatically resolves:

* Database schema
* Table name
* Primary key
* Entity properties

---

# Command / Query Database Separation

The framework supports separate database contexts:

```
Application
    |
    |
GenericRepositories
    |
    +---- GenericCommandDbContext
    |
    +---- GenericQueryDbContext
```

Benefits:

* Better scalability
* Read/write separation
* Database optimization
* CQRS-friendly architecture

Example configuration:

```csharp
services.AddGenericDbContex(
    configuration,
    assembliesSetting);
```

---

# Base Entity

All entities should implement `IBaseEntity`.

Example:

```csharp
public class User : IBaseEntity<Guid,DateTime> // yo can set the tiks of time by set the long for datetime
{
    public Guid Id { get; set; }

    public DateTime CreateDate { get; set; }

    public Guid? CreateUserId { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool IsDeleted { get; set; }

    public byte[] RowVersion { get; set; }
}
```

The base entity provides:

* Audit fields
* Soft delete support
* Concurrency control

---

# Soft Delete

The repository supports soft delete without physically removing data.

Instead of:

```sql
DELETE FROM Users
```

The framework updates:

```sql
IsDeleted = 1
```

Available queries:

```csharp
Table

TableNoTracking

TableDeleted

TableNoTrackingDeleted
```

---

# Pagination

Generic pagination support is available.

Example:

```csharp
var result =
await repository.GetListAsync(
    new GridData<User>
    {
        PageNumber = 1,
        PageSize = 20
    });
```

The repository handles:

* Filtering
* Sorting
* Skip
* Take

---

# Transaction Management

Transactions are supported in EF Core repositories.

Example:

```csharp
await repository.BeginTransactionAsync();

try
{
    await repository.AddAsync(order);

    await repository.UpdateAsync(customer);

    await repository.CommitTransactionAsync();
}
catch
{
    await repository.RollbackTransactionAsync();
}
```

---

# AutoMapper DTO Repository

The package includes DTO conversion support.

Example:

```csharp
var users =
await repository
.GetListAsync<UserDto>();
```

The repository automatically handles:

Entity:

```
User
```

to:

```
UserDto
```

using AutoMapper.

---

# Bulk Operations

For large datasets, bulk operations are supported.

Designed for scenarios such as:

* Importing millions of records
* Batch updates
* Batch deletes

Example:

```csharp
await bulkRepository
    .BulkInsertAsync(users);


await bulkRepository
    .BulkUpdateAsync(users);
```

---

# Database Runtime Migration

The framework supports database initialization during application startup.

Example:

```csharp
using(var scope =
app.Services.CreateScope())
{
    var context =
    scope.ServiceProvider
    .GetRequiredService<AppDbContext>();

    await context.Database
    .MigrateAsync();
}
```

This allows:

* Automatic database creation
* Automatic migration execution
* Easier deployment

---

# Installation

Install using NuGet:

```
dotnet add package MHB.GenericRepositories
```

Or add manually:

```xml
<PackageReference Include="MHB.GenericRepositories"
                  Version="1.0.0" />
```

---

# Required Packages

The framework is built on:

* .NET 8
* Entity Framework Core 8
* Dapper
* AutoMapper

---

# Recommended Architecture

A recommended project structure:

```
src

 ├── Domain

 │     └── Entities


 ├── Application

 │     └── Services


 ├── Infrastructure

 │     └── Persistence


 └── GenericRepositories

       ├── Context

       ├── Repository

       ├── Contracts

       └── Utilities
```

---

# Why MHB.GenericRepositories?

Many enterprise projects contain repeated repository code.

This library attempts to solve this problem by providing:

* Less duplicate code
* Faster project development
* Consistent data access layer
* Better maintainability
* Enterprise-ready repository infrastructure

---

# Roadmap

Future improvements:

* Full CQRS pipeline integration
* Specification Pattern support
* Advanced bulk operations
* Multi database provider support
* MongoDB repository implementation
* Distributed transaction support

---

# Author

Created by:

**MohammadHossein Bavi**

Package:

`MHB.GenericRepositories`

Target Framework:

`.NET 8`
