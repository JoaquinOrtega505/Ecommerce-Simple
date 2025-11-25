namespace EcommerceApi.Models;

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    // Relación con Categoría
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    
    // Relaciones
    public ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();
    public ICollection<PedidoItem> PedidoItems { get; set; } = new List<PedidoItem>();
}
