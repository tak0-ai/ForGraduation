using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MediaController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(string id)
    {
        var media = await _db.Medias.FindAsync(id);
        if (media == null) return NotFound();

        if (string.IsNullOrWhiteSpace(media.Url))
        {
            return NotFound();
        }

        var relativePath = media.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_env.WebRootPath ?? "wwwroot", relativePath);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        var mimeType = string.IsNullOrWhiteSpace(media.MimeType) ? "application/octet-stream" : media.MimeType;
        return PhysicalFile(physicalPath, mimeType);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("请提供文件。");
        }

        try
        {
            var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);

            var fileExt = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploads, fileName);
            await using (var fs = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(fs);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "需要登录才能上传媒体" });
            }

            var media = new Media
            {
                Url = $"/uploads/{fileName}",
                MimeType = file.ContentType ?? "application/octet-stream",
                FileSize = file.Length,
                UploaderId = userId
            };

            _db.Medias.Add(media);
            await _db.SaveChangesAsync();

            var absoluteUrl = $"{Request.Scheme}://{Request.Host}{media.Url}";
            return Ok(new { media.Id, url = absoluteUrl });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message);
        }
    }
}
