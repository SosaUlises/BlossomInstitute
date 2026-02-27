namespace BlossomInstitute.Domain.Entidades.Tarea
{
    public class TareaRecursoEntity
    {
        public int Id { get; set; }
        public int TareaId { get; set; }
        public TareaEntity Tarea { get; set; } = default!;

        public TipoRecursoTarea Tipo { get; set; }
        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }
    }
}
