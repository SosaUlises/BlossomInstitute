using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Application.External;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Infraestructure.DataBase;
using BlossomInstitute.Infraestructure.Email;
using BlossomInstitute.Infraestructure.GetTokenJWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.Text;

namespace BlossomInstitute.Infraestructure
{
    public static class InjeccionDependenciaService
    {
        public static IServiceCollection AddInfraestructure(this IServiceCollection services,
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

            // JWT – Configuración completa

            var jwtKey = configuration["Jwt_Key"];
            var jwtIssuer = configuration["Jwt_Issuer"];
            var jwtAudience = configuration["Jwt_Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt_Key no está configurado.");

            if (jwtKey.Length < 32)
                throw new InvalidOperationException("Jwt_Key debe tener al menos 32 caracteres.");

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                throw new InvalidOperationException("Jwt_Issuer no está configurado.");

            if (string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("Jwt_Audience no está configurado.");
            
            // Registrar autenticacion con Bearer Token. Validamos token
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

            });

            services.AddScoped<IGetTokenJWTService, GetTokenJWTService>();

            // Cargar conf de email
            services.Configure<EmailSettings>(configuration.GetSection("Email"));

            services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            QuestPDF.Settings.License = LicenseType.Community;

            return services;
        }
    }
}
