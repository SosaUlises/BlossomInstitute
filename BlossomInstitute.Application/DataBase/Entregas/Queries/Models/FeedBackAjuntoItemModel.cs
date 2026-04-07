namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class FeedbackAdjuntoItemModel
    {
        public int Id { get; set; }
        public int Tipo { get; set; }
        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }
        public int? StorageProvider { get; set; }
        public string? StorageKey { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
    }
}
