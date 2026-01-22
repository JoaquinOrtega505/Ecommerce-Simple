using System;
using System.Collections.Generic;

namespace EcommerceApi.Models;

public partial class Pedido
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int TiendaId { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = null!;

    public string DireccionEnvio { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public DateTime? FechaPago { get; set; }

    public DateTime? FechaDespacho { get; set; }

    public string? MetodoPago { get; set; }

    public string? TransaccionId { get; set; }

    public string? NumeroSeguimiento { get; set; }

    public string? ServicioEnvio { get; set; }

    public virtual ICollection<PedidoItem> PedidoItems { get; set; } = new List<PedidoItem>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Tienda Tienda { get; set; } = null!;
}
