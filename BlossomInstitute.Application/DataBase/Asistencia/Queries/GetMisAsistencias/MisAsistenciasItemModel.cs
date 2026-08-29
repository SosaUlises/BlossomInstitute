using BlossomInstitute.Domain.Entidades.Clase;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias
{
    public class MisAsistenciasItemModel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public int ClaseId { get; set; }
        public string Fecha { get; set; } = default!;
        public EstadoClase EstadoClase { get; set; }
        public EstadoAsistencia? Estado { get; set; }
        public string? DescripcionClase { get; set; }
    }
}
