using System;
using System.Collections.Generic;

namespace EcommerceApi.Models;

public partial class CarritoItem
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public DateTime FechaAgregado { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
