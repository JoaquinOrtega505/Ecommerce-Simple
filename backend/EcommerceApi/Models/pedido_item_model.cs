namespace EcommerceApi.Models;

public class PedidoItem
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    
    // Relaciones
    public Pedido? Pedido { get; set; }
    public Producto? Producto { get; set; }
}
