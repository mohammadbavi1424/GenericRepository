using GenericRepositories.Repositories.GenericCleanArchitecture;
using GenericRepository.Context;
using GenericRepository.Contracts.Generic;
using GenericRepository.Contracts.GenericCleanArchitecture;
using GenericRepository.Repositories.Generic;
using GenericRepository.Repositories.GenericCleanArchitecture;
using GenericRepository.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenericRepository.Configurations
{
    public static class GenericDbContext
    {
        public static void AddGenericConfigurations(this IServiceCollection services,
            DbConnectionSetting setting, AssembliesSetting assemblies)
        {
            services.AddGenericDbContex(setting, assemblies);
            services.AddLifeCycles();
        }

        public static void AddGenericConfigurations(this IServiceCollection services,
            string ConnectionString, AssembliesSetting assemblies)
        {
            DbConnectionSetting setting = new()
            {
                CommandConnectionString = ConnectionString

            };
            services.AddGenericDbContex(setting, assemblies);
            services.AddLifeCycles();
        }



        private static void AddGenericDbContex(this IServiceCollection services,
            DbConnectionSetting setting, AssembliesSetting assemblies)
        {

            services.AddSingleton(new AssembliesSetting
            {
                EntitiesAssemblies = assemblies.EntitiesAssemblies,
                EntitiesConfigurationAssemblies = assemblies.EntitiesConfigurationAssemblies
            });


            if (setting != null &&
                !string.IsNullOrEmpty(setting.CommandConnectionString))
                services.AddDbContext<GenericCommandDbContext>(option =>
                {
                    option.UseSqlServer(setting.CommandConnectionString,
                        sqlOptions =>
                        {
                            sqlOptions.MigrationsAssembly(
                                typeof(GenericCommandDbContext).Assembly.FullName);
                        }
                        );

                });
            else if (setting != null &&
                !string.IsNullOrEmpty(setting.QueryConnectionString))
                services.AddDbContext<GenericCommandDbContext>(option =>
                {
                    option.UseSqlServer(setting.QueryConnectionString,
                        sqlOptions =>
                        {
                            sqlOptions.MigrationsAssembly(
                                typeof(GenericCommandDbContext).Assembly.FullName);
                        });
                });


            if (setting != null &&
                !string.IsNullOrEmpty(setting.QueryConnectionString))
                services.AddDbContext<GenericQueryDbContext>(option =>
                {
                    option.UseSqlServer(setting.QueryConnectionString,
                        sqlOptions =>
                        {
                            sqlOptions.MigrationsAssembly(
                                typeof(GenericQueryDbContext).Assembly.FullName);
                        });
                });
            else if (setting != null &&
                !string.IsNullOrEmpty(setting.CommandConnectionString))
                services.AddDbContext<GenericQueryDbContext>(option =>
                {
                    option.UseSqlServer(setting.CommandConnectionString,
                        sqlOptions =>
                        {
                            sqlOptions.MigrationsAssembly(
                                typeof(GenericQueryDbContext).Assembly.FullName);
                        });
                });


        }

        private static void AddLifeCycles(this IServiceCollection services)
        {
            #region Generic scopes lifetime
            services.AddScoped(typeof(IRepositorySyncronize<>), typeof(RepositorySyncronize<>));
            services.AddScoped(typeof(IRepositoryPublicAsyncEFCore<>), typeof(RepositoryPublicAsyncEFCore<>));
            services.AddScoped(typeof(IRepositoryPublicAsyncDapper<>), typeof(RepositoryPublicAsyncDapper<>));
            services.AddScoped(typeof(IRepositoryPublicAsyncDtoEFCore<,>), typeof(RepositoryPublicAsyncDtoEFCore<,>));
            #endregion

            #region Gemeric Scope Clean Architectur Repsitory Lifetime
            services.AddScoped(typeof(IRepositoryTransaction), typeof(RepositoryTransaction));
            services.AddScoped(typeof(IRepositoryAdd<>), typeof(RepositoryAdd<>));
            services.AddScoped(typeof(IRepositoryUpdate<>), typeof(RepositoryUpdate<>));
            services.AddScoped(typeof(IRepositoryGet<>), typeof(RepositoryGet<>));
            services.AddScoped(typeof(IRepositoryDelete<>), typeof(RepositoryDelete<>));
            #endregion

        }


    }
}
