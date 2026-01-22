using System;
using System.Collections.Generic;

namespace EcommerceApi.Models;

public partial class Tienda
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Subdominio { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? BannerUrl { get; set; }

    public string? Descripcion { get; set; }

    // Contact information
    public string? TelefonoWhatsApp { get; set; }

    public string? LinkInstagram { get; set; }

    // MercadoPago credentials (encrypted)
    public string? MercadoPagoPublicKey { get; set; }

    public string? MercadoPagoAccessToken { get; set; }

    // Shipping configuration
    public bool EnvioHabilitado { get; set; }

    public string? ApiEnvioProveedor { get; set; } // "Andreani", "OCA", etc.

    public string? ApiEnvioCredenciales { get; set; } // Encrypted JSON with API credentials

    // Store limits
    public int MaxProductos { get; set; } = 100;

    // Plan de suscripción
    public int? PlanSuscripcionId { get; set; }
    public DateTime? FechaSuscripcion { get; set; }
    public DateTime? FechaVencimientoSuscripcion { get; set; }

    // Estado de la tienda: Borrador, Activa, Suspendida, Inactiva
    public string EstadoTienda { get; set; } = "Borrador";

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    // Navigation properties
    public virtual PlanSuscripcion? PlanSuscripcion { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
}
