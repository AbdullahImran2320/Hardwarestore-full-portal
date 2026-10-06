using Microsoft.AspNetCore.Http;

namespace HardwareStorePortal.API.DTOs
{
    // File uploads are wrapped in small form classes because Swagger (Swashbuckle)
    // cannot describe a bare [FromForm] IFormFile parameter. The field names stay
    // "file", "skipInvalid" and "confirm", so the frontend does not change.
    public class UploadFileForm
    {
        public IFormFile? File { get; set; }
    }

    public class ImportCommitForm
    {
        public IFormFile? File { get; set; }
        public bool SkipInvalid { get; set; }
    }

    public class RestoreForm
    {
        public IFormFile? File { get; set; }
        public string? Confirm { get; set; }
    }
}
