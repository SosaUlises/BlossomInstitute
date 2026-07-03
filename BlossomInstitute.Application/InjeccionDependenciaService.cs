using BlossomInstitute.Application.DataBase.Alumno.Commands.ActivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.CreateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.DesactivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAsignableByCurso;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetById;
using BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByAlumno;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByClase;
using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.ArchiveCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.CreateCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionById;
using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByAlumno;
using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByCurso;
using BlossomInstitute.Application.DataBase.Clase.Commands;
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
using BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCursoTheme;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAcademicProfile;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAlumnosByCurso;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetCursoById;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Profesor;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetPersonasAlumnoCurso;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetProfesoresByCurso;
using BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard;
using BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAlumnoDashboard;
using BlossomInstitute.Application.DataBase.Dashboard.Queries.GetProfesorDashboard;
using BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega;
using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMiEntregaByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMisEntregasByCurso;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasDetail;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega;
using BlossomInstitute.Application.DataBase.Login.Commands;
using BlossomInstitute.Application.DataBase.Password.Commands.ForgotPassword;
using BlossomInstitute.Application.DataBase.Password.Commands.ResetPassword;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Apply;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Archive;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Create;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Update;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.GetAll;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.GetById;
using BlossomInstitute.Application.DataBase.Profesor.Commands.ActivarProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Commands.CreateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Commands.DesactivarProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Commands.UpdateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAll;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAcademicSummary;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetById;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteEntregaByTarea;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Settings.Commands.ChangePassword;
using BlossomInstitute.Application.DataBase.Settings.Commands.DeleteAvatar;
using BlossomInstitute.Application.DataBase.Settings.Commands.UpdateAccount;
using BlossomInstitute.Application.DataBase.Settings.Commands.UpdateAvatar;
using BlossomInstitute.Application.DataBase.Settings.Queries.GetMyAccount;
using BlossomInstitute.Application.DataBase.Tarea.Commands.ArchivarTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById;
using BlossomInstitute.Application.Services.Export;
using BlossomInstitute.Application.Validator.Alumno;
using BlossomInstitute.Application.Validator.Asistencia;
using BlossomInstitute.Application.Validator.Calificacion;
using BlossomInstitute.Application.Validator.Curso;
using BlossomInstitute.Application.Validator.Entrega;
using BlossomInstitute.Application.Validator.Login;
using BlossomInstitute.Application.Validator.Password;
using BlossomInstitute.Application.Validator.Profesor;
using BlossomInstitute.Application.Validator.Settings;
using BlossomInstitute.Application.Validator.Tarea;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BlossomInstitute.Application
{
    public static class InjeccionDependenciaService
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
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
            services.AddTransient<IGetProfesorAcademicSummaryQuery, GetProfesorAcademicSummaryQuery>();
            services.AddTransient<IActivarProfesorCommand, ActivarProfesorCommand>();

            // Alumno
            services.AddTransient<ICreateAlumnoCommand, CreateAlumnoCommand>();
            services.AddTransient<IUpdateAlumnoCommand, UpdateAlumnoCommand>();
            services.AddTransient<IDesactivarAlumnoCommand, DesactivarAlumnoCommand>();
            services.AddTransient<IGetAllAlumnosQuery, GetAllAlumnosQuery>();
            services.AddTransient<IGetAlumnoByIdQuery, GetAlumnoByIdQuery>();
            services.AddTransient<IActivarAlumnoCommand, ActivarAlumnoCommand>();
            services.AddTransient<IGetAsignableAlumnosByCursoQuery, GetAsignableAlumnosByCursoQuery>();
            services.AddTransient<IGetAlumnoAcademicSummaryQuery, GetAlumnoAcademicSummaryQuery>();

            // Curso

            services.AddTransient<ICreateCursoCommand, CreateCursoCommand>();
            services.AddTransient<IUpdateCursoCommand, UpdateCursoCommand>();
            services.AddTransient<IUpdateCursoThemeCommand, UpdateCursoThemeCommand>();
            services.AddTransient<IDesactivateCursoCommand, DesactivateCursoCommand>();
            services.AddTransient<IActivateCursoCommand, ActivateCursoCommand>();
            services.AddTransient<IArchiveCursoCommand, ArchiveCursoCommand>();
            services.AddTransient<IGetAllCursosQuery, GetAllCursosQuery>();
            services.AddTransient<IGetCourseAcademicProfileQuery, GetCourseAcademicProfileQuery>();
            services.AddTransient<IGetCursoByIdQuery, GetCursoByIdQuery>();
            services.AddTransient<IGetMyCursosProfesorQuery, GetMyCursosProfesorQuery>();
            services.AddTransient<IGetMyCursosAlumnoQuery, GetMyCursosAlumnoQuery>();
            services.AddTransient<IGetMyCursoDetalleAlumnoQuery, GetMyCursoDetalleAlumnoQuery>();
            services.AddTransient<IGetPersonasAlumnoCursoQuery, GetPersonasAlumnoCursoQuery>();
            services.AddTransient<IAssignProfesoresToCursoCommand, AssignProfesoresToCursoCommand>();
            services.AddTransient<IRemoveProfesorFromCursoCommand, RemoveProfesorFromCursoCommand>();
            services.AddTransient<IMatricularAlumnosCommand, MatricularAlumnosCommand>();
            services.AddTransient<IRemoveAlumnoFromCursoCommand, RemoveAlumnoFromCursoCommand>();
            services.AddTransient<IGetAlumnosByCursoQuery, GetAlumnosByCursoQuery>();
            services.AddTransient<IGetProfesoresByCursoQuery, GetProfesoresByCursoQuery>();


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
            services.AddTransient<IGetTareasAlumnoByCursoQuery, GetTareasAlumnoByCursoQuery>();
            services.AddTransient<IGetTareaAlumnoByIdQuery, GetTareaAlumnoByIdQuery>();

            // Entrega
            services.AddTransient<IUpsertEntregaAlumnoCommand, UpsertEntregaAlumnoCommand>();
            services.AddTransient<ICreateFeedbackEntregaCommand, CreateFeedbackEntregaCommand>();
            services.AddTransient<IGetEntregasByTareaQuery, GetEntregasByTareaQuery>();
            services.AddTransient<IGetEntregaDetailQuery, GetEntregaDetailQuery>();
            services.AddTransient<IGetFeedbacksByEntregaQuery, GetFeedbacksByEntregaQuery>();
            services.AddTransient<IGetMiEntregaByTareaQuery, GetMiEntregaByTareaQuery>();
            services.AddTransient<IGetMisEntregasByCursoQuery, GetMisEntregasByCursoQuery>();

            // Calificaciones
            services.AddTransient<ICreateCalificacionCommand, CreateCalificacionCommand>();
            services.AddTransient<IUpdateCalificacionCommand, UpdateCalificacionCommand>();
            services.AddTransient<IArchiveCalificacionCommand, ArchiveCalificacionCommand>();
            services.AddTransient<IGetCalificacionesByCursoQuery, GetCalificacionesByCursoQuery>();
            services.AddTransient<IGetCalificacionesByAlumnoQuery, GetCalificacionesByAlumnoQuery>();
            services.AddTransient<IGetCalificacionByIdQuery, GetCalificacionByIdQuery>();


            // Plantilla Calificaciones
            services.AddTransient<ICreatePlantillaCalificacionCommand, CreatePlantillaCalificacionCommand>();
            services.AddTransient<IUpdatePlantillaCalificacionCommand, UpdatePlantillaCalificacionCommand>();
            services.AddTransient<IArchivePlantillaCalificacionCommand, ArchivePlantillaCalificacionCommand>();
            services.AddTransient<IGetAllPlantillaCalificacionesByCursoQuery, GetAllPlantillaCalificacionesByCursoQuery>();
            services.AddTransient<IGetPlantillaCalificacionByIdQuery, GetPlantillaCalificacionByIdQuery>();
            services.AddTransient<IApplyPlantillaCalificacionCommand, ApplyPlantillaCalificacionCommand>();


            // Dashboard
            services.AddTransient<IGetAlumnoDashboardQuery, GetAlumnoDashboardQuery>();
            services.AddTransient<IGetProfesorDashboardQuery, GetProfesorDashboardQuery>();
            services.AddTransient<IGetAdminDashboardQuery, GetAdminDashboardQuery>();


            // Reportes
            services.AddTransient<IGetReporteEntregasByTareaQuery, GetReporteEntregasByTareaQuery>();
            services.AddTransient<IGetReporteAsistenciasByCursoQuery, GetReporteAsistenciasByCursoQuery>();
            services.AddTransient<IGetReporteMarksByCursoAndTermQuery, GetReporteMarksByCursoAndTermQuery>();
            services.AddTransient<IGetReporteHomeworkByCursoAndTermQuery, GetReporteHomeworkByCursoAndTermQuery>();
            services.AddTransient<IGetReporteAttendanceByCursoAndTermQuery, GetReporteAttendanceByCursoAndTermQuery>();
            services.AddTransient<IGetReporteStudentSummaryByCursoAndTermQuery, GetReporteStudentSummaryByCursoAndTermQuery>();
            services.AddTransient<IGetReporteStudentMarksDetailByCursoAndTermQuery, GetReporteStudentMarksDetailByCursoAndTermQuery>();
            services.AddScoped<IReporteExportService, ReporteExportService>();

            // Settings
            services.AddTransient<IGetMyAccountSettingsQuery, GetMyAccountSettingsQuery>();
            services.AddTransient<IUpdateMyAccountSettingsCommand, UpdateMyAccountSettingsCommand>();
            services.AddTransient<IChangeMyPasswordCommand, ChangeMyPasswordCommand>();
            services.AddTransient<IUpdateMyAvatarCommand, UpdateMyAvatarCommand>();
            services.AddTransient<IDeleteMyAvatarCommand, DeleteMyAvatarCommand>();

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
            services.AddScoped<IValidator<CreateCalificacionModel>, CreateCalificacionValidator>();
            services.AddScoped<IValidator<UpdateCalificacionModel>, UpdateCalificacionValidator>();
            services.AddScoped<IValidator<UpdateMyAccountSettingsModel>, UpdateMyAccountSettingsModelValidator>();
            services.AddScoped<IValidator<ChangeMyPasswordModel>, ChangeMyPasswordModelValidator>();
            services.AddScoped<IValidator<UpdateAvatarRequest>, UpdateAvatarRequestValidator>();
            services.AddScoped<IValidator<ApplyPlantillaCalificacionModel>, ApplyPlantillaCalificacionModelValidator>();


            return services;
        }
    }
}
