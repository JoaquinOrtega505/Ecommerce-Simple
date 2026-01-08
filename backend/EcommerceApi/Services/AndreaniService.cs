using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EcommerceApi.Services;

/// <summary>
/// Servicio para integración con la API de Andreani
/// </summary>
public class AndreaniService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AndreaniService> _logger;

    private string ApiUrl => _configuration["Andreani:ApiUrl"] ?? "https://api.andreani.com/v2";
    private string Username => _configuration["Andreani:Username"] ?? "";
    private string Password => _configuration["Andreani:Password"] ?? "";
    private string ContractNumber => _configuration["Andreani:ContractNumber"] ?? "";

    public AndreaniService(HttpClient httpClient, IConfiguration configuration, ILogger<AndreaniService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el token de autenticación de Andreani
    /// </summary>
    private async Task<string> GetAuthTokenAsync()
    {
        try
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await _httpClient.PostAsync($"{ApiUrl}/login", null);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AndreaniAuthResponse>();
            return result?.Token ?? throw new Exception("No se pudo obtener el token de Andreani");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al autenticar con Andreani");
            throw;
        }
    }

    /// <summary>
    /// Crea un envío en Andreani
    /// </summary>
    public async Task<AndreaniShipmentResponse> CrearEnvioAsync(CrearEnvioRequest request)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("x-authorization-contract", ContractNumber);

            var jsonContent = JsonSerializer.Serialize(new
            {
                contrato = ContractNumber,
                origen = new
                {
                    postal = new
                    {
                        codigoPostal = request.CodigoPostalOrigen,
                        calle = request.CalleOrigen,
                        numero = request.NumeroOrigen,
                        localidad = request.LocalidadOrigen,
                        provincia = request.ProvinciaOrigen,
                        pais = "Argentina"
                    }
                },
                destino = new
                {
                    postal = new
                    {
                        codigoPostal = request.CodigoPostalDestino,
                        calle = request.CalleDestino,
                        numero = request.NumeroDestino,
                        localidad = request.LocalidadDestino,
                        provincia = request.ProvinciaDestino,
                        pais = "Argentina"
                    }
                },
                bultos = new[]
                {
                    new
                    {
                        kilos = request.PesoKg,
                        valorDeclaradoSinImpuestos = request.ValorDeclarado,
                        volumen = request.VolumenM3
                    }
                },
                remitente = new
                {
                    nombreCompleto = request.RemitenteNombre,
                    email = request.RemitenteEmail,
                    documentoTipo = "DNI",
                    documentoNumero = request.RemitenteDocumento,
                    telefonos = new[] { new { tipo = 1, numero = request.RemitenteTelefono } }
                },
                destinatario = new[]
                {
                    new
                    {
                        nombreCompleto = request.DestinatarioNombre,
                        email = request.DestinatarioEmail,
                        documentoTipo = "DNI",
                        documentoNumero = request.DestinatarioDocumento,
                        telefonos = new[] { new { tipo = 1, numero = request.DestinatarioTelefono } }
                    }
                },
                productoAEntregar = request.Descripcion,
                numeroDeOrden = request.PedidoId.ToString()
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ApiUrl}/envios", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error al crear envío en Andreani. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                throw new Exception($"Error al crear envío: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<AndreaniShipmentResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Envío creado exitosamente en Andreani. Número de seguimiento: {TrackingNumber}",
                result?.NumeroAndreani);

            return result ?? throw new Exception("Respuesta inválida de Andreani");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear envío en Andreani para pedido {PedidoId}", request.PedidoId);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el estado de un envío
    /// </summary>
    public async Task<AndreaniTrackingResponse> ObtenerSeguimientoAsync(string numeroAndreani)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{ApiUrl}/envios/{numeroAndreani}/trazas");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AndreaniTrackingResponse>();
            return result ?? throw new Exception("No se pudo obtener el seguimiento");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener seguimiento de Andreani: {NumeroAndreani}", numeroAndreani);
            throw;
        }
    }

    /// <summary>
    /// Obtiene la etiqueta de envío en PDF
    /// </summary>
    public async Task<byte[]> ObtenerEtiquetaAsync(string numeroAndreani)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{ApiUrl}/envios/{numeroAndreani}/etiquetas");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener etiqueta de Andreani: {NumeroAndreani}", numeroAndreani);
            throw;
        }
    }
}

// DTOs para Andreani
public class AndreaniAuthResponse
{
    public string Token { get; set; } = string.Empty;
}

public class CrearEnvioRequest
{
    public int PedidoId { get; set; }

    // Origen
    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string CalleOrigen { get; set; } = string.Empty;
    public string NumeroOrigen { get; set; } = string.Empty;
    public string LocalidadOrigen { get; set; } = string.Empty;
    public string ProvinciaOrigen { get; set; } = string.Empty;

    // Destino
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string CalleDestino { get; set; } = string.Empty;
    public string NumeroDestino { get; set; } = string.Empty;
    public string LocalidadDestino { get; set; } = string.Empty;
    public string ProvinciaDestino { get; set; } = string.Empty;

    // Bulto
    public decimal PesoKg { get; set; }
    public decimal VolumenM3 { get; set; }
    public decimal ValorDeclarado { get; set; }

    // Remitente
    public string RemitenteNombre { get; set; } = string.Empty;
    public string RemitenteEmail { get; set; } = string.Empty;
    public string RemitenteDocumento { get; set; } = string.Empty;
    public string RemitenteTelefono { get; set; } = string.Empty;

    // Destinatario
    public string DestinatarioNombre { get; set; } = string.Empty;
    public string DestinatarioEmail { get; set; } = string.Empty;
    public string DestinatarioDocumento { get; set; } = string.Empty;
    public string DestinatarioTelefono { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
}

public class AndreaniShipmentResponse
{
    public string NumeroAndreani { get; set; } = string.Empty;
    public List<BultoAndreani> Bultos { get; set; } = new();
}

public class BultoAndreani
{
    public string NumeroDeBulto { get; set; } = string.Empty;
}

public class AndreaniTrackingResponse
{
    public List<AndreaniEvento> Eventos { get; set; } = new();
}

public class AndreaniEvento
{
    public string Fecha { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
}
