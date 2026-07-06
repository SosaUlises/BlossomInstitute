using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.CreateAlumno
{
    public class CreateAlumnoCommand : ICreateAlumnoCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IDataBaseService _dataBaseService;
        public CreateAlumnoCommand(
            UserManager<UsuarioEntity> userManager,
            IDataBaseService dataBaseService)
        {
            _userManager = userManager;
            _dataBaseService = dataBaseService;
        }

        public async Task<BaseResponseModel> Execute(CreateAlumnoModel model)
        {
            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Datos del alumno invalidos");

            var email = model.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Email inválido");

            var usuarioExistente = await _userManager.FindByEmailAsync(email);
            if (usuarioExistente != null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: $"Ya existe un usuario con el email {email}");

            var existeDni = await _userManager.Users.AnyAsync(x => x.Dni == model.Dni);
            if (existeDni)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: $"Ya existe un usuario con el DNI {model.Dni}");

            await using var tx = await _dataBaseService.BeginTransactionAsync();

            try
            {
                var usuario = new UsuarioEntity
                {
                    UserName = email,
                    Email = email,
                    Dni = model.Dni,
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    PhoneNumber = model.Telefono?.Trim(),
                    Activo = true
                };

                var createUser = await _userManager.CreateAsync(usuario, model.Password);
                if (!createUser.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest,
                        createUser.Errors.Select(e => e.Description).ToList(),
                        "Error al crear el usuario");
                }

                var rolResult = await _userManager.AddToRoleAsync(usuario, "Alumno");
                if (!rolResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest,
                        rolResult.Errors.Select(e => e.Description).ToList(),
                        "Error al asignar el rol");
                }

                _dataBaseService.Alumnos.Add(new AlumnoEntity { Id = usuario.Id });
                await _dataBaseService.SaveAsync();

                await tx.CommitAsync();

                return ResponseApiService.Response(StatusCodes.Status201Created,
                    new { UsuarioId = usuario.Id, usuario.Email, usuario.Nombre, usuario.Apellido },
                    "Alumno creado correctamente");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
