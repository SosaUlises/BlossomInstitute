namespace BlossomInstitute.Application.DataBase.Settings.Commands.ChangePassword
{
    public class ChangeMyPasswordModel
    {
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
        public string ConfirmNewPassword { get; set; } = default!;
    }
}
