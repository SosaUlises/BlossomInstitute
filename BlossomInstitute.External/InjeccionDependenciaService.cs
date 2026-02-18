using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Persistence.DataBase;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlossomInstitute.External
{
    public static class InjeccionDependenciaService
    {
        public static IServiceCollection AddExternal(this IServiceCollection services,
       IConfiguration configuration)
        {
            // Conexion a PostgreSQL

            var connectionString = configuration.GetConnectionString("PostgreConnectionString");
            services.AddDbContext<DataBaseService>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IDataBaseService, DataBaseService>();


            // Identity

            services.AddIdentity<UsuarioEntity, IdentityRole<int>>(options =>
            {
                // Password
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // Lockout (anti fuerza bruta)
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
                .AddEntityFrameworkStores<DataBaseService>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}
