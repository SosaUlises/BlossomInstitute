namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAlumnosByCurso
{
    public class AlumnoByCursoItemModel
    {
        public int AlumnoId { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<AlumnoByCursoQuarterAverageModel> PromediosTrimestrales { get; set; } = new();
    }

    public class AlumnoByCursoQuarterAverageModel
    {
        public int Quarter { get; set; }
        public string Label { get; set; } = default!;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal? Promedio { get; set; }
    }
}
