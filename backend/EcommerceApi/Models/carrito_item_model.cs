namespace EcommerceApi.Models;

public class CarritoItem
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;
    
    // Relaciones
    public Usuario? Usuario { get; set; }
    public Producto? Producto { get; set; }
}
