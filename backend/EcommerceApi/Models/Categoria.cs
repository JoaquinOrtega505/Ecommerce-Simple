using System;
using System.Collections.Generic;

namespace EcommerceApi.Models;

public partial class Categoria
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int TiendaId { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual Tienda Tienda { get; set; } = null!;
}
