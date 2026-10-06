using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Services;

namespace HardwareStorePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class BackupController : ControllerBase
    {
        private const long MaxUploadBytes = 500L * 1024 * 1024;

        private readonly BackupService _backup;

        public BackupController(BackupService backup)
        {
            _backup = backup;
        }

        // Browser download of a consistent snapshot.
        [HttpGet("download")]
        public async Task<IActionResult> Download()
        {
            try
            {
                var temp = await _backup.CreateTempSnapshotAsync();
                var stream = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                return File(stream, "application/octet-stream", BackupService.NewBackupFileName());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "The backup could not be created." });
            }
        }

        // Writes a copy into a folder on the PC running the portal (for example a USB drive).
        [HttpPost("copy")]
        public async Task<IActionResult> Copy(CopyBackupDTO dto)
        {
            try
            {
                return Ok(await _backup.CopyToFolderAsync(dto?.FolderPath));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("list")]
        public IActionResult List()
        {
            return Ok(_backup.List());
        }

        [HttpPost("restore/validate")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> ValidateRestore([FromForm] UploadFileForm form)
        {
            var file = form.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Choose a backup (.db) file first." });

            var temp = await SaveUploadAsync(file);
            try
            {
                return Ok(_backup.ValidateBackupFile(temp));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            finally
            {
                TryDelete(temp);
            }
        }

        [HttpPost("restore")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> Restore([FromForm] RestoreForm form)
        {
            var file = form.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Choose a backup (.db) file first." });

            if (form.Confirm != "RESTORE")
                return BadRequest(new { message = "Type RESTORE to confirm." });

            var temp = await SaveUploadAsync(file);
            try
            {
                var safety = await _backup.RestoreAsync(temp);
                return Ok(new { message = "Restore complete.", safetyBackup = safety });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            finally
            {
                TryDelete(temp);
            }
        }

        private static async Task<string> SaveUploadAsync(IFormFile file)
        {
            var temp = Path.Combine(Path.GetTempPath(), "hsp-upload-" + Guid.NewGuid().ToString("N") + ".db");
            await using var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fs);
            return temp;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch
            {
                // best effort
            }
        }
    }
}
