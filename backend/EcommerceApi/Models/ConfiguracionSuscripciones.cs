using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    public class ConfiguracionSuscripciones
    {
        [Key]
        public int Id { get; set; }

        // Período de prueba
        public int DiasPrueba { get; set; } = 7;

        // Reintentos de pago antes de suspender
        public int MaxReintentosPago { get; set; } = 3;

        // Días de gracia después de suspensión antes de poder eliminar
        public int DiasGraciaSuspension { get; set; } = 3;

        // Días antes del fin del trial para enviar recordatorio
        public int DiasAvisoFinTrial { get; set; } = 2;

        // Configuración activa
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
    }
}
