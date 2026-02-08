namespace EcommerceApi.DTOs
{
    public class ConfiguracionSuscripcionesDto
    {
        public int Id { get; set; }
        public int DiasPrueba { get; set; }
        public int MaxReintentosPago { get; set; }
        public int DiasGraciaSuspension { get; set; }
        public int DiasAvisoFinTrial { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }

    public class UpdateConfiguracionSuscripcionesDto
    {
        public int DiasPrueba { get; set; }
        public int MaxReintentosPago { get; set; }
        public int DiasGraciaSuspension { get; set; }
        public int DiasAvisoFinTrial { get; set; }
    }

    public class MercadoPagoCredencialesDto
    {
        public int Id { get; set; }
        public bool Conectado { get; set; }
        public string? MercadoPagoEmail { get; set; }
        public DateTime? FechaConexion { get; set; }
        public bool EsProduccion { get; set; }
        public bool TokenValido { get; set; }
    }

    public class MercadoPagoOAuthRequestDto
    {
        public required string Code { get; set; }
        public required string RedirectUri { get; set; }
    }

    public class MercadoPagoConectarManualDto
    {
        public required string AccessToken { get; set; }
        public required string PublicKey { get; set; }
        public bool EsProduccion { get; set; }
    }
}
