using GenericRepository.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Migrations;
using GenericRepository.Context.AutoMigration;

namespace GenericRepository.Configurations
{
    public static class GenericIApplicationConfiguration
    {

        public static async Task GenericAppConfiguration(this IApplicationBuilder app)
        {

            //using (var ServiceCollection = app.())
            

            using (var scop = app.ApplicationServices.CreateScope())
            {
                //if (setting != null && 
                //    setting.QueryConnectionString != setting.CommandConnectionString)
                //{
                //scop.CreateCommandDbContextInStart();
                //scop.CreateQyeryDbContextInStart();
                //}
                //else
                //{
                //    scop.CreateCommandDbContextInStart();
                //}

                await scop.CreateCommandDbContextInStart();
                await scop.CreateQueryDbContextInStart();

            }
        }


        private static void CreateCommandDbContextInStart1(this IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<GenericCommandDbContext>();

            dbContext.Database.EnsureCreated();

            var created = dbContext.Database.EnsureCreated();

            Console.WriteLine($"EnsureCreated: {created}");

            Console.WriteLine(
                $"CanConnect: {dbContext.Database.CanConnect()}");

            Console.WriteLine(
                $"EntityCount: {dbContext.Model.GetEntityTypes().Count()}");

            var tables = dbContext.Model
    .GetEntityTypes()
    .Select(x => x.GetTableName())
    .Where(x => x != null)
    .ToList();

            foreach (var table in tables)
            {
                Console.WriteLine($"TABLE: {table}");
            }


        }

        private static void CreateQyeryDbContextInStart1(this IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<GenericQueryDbContext>();
            dbContext.Database.EnsureCreated();
            //dbContext.Database.Migrate();
        }


        private static async Task CreateCommandDbContextInStart(
    this IServiceScope scope,
    CancellationToken cancellationToken = default)
        {
            var migration =
                scope.ServiceProvider.GetRequiredService<GenericAutoMigrationCommandDb>();

            await migration.SynchronizeAsync();
        }



        private static async Task CreateQueryDbContextInStart(
    this IServiceScope scope,
    CancellationToken cancellationToken = default)
        {
            var migration =
                scope.ServiceProvider.GetRequiredService<GenericAutoMigrationQueryDb>();

            await migration.SynchronizeAsync();
        }

    }
}
