using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceApi.Services;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class UploadController : ControllerBase
{
    private readonly CloudinaryService _cloudinaryService;

    public UploadController(CloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    [HttpPost("image")]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] IFormFile file, [FromForm] string? folder = "tiendas")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
            }

            var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder ?? "tiendas");

            return Ok(new UploadImageResponse
            {
                Url = imageUrl,
                Message = "Imagen subida correctamente"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al subir la imagen", details = ex.Message });
        }
    }

    [HttpDelete("image")]
    public async Task<ActionResult> DeleteImage([FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest(new { message = "URL de imagen no proporcionada" });
            }

            var publicId = _cloudinaryService.GetPublicIdFromUrl(imageUrl);
            var deleted = await _cloudinaryService.DeleteImageAsync(publicId);

            if (deleted)
            {
                return Ok(new { message = "Imagen eliminada correctamente" });
            }

            return NotFound(new { message = "No se pudo eliminar la imagen" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar la imagen", details = ex.Message });
        }
    }
}

public class UploadImageResponse
{
    public string Url { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
