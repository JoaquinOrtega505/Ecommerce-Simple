namespace EcommerceApi.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente"; // Admin o Cliente
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    // Relaciones
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
