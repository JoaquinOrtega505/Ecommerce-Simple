using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    public class MercadoPagoCredencialesSuperAdmin
    {
        [Key]
        public int Id { get; set; }

        // Tokens OAuth de MercadoPago (encriptados)
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? PublicKey { get; set; }

        // ID de usuario en MercadoPago
        public string? MercadoPagoUserId { get; set; }

        // Email asociado a la cuenta de MercadoPago
        public string? MercadoPagoEmail { get; set; }

        // Fecha de expiración del token
        public DateTime? TokenExpiracion { get; set; }

        // Estado de la conexión
        public bool Conectado { get; set; } = false;

        // Fechas de auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaConexion { get; set; }
        public DateTime? FechaDesconexion { get; set; }

        // Para identificar si es producción o sandbox
        public bool EsProduccion { get; set; } = false;
    }
}
