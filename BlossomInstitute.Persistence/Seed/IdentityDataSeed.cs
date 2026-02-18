using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Persistence.Seed
{
    public class IdentityDataSeed
    {
        public static async Task SeedRolesAsync(IHost app)
        {
            using var scope = app.Services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioEntity>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string[] roles = { "Alumno", "Profesor", "Administrador" };


            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var res = await roleManager.CreateAsync(new IdentityRole<int>(role));
                    if (!res.Succeeded)
                    {
                        throw new Exception("No se pudo crear el rol: " +
                            string.Join(", ", res.Errors.Select(e => e.Description)));
                    }
                }
            }

            var adminEmail = config["Admin_Email"]?.Trim();
            var adminPassword = config["Admin_Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                throw new Exception("Debes configurar Admin_Email y Admin_Password como secreto");

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new UsuarioEntity
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    Nombre = "Instituto Blossom",
                    Activo = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new Exception("No se pudo crear el usuario administrador: " +
                                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }

                var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Administrador");
                if (!addRoleResult.Succeeded)
                {
                    throw new Exception("No se pudo asignar rol Administrador: " +
                                        string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }

    }
}
