namespace NexusVault.Api.Controllers
{
    public class UploadDocumentRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
    }
}
