using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMisEntregasByCurso
{
    public class GetMisEntregasByCursoQuery : IGetMisEntregasByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetMisEntregasByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, int pageNumber, int pageSize, CancellationToken ct)
        {
            if (cursoId <= 0) return ResponseApiService.Response(400, "CursoId inválido");
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var matriculado = await _db.Matriculas.AsNoTracking()
                .AnyAsync(m => m.CursoId == cursoId && m.AlumnoId == alumnoUserId, ct);
            if (!matriculado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No estás matriculado en este curso");

            // Lista tareas del curso + mi entrega (si existe) con feedback vigente
            var q = _db.Tareas.AsNoTracking()
                .Where(t => t.CursoId == cursoId)
                .Select(t => new
                {
                    t.Id,
                    t.Titulo,
                    t.FechaEntregaUtc,
                    Entrega = _db.Entregas
                        .Where(e => e.TareaId == t.Id && e.AlumnoId == alumnoUserId)
                        .Select(e => new
                        {
                            e.Id,
                            e.FechaEntregaUtc,
                            e.Estado,
                            FeedbackVigente = e.Feedbacks.Where(f => f.EsVigente)
                                .Select(f => new FeedbackVigenteModel
                                {
                                    FeedbackId = f.Id,
                                    Estado = (int)f.Estado,
                                    Nota = f.Nota,
                                    FechaCorreccionUtc = f.FechaCorreccionUtc
                                })
                                .FirstOrDefault()
                        })
                        .FirstOrDefault()
                });

            var total = await q.CountAsync(ct);

            var data = await q
                .OrderByDescending(x => x.FechaEntregaUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = data
            });
        }
    }
}
