using GenericRepositories.ParentEntities;
using GenericRepositories.Settings;
using GenericRepositories.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GenericRepositories.Context
{
    public class GenericQueryDbContext : DbContext
    {
        private readonly AssembliesSetting assembliesSetting;

        public GenericQueryDbContext(DbContextOptions<GenericQueryDbContext> options,
            AssembliesSetting assembliesSetting)
            : base(options)
        {
            this.assembliesSetting = assembliesSetting;
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var assemblyEntities in assembliesSetting.EntitiesAssemblies)
            {
                modelBuilder.RegisterAllEntities<IBaseEntity>(assemblyEntities);
            }
            foreach (var assemblyConfiguration in assembliesSetting.EntitiesConfigurationAssemblies)
            {
                modelBuilder.RegisterEntityTypeConfiguration(assemblyConfiguration);
            }

            modelBuilder.AddRestrictDeleteBehaviorConvention();
            modelBuilder.AddSequentialGuidForIdConvention();

        }
    }
}
