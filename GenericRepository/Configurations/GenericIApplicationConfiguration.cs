using GenericRepository.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenericRepository.Configurations
{
    public static class GenericIApplicationConfiguration
    {

        public static void GenericAppConfiguration(this IApplicationBuilder app)
        {

            //using (var ServiceCollection = app.())
            

            using (var scop = app.ApplicationServices.CreateScope())
            {
                //if (setting != null && 
                //    setting.QueryConnectionString != setting.CommandConnectionString)
                //{



                    scop.CreateCommandDbContextInStart();
                    scop.CreateQyeryDbContextInStart();
                //}
                //else
                //{
                //    scop.CreateCommandDbContextInStart();
                //}
            }
        }


        private static void CreateCommandDbContextInStart(this IServiceScope scope)
        {
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<GenericCommandDbContext>();
                dbContext.Database.Migrate();
        }

        private static void CreateQyeryDbContextInStart(this IServiceScope scope)
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<GenericQueryDbContext>();
            dbContext.Database.Migrate();
        }


    }
}
