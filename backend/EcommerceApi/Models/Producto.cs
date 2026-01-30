using System;
using System.Collections.Generic;

namespace EcommerceApi.Models;

public partial class Producto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public string ImagenUrl { get; set; } = null!;

    public string? ImagenUrl2 { get; set; }

    public string? ImagenUrl3 { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CategoriaId { get; set; }

    public int TiendaId { get; set; }

    public virtual ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();

    public virtual Categoria Categoria { get; set; } = null!;

    public virtual Tienda Tienda { get; set; } = null!;

    public virtual ICollection<PedidoItem> PedidoItems { get; set; } = new List<PedidoItem>();
}
