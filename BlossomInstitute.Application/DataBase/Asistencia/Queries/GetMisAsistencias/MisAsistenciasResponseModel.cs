using System.Text.Json.Serialization;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias
{
    public class MisAsistenciasResponseModel
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("items")]
        public List<MisAsistenciasItemModel> Items { get; set; } = new();
    }
}
