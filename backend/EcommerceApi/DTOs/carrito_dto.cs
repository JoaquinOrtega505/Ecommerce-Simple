namespace EcommerceApi.DTOs;

public class CarritoItemDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoImagen { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}

public class CarritoDto
{
    public List<CarritoItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public int TotalItems { get; set; }
}

public class AgregarCarritoDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class ActualizarCarritoDto
{
    public int Cantidad { get; set; }
}
