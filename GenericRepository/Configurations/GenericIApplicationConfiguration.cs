using GenericRepository.AutoMigration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GenericRepository.Configurations
{
    public static class GenericIApplicationConfiguration
    {
        public static void GenericAppConfiguration(this IApplicationBuilder app)
        {
            using (var scop = app.ApplicationServices.CreateScope())
            {
                scop.CreateOrUpdateCommandDbContextInStart().Wait();
                scop.CreateOrUpdateQueryDbContextInStart().Wait();
            }
        }

        private static async Task CreateOrUpdateCommandDbContextInStart(
    this IServiceScope scope, CancellationToken cancellationToken = default)
        {
            var migration = scope.ServiceProvider
                .GetRequiredService<GenericAutoMigrationCommandDb>();

            await migration.SynchronizeAsync();
        }

        private static async Task CreateOrUpdateQueryDbContextInStart(
    this IServiceScope scope, CancellationToken cancellationToken = default)
        {
            var migration = scope.ServiceProvider
                .GetRequiredService<GenericAutoMigrationQueryDb>();

            await migration.SynchronizeAsync();
        }
    }
}
