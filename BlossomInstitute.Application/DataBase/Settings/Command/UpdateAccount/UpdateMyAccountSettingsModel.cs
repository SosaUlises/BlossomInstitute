namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAccount
{
    public class UpdateMyAccountSettingsModel
    {
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Telefono { get; set; }
        public long Dni { get; set; }
    }
}
