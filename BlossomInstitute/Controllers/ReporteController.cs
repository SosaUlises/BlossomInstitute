using BlossomInstitute.Application.DataBase.Entregas.Queries.ReporteEntregaByTarea;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Authorize(Roles = "Profesor,Administrador")]
    public class ReporteController : ControllerBase
    {
        private int GetUserId() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        private bool IsAdmin() => User.IsInRole("Administrador");

        [HttpGet("cursos/{cursoId:int}/tareas{tareaId:int}/reporte-entregas")]
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
    }
}
