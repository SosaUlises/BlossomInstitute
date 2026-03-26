namespace BlossomInstitute.Application.DataBase.Settings.Queries.GetMyAccount
{
    public class GetMyAccountSettingsModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Telefono { get; set; }
        public long Dni { get; set; }

        public bool Activo { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}

