using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega
{
    public class GetFeedbacksByEntregaQuery : IGetFeedbacksByEntregaQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetFeedbacksByEntregaQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int alumnoId,
            int profesorUserId,
            CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0 || alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var profesorAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");

            var tareaExiste = await _db.Tareas
                .AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (!tareaExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            var entrega = await _db.Entregas
                .AsNoTracking()
                .Where(e => e.TareaId == tareaId && e.AlumnoId == alumnoId)
                .Select(e => new
                {
                    e.Id
                })
                .FirstOrDefaultAsync(ct);

            if (entrega == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Entrega no encontrada");

            var feedbacks = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(f => f.EntregaId == entrega.Id)
                .OrderByDescending(f => f.FechaCorreccionUtc)
                .Select(f => new FeedbackHistoryItemModel
                {
                    FeedbackId = f.Id,
                    EsVigente = f.EsVigente,
                    Estado = f.Estado,
                    Nota = f.Nota,
                    Comentario = f.Comentario,
                    FechaCorreccionUtc = f.FechaCorreccionUtc,
                    Adjuntos = f.Adjuntos
                        .OrderBy(a => a.CreatedAtUtc)
                        .Select(a => new FeedbackAdjuntoItemModel
                        {
                            Id = a.Id,
                            Tipo = (int)a.Tipo,
                            Url = a.Url,
                            Nombre = a.Nombre,
                            StorageProvider = a.StorageProvider.HasValue ? (int)a.StorageProvider.Value : null,
                            StorageKey = a.StorageKey,
                            ContentType = a.ContentType,
                            SizeBytes = a.SizeBytes
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            var calificacionActual = await _db.Calificaciones
                .AsNoTracking()
                .Where(c =>
                    c.CursoId == cursoId &&
                    c.AlumnoId == alumnoId &&
                    c.TareaId == tareaId &&
                    c.EntregaId == entrega.Id &&
                    !c.Archivado)
                .Select(c => new
                {
                    c.Id,
                    c.Tipo,
                    c.Titulo,
                    c.Nota,
                    c.Fecha
                })
                .FirstOrDefaultAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                entregaId = entrega.Id,
                total = feedbacks.Count,
                calificacionActual,
                items = feedbacks
            });
        }
    }
}