using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using EcommerceApi.Models;

namespace EcommerceApi.Services;

/// <summary>
/// Servicio para interactuar con la API de MercadoPago
/// </summary>
public class MercadoPagoService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoService> _logger;
    private readonly string _accessToken;

    public MercadoPagoService(IConfiguration configuration, ILogger<MercadoPagoService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _accessToken = _configuration["MercadoPago:AccessToken"]
            ?? throw new InvalidOperationException("MercadoPago AccessToken no configurado");

        // Configurar el Access Token de MercadoPago
        MercadoPagoConfig.AccessToken = _accessToken;
    }

    /// <summary>
    /// Crea una preferencia de pago en MercadoPago
    /// </summary>
    public async Task<Preference> CrearPreferenciaPagoAsync(
        Pedido pedido,
        string emailComprador,
        string urlSuccess,
        string urlFailure,
        string urlPending)
    {
        try
        {
            // Crear los items de la preferencia
            var items = pedido.PedidoItems.Select(item => new PreferenceItemRequest
            {
                Id = item.ProductoId.ToString(),
                Title = item.Producto?.Nombre ?? "Producto",
                Description = $"Pedido #{pedido.Id}",
                Quantity = item.Cantidad,
                CurrencyId = "ARS",
                UnitPrice = item.PrecioUnitario
            }).ToList();

            // Crear la preferencia
            var request = new PreferenceRequest
            {
                Items = items,
                Payer = new PreferencePayerRequest
                {
                    Email = emailComprador
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = urlSuccess,
                    Failure = urlFailure,
                    Pending = urlPending
                },
                PaymentMethods = new PreferencePaymentMethodsRequest
                {
                    DefaultPaymentMethodId = "account_money",
                    ExcludedPaymentMethods = new List<PreferencePaymentMethodRequest>(),
                    ExcludedPaymentTypes = new List<PreferencePaymentTypeRequest>(),
                    Installments = 1
                },
                ExternalReference = pedido.Id.ToString(),
                NotificationUrl = $"{_configuration["AppUrl"]}/api/pagos/webhook",
                StatementDescriptor = "ECOMMERCE"
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request);

            _logger.LogInformation("Preferencia de pago creada: {PreferenceId} para pedido {PedidoId}",
                preference.Id, pedido.Id);

            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de pago para pedido {PedidoId}", pedido.Id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el estado de un pago
    /// </summary>
    public async Task<MercadoPago.Resource.Payment.Payment?> ObtenerPagoAsync(long paymentId)
    {
        try
        {
            var client = new MercadoPago.Client.Payment.PaymentClient();
            var payment = await client.GetAsync(paymentId);

            _logger.LogInformation("Pago consultado: {PaymentId}, Estado: {Status}",
                paymentId, payment?.Status);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar pago {PaymentId}", paymentId);
            return null;
        }
    }

    /// <summary>
    /// Crea una preferencia de pago para una suscripción
    /// </summary>
    public async Task<Preference> CrearPreferenciaSuscripcionAsync(
        PlanSuscripcion plan,
        Tienda tienda,
        string emailComprador,
        string urlSuccess,
        string urlFailure,
        string urlPending)
    {
        try
        {
            // Crear el item de la preferencia
            var items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Id = plan.Id.ToString(),
                    Title = $"Suscripción {plan.Nombre}",
                    Description = plan.Descripcion,
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = plan.PrecioMensual
                }
            };

            // Crear la preferencia
            var request = new PreferenceRequest
            {
                Items = items,
                Payer = new PreferencePayerRequest
                {
                    Email = emailComprador
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = urlSuccess,
                    Failure = urlFailure,
                    Pending = urlPending
                },
                PaymentMethods = new PreferencePaymentMethodsRequest
                {
                    DefaultPaymentMethodId = "account_money",
                    ExcludedPaymentMethods = new List<PreferencePaymentMethodRequest>(),
                    ExcludedPaymentTypes = new List<PreferencePaymentTypeRequest>(),
                    Installments = 1
                },
                ExternalReference = $"SUSCRIPCION_{tienda.Id}_{plan.Id}",
                NotificationUrl = $"{_configuration["AppUrl"]}/api/planes/webhook",
                StatementDescriptor = "SUSCRIPCION ECOMMERCE"
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request);

            _logger.LogInformation(
                "Preferencia de suscripción creada: {PreferenceId} para tienda {TiendaId} plan {PlanId}",
                preference.Id, tienda.Id, plan.Id);

            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al crear preferencia de suscripción para tienda {TiendaId} plan {PlanId}",
                tienda.Id, plan.Id);
            throw;
        }
    }
}
