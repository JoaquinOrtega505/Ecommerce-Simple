using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EcommerceApi.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        // Leer de variables de entorno primero, luego appsettings
        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? configuration["Cloudinary:CloudName"];
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? configuration["Cloudinary:ApiKey"];
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new InvalidOperationException("Configuración de Cloudinary incompleta. Configure CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder = "tiendas")
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("El archivo no puede estar vacío");
        }

        // Validar que sea una imagen
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Solo se permiten archivos de imagen (jpg, jpeg, png, gif, webp)");
        }

        // Validar tamaño (máximo 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("El archivo no puede superar los 5MB");
        }

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation()
                        .Quality("auto:good")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new Exception($"Error al subir imagen: {uploadResult.Error.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al subir la imagen a Cloudinary", ex);
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId))
        {
            return false;
        }

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }
        catch
        {
            return false;
        }
    }

    public string GetPublicIdFromUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return string.Empty;
        }

        try
        {
            // Extraer el public ID de la URL de Cloudinary
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath;

            // El formato es: /v{version}/{folder}/{publicId}.{extension}
            var parts = path.Split('/');
            if (parts.Length >= 3)
            {
                var fileNameWithExtension = parts[^1];
                var fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
                var folder = parts[^2];
                return $"{folder}/{fileName}";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
