using BlossomInstitute.Application.Configuration;
using BlossomInstitute.Application.DataBase.Login.Command;
using BlossomInstitute.Application.DataBase.Password.Command.ForgotPassword;
using BlossomInstitute.Application.DataBase.Password.Command.ResetPassword;
using BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.DeleteProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetById;
using BlossomInstitute.Application.Validator.Login;
using BlossomInstitute.Application.Validator.Password;
using BlossomInstitute.Application.Validator.Profesor;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BlossomInstitute.Application
{
    public static class InjeccionDependenciaService
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MapperProfile).Assembly);

            // Login
            services.AddTransient<ILoginCommand, LoginCommand>();

            // Password
            services.AddTransient<IForgotPasswordCommand, ForgotPasswordCommand>();
            services.AddTransient<IResetPasswordCommand, ResetPasswordCommand>();

            // Profesor
            services.AddTransient<ICreateProfesorCommand, CreateProfesorCommand>();
            services.AddTransient<IUpdateProfesorCommand, UpdateProfesorCommand>();
            services.AddTransient<IDesactivarProfesorCommand, DesactivarProfesorCommand>();
            services.AddTransient<IGetAllProfesoresQuery, GetAllProfesoresQuery>();
            services.AddTransient<IGetProfesorByIdQuery, GetProfesorByIdQuery>();

            // Validators
            services.AddScoped<IValidator<LoginModel>, LoginValidator>();
            services.AddScoped<IValidator<ForgotPasswordModel>, ForgotPasswordValidator>();
            services.AddScoped<IValidator<ResetPasswordModel>, ResetPasswordValidator>();
            services.AddScoped<IValidator<CreateProfesorModel>, CreateProfesorValidator>();
            services.AddScoped<IValidator<UpdateProfesorModel>, UpdateProfesorValidator>();


            return services;
        }
    }
}
