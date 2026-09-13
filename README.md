Generic Repository

MHBavi.GenericRepositories is a reusable and extensible generic repository framework for modern .NET applications.

The library is designed to reduce repetitive data-access code and provide a consistent infrastructure for applications using Entity Framework Core, Dapper, AutoMapper, and SQL Server.

It can be used in traditional applications as well as applications following Clean Architecture principles.

---

Table of Contents

- "Overview" (#overview)
- "Key Features" (#key-features)
- "Architecture" (#architecture)
- "Installation" (#installation)
- "Configuration" (#configuration)
- "Assemblies Configuration" (#assemblies-configuration)
- "Database Connection Configuration" (#database-connection-configuration)
- "Using a Single Database" (#using-a-single-database)
- "Using Separate Command and Query Databases" (#using-separate-command-and-query-databases)
- "Automatic Database Migration" (#automatic-database-migration)
- "Repository Types" (#repository-types)
- "Generic EF Core Repository" (#generic-ef-core-repository)
- "Dapper Repository" (#dapper-repository)
- "DTO Repository" (#dto-repository)
- "Bulk Repository" (#bulk-repository)
- "Synchronization Repository" (#synchronization-repository)
- "Clean Architecture Repositories" (#clean-architecture-repositories)
- "Transactions" (#transactions)
- "Soft Delete" (#soft-delete)
- "Primary Key Handling" (#primary-key-handling)
- "Pagination" (#pagination)
- "Entity Configuration" (#entity-configuration)
- "Best Practices" (#best-practices)
- "Requirements" (#requirements)
- "License" (#license)

---

Overview

The purpose of "MHB.GenericRepositories" is to provide a reusable data-access infrastructure that can be shared between multiple projects.

Instead of implementing the same repository logic repeatedly in every application, the library provides generic implementations that can work with different entities and database structures.

The framework supports both:

Application
    |
    +-- Generic Repository
            |
            +-- Entity Framework Core
            |
            +-- Dapper
            |
            +-- AutoMapper
            |
            +-- SQL Server

It also supports Command/Query database separation:

                    Application
                         |
              MHB.GenericRepositories
                    /           \
                   /             \
        Command Database      Query Database
              |                    |
      GenericCommandDbContext  GenericQueryDbContext

---

Key Features

Generic Repository

Provides reusable repository implementations without requiring repository code to be rewritten for every entity.

Entity Framework Core

Supports EF Core for standard CRUD operations, queries, transactions, tracking, and other database operations.

Dapper

Provides Dapper-based data access for scenarios where lightweight and high-performance SQL execution is preferred.

Command / Query Separation

The framework can use separate database connections for read and write operations.

Clean Architecture Support

Dedicated repository interfaces and implementations are provided for applications following Clean Architecture principles.

DTO Mapping

Provides repository support for working with DTOs using AutoMapper.

Bulk Operations

Provides bulk operations for large datasets, including scenarios involving hundreds of thousands or millions of records.

Soft Delete

Supports logical deletion through the entity's deletion state rather than physically removing the record.

Transactions

Provides transaction support for operations that require atomic execution.

Automatic Migration

Database migrations can be applied automatically during application startup.

Dynamic Primary Key Support

The framework does not require a specific primary key type such as "Guid" or "int".

Primary keys are resolved through EF Core metadata.

This allows entities with:

- "int"
- "long"
- "Guid"
- "string"
- composite keys

to be supported.

---

Architecture

A typical application using the library can be structured as follows:

MyApplication
│
├── Domain
│   ├── Entities
│   └── Configurations
│
├── Application
│   ├── Services
│   └── DTOs
│
├── Infrastructure
│   └── Persistence
│
└── MHB.GenericRepositories
    ├── Context
    ├── Contracts
    ├── Repositories
    ├── Configurations
    ├── Settings
    └── Utilities

The generic repository library is intended to remain independent from application-specific Domain and Application projects.

The consuming application provides its entity and configuration assemblies through "AssembliesSetting".

---

Installation

Install the package using NuGet:

dotnet add package MHB.GenericRepositories

Or add the package manually:

<PackageReference Include="MHBavi.GenericRepositories" Version="1.2.1" />

---

Configuration

The main configuration entry point is:

AddGenericConfigurations()

The library provides three configuration overloads.

---

Using DbConnectionSetting

The recommended approach when both Command and Query connection strings are required is:

services.AddGenericConfigurations(
    new DbConnectionSetting
    {
        CommandConnectionString = configuration
            .GetConnectionString("CommandDatabase"),

        QueryConnectionString = configuration
            .GetConnectionString("QueryDatabase")
    },
    assemblies);

---

Using Separate Command and Query Connection Strings

You can also directly provide both connection strings:

services.AddGenericConfigurations(
    CommandDbConnectionString,
    QueryDbConnectionString,
    assemblies);

For example:

services.AddGenericConfigurations(
    configuration.GetConnectionString("CommandDatabase"),
    configuration.GetConnectionString("QueryDatabase"),
    assemblies);

In this configuration:

Write Operations
      |
      v
CommandConnectionString
      |
      v
GenericCommandDbContext

and:

Read Operations
      |
      v
QueryConnectionString
      |
      v
GenericQueryDbContext

---

Using a Single Database

If the application uses only one database, a single connection string can be provided:

services.AddGenericConfigurations(
    configuration.GetConnectionString("DefaultConnection"),
    assemblies);

The library configures the Command and Query DbContexts using the available connection string.

This is useful when Command and Query operations use the same SQL Server database.

---

Assemblies Configuration

The framework discovers entities and Entity Framework configurations from assemblies supplied through "AssembliesSetting".

Example:

var assemblies = new AssembliesSetting
{
    EntitiesAssemblies = new[]
    {
        typeof(User).Assembly
    },

    EntitiesConfigurationAssemblies = new[]
    {
        typeof(UserConfiguration).Assembly
    }
};

Then:

services.AddGenericConfigurations(
    configuration.GetConnectionString("DefaultConnection"),
    assemblies);

This allows the repository framework to remain independent from the application's Domain project.

---

Entity Discovery

The "GenericCommandDbContext" and "GenericQueryDbContext" use the configured assemblies to discover:

- Entities
- Entity configurations
- EF Core mappings

This means the application does not need to manually register every entity inside the generic repository framework.

---

Entity Example

A typical entity can be defined in the application:

public class User : IBaseEntity
{
    public int UserId { get; set; }

    public string Name { get; set; }

    public DateTime CreateDate { get; set; }

    public bool IsDeleted { get; set; }
}

The repository framework does not require the primary key to be named "Id".

---

Automatic Database Migration

The library provides application startup configuration through:

app.GenericAppConfiguration(app);

Example:

app.GenericAppConfiguration();

The framework creates a service scope and resolves:

GenericCommandDbContext

and:

GenericQueryDbContext

Then it executes:

Database.Migrate();

for each context.

Conceptually:

Application Startup
        |
        v
Create Service Scope
        |
        +-----------------------+
        |                       |
        v                       v
Command DbContext        Query DbContext
        |                       |
        v                       v
Database.Migrate()       Database.Migrate()

This allows pending EF Core migrations to be applied automatically when the application starts.

«Important: Automatic migration should be used carefully in production environments. In larger deployments, migrations are often better handled as a dedicated deployment step rather than automatically by every application instance.»

---

Repository Types

The package registers several repository implementations automatically.

Generic repositories

IRepositorySyncronize<TEntity>
IRepositoryPublicAsyncEFCore<TEntity>
IRepositoryPublicAsyncDapper<TEntity>
IRepositoryPublicAsyncDtoEFCore<TEntity, TDto>
IRepositoryBulk<TEntity>

Clean Architecture repositories

IRepositoryTransaction
IRepositoryAdd<TEntity>
IRepositoryUpdate<TEntity>
IRepositoryGet<TEntity>
IRepositoryDelete<TEntity>

All repository implementations are registered with Scoped lifetime.

---

Generic EF Core Repository

The EF Core repository is:

IRepositoryPublicAsyncEFCore<TEntity>

Implementation:

RepositoryPublicAsyncEFCore<TEntity>

It is intended for common EF Core data-access operations.

Example dependency injection:

public class UserService
{
    private readonly IRepositoryPublicAsyncEFCore<User> _repository;

    public UserService(
        IRepositoryPublicAsyncEFCore<User> repository)
    {
        _repository = repository;
    }
}

The repository can then be used to perform operations against the entity.

---

Dapper Repository

The Dapper repository is:

IRepositoryPublicAsyncDapper<TEntity>

Implementation:

RepositoryPublicAsyncDapper<TEntity>

Dapper can be useful when:

- SQL performance is important
- Lightweight queries are preferred
- Complex SQL queries are required
- Large read operations are involved

Example:

public class UserQueryService
{
    private readonly IRepositoryPublicAsyncDapper<User> _repository;

    public UserQueryService(
        IRepositoryPublicAsyncDapper<User> repository)
    {
        _repository = repository;
    }
}

The repository uses EF Core metadata to determine information such as:

- Table
- Schema
- Primary key
- Entity properties

This avoids hard-coding database metadata inside the repository implementation.

---

DTO Repository

For applications using DTOs, the package provides:

IRepositoryPublicAsyncDtoEFCore<TEntity, TDto>

Implementation:

RepositoryPublicAsyncDtoEFCore<TEntity, TDto>

AutoMapper is used to convert between entities and DTOs.

Example:

public class UserDto
{
    public int UserId { get; set; }

    public string Name { get; set; }
}

The repository can work with:

Entity
  |
  | AutoMapper
  v
DTO

This helps prevent repetitive mapping code in application services.

---

Bulk Repository

For large datasets:

IRepositoryBulk<TEntity>

Implementation:

RepositoryBulk<TEntity>

The bulk repository is intended for high-volume operations such as:

100,000 records
500,000 records
1,000,000+ records

Typical use cases include:

- Data migration
- Data import
- Synchronization
- Batch updates
- Batch deletion
- Large-scale data processing

Example:

await repository.BulkInsertAsync(entities);

Other supported operations include:

BulkInsert
BulkUpdate
BulkDelete
BulkSoftDelete

---

Synchronization Repository

The framework also provides:

IRepositorySyncronize<TEntity>

with:

RepositorySyncronize<TEntity>

This repository is intended for synchronization scenarios where application data needs to be synchronized with another data source.

---

Clean Architecture Support

For applications following Clean Architecture, the package provides separated repository responsibilities.

Instead of using one large repository interface, responsibilities can be separated into:

IRepositoryAdd<TEntity>
IRepositoryUpdate<TEntity>
IRepositoryGet<TEntity>
IRepositoryDelete<TEntity>
IRepositoryTransaction

This allows an application to depend only on the repository capabilities it actually needs.

For example:

public class UserService
{
    private readonly IRepositoryGet<User> _userRepository;

    public UserService(
        IRepositoryGet<User> userRepository)
    {
        _userRepository = userRepository;
    }
}

This approach follows the Interface Segregation Principle and works well with Clean Architecture.

---

Transactions

Transaction support is provided through:

IRepositoryTransaction

Example:

await transaction.BeginTransactionAsync();

try
{
    // Database operations

    await transaction.CommitTransactionAsync();
}
catch
{
    await transaction.RollbackTransactionAsync();
    throw;
}

Transactions are useful when several operations must succeed or fail as a single unit.

---

Soft Delete

The framework supports soft deletion.

Instead of physically removing a record:

DELETE FROM Users

the entity can remain in the database while its deletion state is changed.

For example:

IsDeleted = true;

The repository provides separate queryable access for normal and deleted records.

Typical concepts include:

Table
TableNoTracking

TableDeleted
TableNoTrackingDeleted

This makes it possible to keep deleted records while excluding them from normal application queries.

---

Primary Key Handling

One of the design principles of the framework is that repositories should not depend on a specific key type.

The framework does not require:

Guid Id

or:

int Id

as a universal convention.

Instead, EF Core metadata can be used to discover the configured primary key.

For example, an entity may use:

public int UserId { get; set; }

while another entity may use:

public Guid EmployeeKey { get; set; }

The repository infrastructure can work with both.

---

Composite Keys

Composite primary keys can also be represented without forcing the repository to use a single "Id" property.

For example:

modelBuilder.Entity<OrderItem>()
    .HasKey(x => new
    {
        x.OrderId,
        x.ProductId
    });

Repository methods that work with entity keys can receive key values through:

params object[]

For example:

await repository.GetAsync(cancelationToken,
    productId);

This approach allows the repository to remain independent from the concrete key structure of the entity.

---

Pagination

The framework provides support for paginated queries.

The general query flow is:

Filter
   |
   v
Sort
   |
   v
Skip
   |
   v
Take
   |
   v
Result

Example concept:

var result = await repository.GetListAsync(
    filter,cancelationToken);

Pagination is especially important when working with large tables because retrieving an entire table can cause unnecessary memory consumption and database load.

---

Entity Configuration

Entity configurations can be maintained separately from entities.

Example:

public class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.Name)
            .HasMaxLength(200);
    }
}

The configuration assembly is then supplied through:

AssembliesSetting

This allows the generic DbContexts to discover the application's EF Core configurations automatically.

---

Dependency Injection

The package automatically registers its repositories using the Scoped lifetime.

Equivalent registrations include:

services.AddScoped(
    typeof(IRepositoryPublicAsyncEFCore<>),
    typeof(RepositoryPublicAsyncEFCore<>));

and:

services.AddScoped(
    typeof(IRepositoryPublicAsyncDapper<>),
    typeof(RepositoryPublicAsyncDapper<>));

Bulk repositories:

services.AddScoped(
    typeof(IRepositoryBulk<>),
    typeof(RepositoryBulk<>));

Clean Architecture repositories:

services.AddScoped(
    typeof(IRepositoryAdd<>),
    typeof(RepositoryAdd<>));

services.AddScoped(
    typeof(IRepositoryUpdate<>),
    typeof(RepositoryUpdate<>));

services.AddScoped(
    typeof(IRepositoryGet<>),
    typeof(RepositoryGet<>));

services.AddScoped(
    typeof(IRepositoryDelete<>),
    typeof(RepositoryDelete<>));

The application therefore does not need to register every generic repository manually.

---

Complete ASP.NET Core Example

A basic application configuration can look like this:

var builder = WebApplication.CreateBuilder(args);

var assemblies = new AssembliesSetting
{
    EntitiesAssemblies = new[]
    {
        typeof(User).Assembly
    },

    EntitiesConfigurationAssemblies = new[]
    {
        typeof(UserConfiguration).Assembly
    }
};

builder.Services.AddGenericConfigurations(
    builder.Configuration
        .GetConnectionString("DefaultConnection"),
    assemblies);

var app = builder.Build();

app.GenericAppConfiguration();

app.Run();

---

Command / Query Example

For applications using separate databases:

var assemblies = new AssembliesSetting
{
    EntitiesAssemblies = new[]
    {
        typeof(User).Assembly
    },

    EntitiesConfigurationAssemblies = new[]
    {
        typeof(UserConfiguration).Assembly
    }
};

builder.Services.AddGenericConfigurations(
    builder.Configuration
        .GetConnectionString("CommandDatabase"),

    builder.Configuration
        .GetConnectionString("QueryDatabase"),

    assemblies);

The architecture becomes:

                   Application
                       |
              Generic Repository
                  /          \
                 /            \
              Write          Read
                |              |
                v              v
        Command Database   Query Database

This configuration can be useful in systems that require independent optimization of read and write workloads.

---

Recommended Usage

A recommended approach is to use:

EF Core

For:

- Standard CRUD
- Entity tracking
- Complex LINQ queries
- Transactions
- Domain-oriented data access

Dapper

For:

- High-performance queries
- Reporting
- Complex SQL
- Read-heavy workloads

Bulk Repository

For:

- Large imports
- Large updates
- Large deletes
- Data synchronization

Clean Architecture Repositories

For:

- Applications following Clean Architecture
- Separation of repository responsibilities
- Better dependency control

---

Design Principles

The framework is designed around several principles:

Reusability

Repository implementations should not be duplicated across projects.

Separation of Concerns

Data access infrastructure is separated from application business logic.

Database Abstraction

The application should not need to know the internal implementation details of the repository.

EF Metadata Driven Design

Database metadata such as primary keys, schemas, tables, and properties can be resolved dynamically through EF Core metadata.

Scalability

The framework provides different data-access strategies for different workloads.

Clean Architecture Compatibility

Repository responsibilities can be separated into smaller interfaces.

---

Requirements

The current package targets:

.NET 8,9,10

Main technologies:

Entity Framework Core 8,9,10
Dapper
AutoMapper
SQL Server
Microsoft.Extensions.DependencyInjection

---

Important Notes

Database Provider

The current configuration uses:

UseSqlServer(...)

Therefore, SQL Server is the primary supported relational database provider.

Automatic Migration

Calling:

Database.Migrate();

at application startup applies pending migrations.

For production environments with multiple application instances, consider handling migrations through a dedicated deployment process.

Repository Lifetime

Repositories are registered as:

Scoped

This is appropriate for typical ASP.NET Core request-based database operations.

---

Example Project Structure

A recommended application structure:

MyProject
│
├── Domain
│   ├── Entities
│   │   ├── User.cs
│   │   └── Order.cs
│   │
│   └── Configurations
│       ├── UserConfiguration.cs
│       └── OrderConfiguration.cs
│
├── Application
│   ├── DTOs
│   ├── Services
│   └── Interfaces
│
├── Infrastructure
│   └── Persistence
│
└── API
    ├── Controllers
    └── Program.cs

The generic repository infrastructure remains reusable and independent of these application-specific projects.

---

Summary

"MHB.GenericRepositories" provides a reusable repository infrastructure for .NET applications with support for:

                 MHB.GenericRepositories
                           |
        +------------------+------------------+
        |                  |                  |
     EF Core            Dapper            AutoMapper
        |                  |                  |
        +------------------+------------------+
                           |
                  Generic Repositories
                           |
        +------------------+------------------+
        |                  |                  |
      CRUD             Transactions       Bulk Operations
        |
        +------------------+
        |
    Command / Query Separation
        |
        +------------------+
        |
    Clean Architecture

The goal is simple:

«Write the repository infrastructure once and reuse it across multiple .NET projects.»

---

Author

MohammadHossein Bavi

Package:

"MHB.GenericRepositories"

Target Framework:

Dot NET Core 8,9,10

---

License

Add your project license information here.

For example:

MIT License

