namespace EcommerceApi.DTOs;

public class PedidoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string DireccionEnvio { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public List<PedidoItemDto> Items { get; set; } = new();
}

public class PedidoItemDto
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreatePedidoDto
{
    public string DireccionEnvio { get; set; } = string.Empty;
}

public class UpdateEstadoDto
{
    public string Estado { get; set; } = string.Empty;
}
