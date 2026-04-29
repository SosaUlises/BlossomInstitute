using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByCurso;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteEntregaByTarea;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm;
using BlossomInstitute.Application.Services.Export;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Reportes
{
    [ApiController]
    [Route("api/v1/reportes")]
    [Authorize(Roles = "Profesor,Administrador")]
    public class ReporteController : ControllerBase, IActionFilter
    {
        private int _userId;
        private int GetUserId() => _userId;
        private bool IsAdmin() => User.IsInRole("Administrador");

        [NonAction]
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out _userId) || _userId <= 0)
            {
                context.Result = Unauthorized(ResponseApiService.Response(
                    StatusCodes.Status401Unauthorized,
                    message: "Usuario inválido"));
            }
        }

        [NonAction]
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        [HttpGet("cursos/{cursoId:int}/tareas/{tareaId:int}/entregas")]
        public async Task<IActionResult> GetReporteEntregasByTarea(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromServices] IGetReporteEntregasByTareaQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] EstadoEntregaReporte? estado = null,
            [FromQuery] bool? pendienteCorreccion = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                tareaId,
                GetUserId(),
                IsAdmin(),
                pageNumber,
                pageSize,
                search,
                estado,
                pendienteCorreccion,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/asistencias")]
        public async Task<IActionResult> GetReporteAsistenciasByCurso(
            [FromServices] IGetReporteAsistenciasByCursoQuery query,
            [FromRoute] int cursoId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "CursoId inválido"));

            if (to < from)
                return BadRequest(ResponseApiService.Response(400, message: "Rango de fechas inválido"));

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await query.Execute(cursoId, GetUserId(), IsAdmin(), from, to, pageNumber, pageSize, search, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/calificaciones")]
        public async Task<IActionResult> GetCalificacionesByCurso(
            [FromRoute] int cursoId,
            [FromServices] IGetCalificacionesByCursoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? alumnoId = null,
            [FromQuery] int? tipo = null,
            [FromQuery] DateOnly? from = null,
            [FromQuery] DateOnly? to = null,
            [FromQuery] int? year = null,
            [FromQuery] int? term = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                GetUserId(),
                IsAdmin(),
                pageNumber,
                pageSize,
                search,
                alumnoId,
                tipo,
                from,
                to,
                year,
                term,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/marks")]
        public async Task<IActionResult> GetReporteMarksByCursoAndTerm(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteMarksByCursoAndTermQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber,
                pageSize,
                search,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/homework")]
        public async Task<IActionResult> GetReporteHomeworkByCursoAndTerm(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteHomeworkByCursoAndTermQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber,
                pageSize,
                search,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/attendance")]
        public async Task<IActionResult> GetReporteAttendanceByCursoAndTerm(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteAttendanceByCursoAndTermQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber,
                pageSize,
                search,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/alumnos/{alumnoId:int}/years/{year:int}/terms/{term:int}/summary")]
        public async Task<IActionResult> GetReporteStudentSummaryByCursoAndTerm(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteStudentSummaryByCursoAndTermQuery query,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                alumnoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/marks/export/excel")]
        public async Task<IActionResult> ExportReporteMarksByCursoAndTermExcel(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteMarksByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteMarksByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportMarksByCourseTermToExcel(
                data.Resumen,
                data.Items);

            var fileName = $"marks-course-{cursoId}-year-{year}-term-{term}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/marks/export/pdf")]
        public async Task<IActionResult> ExportReporteMarksByCursoAndTermPdf(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteMarksByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteMarksByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportMarksByCourseTermToPdf(
                data.Resumen,
                data.Items);

            var fileName = $"marks-course-{cursoId}-year-{year}-term-{term}.pdf";

            return File(bytes, "application/pdf", fileName);
        }


        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/attendance/export/excel")]
        public async Task<IActionResult> ExportReporteAttendanceByCursoAndTermExcel(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteAttendanceByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteAttendanceByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportAttendanceByCourseTermToExcel(
                data.Resumen,
                data.Items);

            var fileName = $"attendance-course-{cursoId}-year-{year}-term-{term}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/attendance/export/pdf")]
        public async Task<IActionResult> ExportReporteAttendanceByCursoAndTermPdf(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteAttendanceByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteAttendanceByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportAttendanceByCourseTermToPdf(
                data.Resumen,
                data.Items);

            var fileName = $"attendance-course-{cursoId}-year-{year}-term-{term}.pdf";

            return File(bytes, "application/pdf", fileName);
        }


        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/homework/export/excel")]
        public async Task<IActionResult> ExportReporteHomeworkByCursoAndTermExcel(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteHomeworkByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteHomeworkByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportHomeworkByCourseTermToExcel(
                data.Resumen,
                data.Items);

            var fileName = $"homework-course-{cursoId}-year-{year}-term-{term}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        [HttpGet("cursos/{cursoId:int}/years/{year:int}/terms/{term:int}/homework/export/pdf")]
        public async Task<IActionResult> ExportReporteHomeworkByCursoAndTermPdf(
            [FromRoute] int cursoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteHomeworkByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                pageNumber: 1,
                pageSize: 5000,
                search: search,
                ct: ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteHomeworkByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportHomeworkByCourseTermToPdf(
                data.Resumen,
                data.Items);

            var fileName = $"homework-course-{cursoId}-year-{year}-term-{term}.pdf";

            return File(bytes, "application/pdf", fileName);
        }


        [HttpGet("cursos/{cursoId:int}/alumnos/{alumnoId:int}/years/{year:int}/terms/{term:int}/summary/export/pdf")]
        public async Task<IActionResult> ExportReporteStudentSummaryByCursoAndTermPdf(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteStudentSummaryByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                alumnoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteStudentSummaryByCursoAndTermResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportStudentSummaryByCourseTermToPdf(data);

            var fileName = $"student-summary-course-{cursoId}-student-{alumnoId}-year-{year}-term-{term}.pdf";

            return File(bytes, "application/pdf", fileName);
        }


        [HttpGet("cursos/{cursoId:int}/alumnos/{alumnoId:int}/years/{year:int}/terms/{term:int}/marks-detail")]
        public async Task<IActionResult> GetReporteStudentMarksDetailByCursoAndTerm(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteStudentMarksDetailByCursoAndTermQuery query,
            [FromQuery] int? tipo = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                alumnoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                tipo,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/alumnos/{alumnoId:int}/years/{year:int}/terms/{term:int}/marks-detail/export/pdf")]
        public async Task<IActionResult> ExportReporteStudentMarksDetailByCursoAndTermPdf(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int year,
            [FromRoute] int term,
            [FromServices] IGetReporteStudentMarksDetailByCursoAndTermQuery query,
            [FromServices] IReporteExportService exportService,
            [FromQuery] int? tipo = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId,
                alumnoId,
                year,
                term,
                GetUserId(),
                IsAdmin(),
                tipo,
                ct);

            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(result.StatusCode, result);

            if (result.Data is not ReporteStudentMarksDetailResponseModel data)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    ResponseApiService.Response(
                        StatusCodes.Status404NotFound,
                        message: "No se encontraron datos para exportar"));
            }

            var bytes = exportService.ExportStudentAssessmentsDetailByCourseTermToPdf(data);

            var fileName = $"student-assessments-course-{cursoId}-student-{alumnoId}-year-{year}-term-{term}.pdf";

            return File(bytes, "application/pdf", fileName);
        }
    }
}

