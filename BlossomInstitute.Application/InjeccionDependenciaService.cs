using BlossomInstitute.Application.Configuration;
using BlossomInstitute.Application.DataBase.Alumno.Command.ActivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Command.CreateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Command.DesactivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Command.UpdateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetById;
using BlossomInstitute.Application.DataBase.Asistencia.Command.TomarAsistencia;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByAlumno;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByClase;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias;
using BlossomInstitute.Application.DataBase.Clase.Command;
using BlossomInstitute.Application.DataBase.Clase.Queries.GetClasesByCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.ActivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.ArchivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.AsignarAlumnos;
using BlossomInstitute.Application.DataBase.Curso.Commands.AsignarProfesores;
using BlossomInstitute.Application.DataBase.Curso.Commands.CreateCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.DesactivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.RemoveAlumno;
using BlossomInstitute.Application.DataBase.Curso.Commands.RemoveProfesores;
using BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCurso;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetCursoById;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Profesor;
using BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega;
using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMiEntregaByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMisEntregasByCurso;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasDetail;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega;
using BlossomInstitute.Application.DataBase.Entregas.Queries.ReporteEntregaByTarea;
using BlossomInstitute.Application.DataBase.Login.Command;
using BlossomInstitute.Application.DataBase.Password.Command.ForgotPassword;
using BlossomInstitute.Application.DataBase.Password.Command.ResetPassword;
using BlossomInstitute.Application.DataBase.Profesor.Command.ActivarProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.DeleteProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetById;
using BlossomInstitute.Application.DataBase.Tarea.Commands.ArchivarTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById;
using BlossomInstitute.Application.Validator.Alumno;
using BlossomInstitute.Application.Validator.Asistencia;
using BlossomInstitute.Application.Validator.Curso;
using BlossomInstitute.Application.Validator.Entrega;
using BlossomInstitute.Application.Validator.Login;
using BlossomInstitute.Application.Validator.Password;
using BlossomInstitute.Application.Validator.Profesor;
using BlossomInstitute.Application.Validator.Tarea;
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
            services.AddTransient<IActivarProfesorCommand, ActivarProfesorCommand>();

            // Alumno
            services.AddTransient<ICreateAlumnoCommand, CreateAlumnoCommand>();
            services.AddTransient<IUpdateAlumnoCommand, UpdateAlumnoCommand>();
            services.AddTransient<IDesactivarAlumnoCommand, DesactivarAlumnoCommand>();
            services.AddTransient<IGetAllAlumnosQuery, GetAllAlumnosQuery>();
            services.AddTransient<IGetAlumnoByIdQuery, GetAlumnoByIdQuery>();
            services.AddTransient<IActivarAlumnoCommand, ActivarAlumnoCommand>();

            // Curso

            services.AddTransient<ICreateCursoCommand, CreateCursoCommand>();
            services.AddTransient<IUpdateCursoCommand, UpdateCursoCommand>();
            services.AddTransient<IDesactivateCursoCommand, DesactivateCursoCommand>();
            services.AddTransient<IActivateCursoCommand, ActivateCursoCommand>();
            services.AddTransient<IArchiveCursoCommand, ArchiveCursoCommand>();
            services.AddTransient<IGetAllCursosQuery, GetAllCursosQuery>();
            services.AddTransient<IGetCursoByIdQuery, GetCursoByIdQuery>();
            services.AddTransient<IGetMyCursosProfesorQuery, GetMyCursosProfesorQuery>();
            services.AddTransient<IGetMyCursosAlumnoQuery, GetMyCursosAlumnoQuery>();
            services.AddTransient<IAssignProfesoresToCursoCommand, AssignProfesoresToCursoCommand>();
            services.AddTransient<IRemoveProfesorFromCursoCommand, RemoveProfesorFromCursoCommand>();
            services.AddTransient<IMatricularAlumnosCommand, MatricularAlumnosCommand>();
            services.AddTransient<IRemoveAlumnoFromCursoCommand, RemoveAlumnoFromCursoCommand>();

            // Asistencia
            services.AddTransient<ITomarAsistenciaCommand, TomarAsistenciaCommand>();
            services.AddTransient<IGetAsistenciasByClaseQuery, GetAsistenciasByClaseQuery>();
            services.AddTransient<IGetAsistenciasByAlumnoQuery, GetAsistenciasByAlumnoQuery>();
            services.AddTransient<IGetMisAsistenciasQuery, GetMisAsistenciasQuery>();

            // Clase
            services.AddTransient<ICancelarClaseCommand, CancelarClaseCommand>();
            services.AddTransient<IGetClasesByCursoQuery, GetClasesByCursoQuery>();

            // Tarea
            services.AddTransient<ICreateTareaCommand, CreateTareaCommand>();
            services.AddTransient<IUpdateTareaCommand, UpdateTareaCommand>();
            services.AddTransient<IArchivarTareaCommand, ArchivarTareaCommand>();
            services.AddTransient<IGetTareaByIdQuery, GetTareaByIdQuery>();
            services.AddTransient<IGetTareasByCursoQuery, GetTareasByCursoQuery>();

            // Entrega
            services.AddTransient<IUpsertEntregaAlumnoCommand, UpsertEntregaAlumnoCommand>();
            services.AddTransient<ICreateFeedbackEntregaCommand, CreateFeedbackEntregaCommand>();
            services.AddTransient<IGetEntregasByTareaQuery, GetEntregasByTareaQuery>();
            services.AddTransient<IGetEntregaDetailQuery, GetEntregaDetailQuery>();
            services.AddTransient<IGetFeedbacksByEntregaQuery, GetFeedbacksByEntregaQuery>();
            services.AddTransient<IGetMiEntregaByTareaQuery, GetMiEntregaByTareaQuery>();
            services.AddTransient<IGetMisEntregasByCursoQuery, GetMisEntregasByCursoQuery>();
            services.AddTransient<IGetReporteEntregasByTareaQuery, GetReporteEntregasByTareaQuery>();


            // Validators
            services.AddScoped<IValidator<LoginModel>, LoginValidator>();
            services.AddScoped<IValidator<ForgotPasswordModel>, ForgotPasswordValidator>();
            services.AddScoped<IValidator<ResetPasswordModel>, ResetPasswordValidator>();
            services.AddScoped<IValidator<CreateProfesorModel>, CreateProfesorValidator>();
            services.AddScoped<IValidator<UpdateProfesorModel>, UpdateProfesorValidator>();
            services.AddScoped<IValidator<CreateAlumnoModel>, CreateAlumnoValidator>();
            services.AddScoped<IValidator<UpdateAlumnoModel>, UpdateAlumnoValidator>();
            services.AddScoped<IValidator<CreateCursoModel>, CreateCursoValidator>();
            services.AddScoped<IValidator<CreateCursoHorarioModel>, CreateCursoHorarioValidator>();
            services.AddScoped<IValidator<UpdateCursoModel>, UpdateCursoValidator>();
            services.AddScoped<IValidator<UpdateCursoHorarioModel>, UpdateCursoHorarioValidator>();
            services.AddScoped<IValidator<AssignProfesoresToCursoModel>, AssignProfesoresValidator>();
            services.AddScoped<IValidator<MatricularAlumnosModel>, MatricularAlumnosValidator>();
            services.AddScoped<IValidator<TomarAsistenciaModel>, TomarAsistenciaValidator>();
            services.AddScoped<IValidator<CreateTareaModel>, CreateTareaValidator>();
            services.AddScoped<IValidator<UpdateTareaModel>, UpdateTareaValidator>();
            services.AddScoped<IValidator<UpsertEntregaAdjuntoModel>, UpsertEntregaAdjuntoValidator>();
            services.AddScoped<IValidator<UpsertEntregaAlumnoModel>, UpsertEntregaAlumnoValidator>();
            services.AddScoped<IValidator<CreateFeedbackEntregaModel>, CreateFeedbackEntregaValidator>();


            return services;
        }
    }
}
