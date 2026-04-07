using BlossomInstitute.Domain.Entidades.Common;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.Models
{
    public class TareaRecursoItemModel
    {
        public int Id { get; set; }
        public int Tipo { get; set; }
        public string Url { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public StorageProviderType? StorageProvider { get; set; }
        public string? StorageKey { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
    }

    public class TareaByCursoItemModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public int ProfesorId { get; set; }
        public string Titulo { get; set; } = default!;
        public int Estado { get; set; }
        public DateTime? FechaEntregaUtc { get; set; }
        public bool EsAnuncio { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class TareaDetailModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public int ProfesorId { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Consigna { get; set; }
        public int Estado { get; set; }
        public DateTime? FechaEntregaUtc { get; set; }
        public bool EsAnuncio { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<TareaRecursoItemModel> Recursos { get; set; } = new();
    }

    public class TareasByCursoPagedModel
    {
        public int Total { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<TareaByCursoItemModel> Items { get; set; } = new();
    }
}



