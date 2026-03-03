namespace BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno
{
    public class UpsertEntregaAlumnoModel
    {
        public string? Texto { get; set; }
        public List<UpsertEntregaAdjuntoModel> Adjuntos { get; set; } = new();
    }

}
