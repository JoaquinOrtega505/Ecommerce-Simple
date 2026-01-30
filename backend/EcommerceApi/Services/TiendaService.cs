using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services;

/// <summary>
/// Servicio para gestionar tiendas en el sistema multi-tenant
/// </summary>
public class TiendaService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TiendaService> _logger;
    private readonly EncryptionService _encryptionService;

    public TiendaService(AppDbContext context, ILogger<TiendaService> logger, EncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Obtiene todas las tiendas activas
    /// </summary>
    public async Task<List<Tienda>> ObtenerTiendasActivasAsync()
    {
        return await _context.Tiendas
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todas las tiendas (activas e inactivas)
    /// </summary>
    public async Task<List<Tienda>> ObtenerTodasLasTiendasAsync()
    {
        return await _context.Tiendas
            .OrderByDescending(t => t.FechaCreacion)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene una tienda por ID
    /// </summary>
    public async Task<Tienda?> ObtenerTiendaPorIdAsync(int id)
    {
        return await _context.Tiendas
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Obtiene una tienda por subdominio
    /// </summary>
    public async Task<Tienda?> ObtenerTiendaPorSubdominioAsync(string subdominio)
    {
        return await _context.Tiendas
            .Include(t => t.Productos)
            .Include(t => t.Categorias)
            .FirstOrDefaultAsync(t => t.Subdominio == subdominio && t.Activo);
    }

    /// <summary>
    /// Crea una nueva tienda
    /// </summary>
    public async Task<Tienda> CrearTiendaAsync(Tienda tienda)
    {
        // Validar que el subdominio no exista
        var existeSubdominio = await _context.Tiendas
            .AnyAsync(t => t.Subdominio == tienda.Subdominio);

        if (existeSubdominio)
        {
            throw new InvalidOperationException($"El subdominio '{tienda.Subdominio}' ya está en uso");
        }

        // Encriptar credenciales sensibles
        if (!string.IsNullOrEmpty(tienda.MercadoPagoAccessToken))
        {
            tienda.MercadoPagoAccessToken = _encryptionService.Encrypt(tienda.MercadoPagoAccessToken);
        }

        if (!string.IsNullOrEmpty(tienda.ApiEnvioCredenciales))
        {
            tienda.ApiEnvioCredenciales = _encryptionService.Encrypt(tienda.ApiEnvioCredenciales);
        }

        tienda.FechaCreacion = DateTime.UtcNow;
        tienda.Activo = true;

        _context.Tiendas.Add(tienda);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda creada: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);

        return tienda;
    }

    /// <summary>
    /// Actualiza una tienda existente
    /// </summary>
    public async Task<Tienda> ActualizarTiendaAsync(int id, Tienda tiendaActualizada)
    {
        var tienda = await _context.Tiendas.FindAsync(id);
        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {id} no encontrada");
        }

        // Validar que el subdominio no exista en otra tienda
        if (tienda.Subdominio != tiendaActualizada.Subdominio)
        {
            var existeSubdominio = await _context.Tiendas
                .AnyAsync(t => t.Subdominio == tiendaActualizada.Subdominio && t.Id != id);

            if (existeSubdominio)
            {
                throw new InvalidOperationException($"El subdominio '{tiendaActualizada.Subdominio}' ya está en uso");
            }
        }

        tienda.Nombre = tiendaActualizada.Nombre;
        tienda.Subdominio = tiendaActualizada.Subdominio;
        tienda.LogoUrl = tiendaActualizada.LogoUrl;
        tienda.BannerUrl = tiendaActualizada.BannerUrl;
        tienda.Descripcion = tiendaActualizada.Descripcion;
        tienda.TelefonoWhatsApp = tiendaActualizada.TelefonoWhatsApp;
        tienda.LinkInstagram = tiendaActualizada.LinkInstagram;
        tienda.MercadoPagoPublicKey = tiendaActualizada.MercadoPagoPublicKey;

        // Encriptar credenciales sensibles si se proporcionaron nuevas
        if (!string.IsNullOrEmpty(tiendaActualizada.MercadoPagoAccessToken))
        {
            tienda.MercadoPagoAccessToken = _encryptionService.Encrypt(tiendaActualizada.MercadoPagoAccessToken);
        }

        tienda.EnvioHabilitado = tiendaActualizada.EnvioHabilitado;
        tienda.ApiEnvioProveedor = tiendaActualizada.ApiEnvioProveedor;

        if (!string.IsNullOrEmpty(tiendaActualizada.ApiEnvioCredenciales))
        {
            tienda.ApiEnvioCredenciales = _encryptionService.Encrypt(tiendaActualizada.ApiEnvioCredenciales);
        }

        tienda.MaxProductos = tiendaActualizada.MaxProductos;
        tienda.Activo = tiendaActualizada.Activo;
        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda actualizada: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);

        return tienda;
    }

    /// <summary>
    /// Actualiza parcialmente una tienda (solo campos proporcionados)
    /// </summary>
    public async Task<Tienda> ActualizarTiendaParcialAsync(int id, UpdateTiendaDto dto)
    {
        var tienda = await _context.Tiendas.FindAsync(id);
        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {id} no encontrada");
        }

        // Solo actualizar campos que fueron proporcionados
        if (dto.Nombre != null)
        {
            if (dto.Nombre.Length < 3)
            {
                throw new InvalidOperationException("El nombre debe tener al menos 3 caracteres");
            }
            tienda.Nombre = dto.Nombre;
        }

        if (dto.LogoUrl != null)
        {
            tienda.LogoUrl = string.IsNullOrEmpty(dto.LogoUrl) ? null : dto.LogoUrl;
        }

        if (dto.BannerUrl != null)
        {
            tienda.BannerUrl = string.IsNullOrEmpty(dto.BannerUrl) ? null : dto.BannerUrl;
        }

        if (dto.Descripcion != null)
        {
            tienda.Descripcion = string.IsNullOrEmpty(dto.Descripcion) ? null : dto.Descripcion;
        }

        if (dto.TelefonoWhatsApp != null)
        {
            tienda.TelefonoWhatsApp = string.IsNullOrEmpty(dto.TelefonoWhatsApp) ? null : dto.TelefonoWhatsApp;
        }

        if (dto.LinkInstagram != null)
        {
            tienda.LinkInstagram = string.IsNullOrEmpty(dto.LinkInstagram) ? null : dto.LinkInstagram;
        }

        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda actualizada parcialmente: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);

        return tienda;
    }

    /// <summary>
    /// Desactiva una tienda (soft delete)
    /// </summary>
    public async Task DesactivarTiendaAsync(int id)
    {
        var tienda = await _context.Tiendas.FindAsync(id);
        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {id} no encontrada");
        }

        tienda.Activo = false;
        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda desactivada: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);
    }

    /// <summary>
    /// Activa una tienda previamente desactivada
    /// </summary>
    public async Task ActivarTiendaAsync(int id)
    {
        var tienda = await _context.Tiendas.FindAsync(id);
        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {id} no encontrada");
        }

        tienda.Activo = true;
        tienda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda activada: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);
    }

    /// <summary>
    /// Elimina una tienda permanentemente (solo si no tiene datos relacionados)
    /// </summary>
    public async Task EliminarTiendaAsync(int id)
    {
        var tienda = await _context.Tiendas
            .Include(t => t.Productos)
            .Include(t => t.Pedidos)
            .Include(t => t.Usuarios)
            .Include(t => t.Categorias)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {id} no encontrada");
        }

        // Validar que no tenga datos relacionados
        if (tienda.Productos.Any() || tienda.Pedidos.Any() || tienda.Usuarios.Any() || tienda.Categorias.Any())
        {
            throw new InvalidOperationException("No se puede eliminar la tienda porque tiene productos, pedidos, usuarios o categorías asociados. Use desactivar en su lugar.");
        }

        _context.Tiendas.Remove(tienda);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Tienda eliminada permanentemente: {TiendaNombre} (ID: {TiendaId})", tienda.Nombre, tienda.Id);
    }

    /// <summary>
    /// Obtiene estadísticas de una tienda
    /// </summary>
    public async Task<object> ObtenerEstadisticasTiendaAsync(int tiendaId)
    {
        var tienda = await _context.Tiendas.FindAsync(tiendaId);
        if (tienda == null)
        {
            throw new KeyNotFoundException($"Tienda con ID {tiendaId} no encontrada");
        }

        var totalProductos = await _context.Productos.CountAsync(p => p.TiendaId == tiendaId && p.Activo);
        var totalPedidos = await _context.Pedidos.CountAsync(p => p.TiendaId == tiendaId);
        var totalUsuarios = await _context.Usuarios.CountAsync(u => u.TiendaId == tiendaId);
        var totalCategorias = await _context.Categorias.CountAsync(c => c.TiendaId == tiendaId);
        var totalVentas = await _context.Pedidos
            .Where(p => p.TiendaId == tiendaId && p.Estado == "Completado")
            .SumAsync(p => (decimal?)p.Total) ?? 0;

        return new
        {
            TiendaId = tienda.Id,
            Nombre = tienda.Nombre,
            Subdominio = tienda.Subdominio,
            TotalProductos = totalProductos,
            MaxProductos = tienda.MaxProductos,
            TotalPedidos = totalPedidos,
            TotalUsuarios = totalUsuarios,
            TotalCategorias = totalCategorias,
            TotalVentas = totalVentas,
            EnvioHabilitado = tienda.EnvioHabilitado,
            Activo = tienda.Activo
        };
    }

    /// <summary>
    /// Verifica si una tienda puede agregar más productos
    /// </summary>
    public async Task<bool> PuedeAgregarProductoAsync(int tiendaId)
    {
        var tienda = await _context.Tiendas.FindAsync(tiendaId);
        if (tienda == null)
        {
            return false;
        }

        var totalProductos = await _context.Productos.CountAsync(p => p.TiendaId == tiendaId && p.Activo);
        return totalProductos < tienda.MaxProductos;
    }

    /// <summary>
    /// Crea una tienda para un usuario específico (onboarding de emprendedores)
    /// </summary>
    public async Task<Tienda> CrearTiendaParaUsuarioAsync(CreateTiendaDto dto, int usuarioId)
    {
        // Verificar que el usuario exista y sea Admin
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        if (usuario.Rol != "Admin")
        {
            throw new InvalidOperationException("Solo los usuarios con rol Admin pueden crear tiendas");
        }

        // Verificar que el usuario no tenga ya una tienda
        if (usuario.TiendaId != null)
        {
            throw new InvalidOperationException("El usuario ya tiene una tienda asignada");
        }

        // Validar que el subdominio no exista
        var existeSubdominio = await _context.Tiendas
            .AnyAsync(t => t.Subdominio == dto.Subdominio);

        if (existeSubdominio)
        {
            throw new InvalidOperationException($"El subdominio '{dto.Subdominio}' ya está en uso");
        }

        // Crear la tienda en estado Borrador
        var tienda = new Tienda
        {
            Nombre = dto.Nombre,
            Subdominio = dto.Subdominio,
            LogoUrl = dto.LogoUrl,
            BannerUrl = dto.BannerUrl,
            Descripcion = dto.Descripcion,
            TelefonoWhatsApp = dto.TelefonoWhatsApp,
            LinkInstagram = dto.LinkInstagram,
            EstadoTienda = "Borrador",
            MaxProductos = 10, // Plan gratuito inicial
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Tiendas.Add(tienda);
        await _context.SaveChangesAsync();

        // Asignar la tienda al usuario
        usuario.TiendaId = tienda.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tienda creada para usuario {UsuarioId}: {TiendaNombre} (ID: {TiendaId})",
            usuarioId, tienda.Nombre, tienda.Id);

        return tienda;
    }
}

/// <summary>
/// DTO para crear una tienda
/// </summary>
public class CreateTiendaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Descripcion { get; set; }
    public string? TelefonoWhatsApp { get; set; }
    public string? LinkInstagram { get; set; }
}

/// <summary>
/// DTO para actualización parcial de una tienda
/// </summary>
public class UpdateTiendaDto
{
    public string? Nombre { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Descripcion { get; set; }
    public string? TelefonoWhatsApp { get; set; }
    public string? LinkInstagram { get; set; }
}
