namespace BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile
{
    public class CloudinaryStorageOptions
    {
        public string CloudName { get; set; } = default!;
        public string ApiKey { get; set; } = default!;
        public string ApiSecret { get; set; } = default!;
        public string Folder { get; set; } = "blossom";
    }
}
