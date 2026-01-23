using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;

namespace EcommerceApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<CarritoItem> CarritoItems { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<PedidoItem> PedidoItems { get; set; }
    public DbSet<Tienda> Tiendas { get; set; }
    public DbSet<PlanSuscripcion> PlanesSuscripcion { get; set; }
    public DbSet<HistorialSuscripcion> HistorialSuscripciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar decimales
        modelBuilder.Entity<Producto>()
            .Property(p => p.Precio)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Pedido>()
            .Property(p => p.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PedidoItem>()
            .Property(p => p.PrecioUnitario)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PedidoItem>()
            .Property(p => p.Subtotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PlanSuscripcion>()
            .Property(p => p.PrecioMensual)
            .HasPrecision(18, 2);

        modelBuilder.Entity<HistorialSuscripcion>()
            .Property(h => h.MontoTotal)
            .HasPrecision(18, 2);

        // Configurar relaciones
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Tienda)
            .WithMany(t => t.Productos)
            .HasForeignKey(p => p.TiendaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CarritoItem>()
            .HasOne(ci => ci.Usuario)
            .WithMany()
            .HasForeignKey(ci => ci.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CarritoItem>()
            .HasOne(ci => ci.Producto)
            .WithMany(p => p.CarritoItems)
            .HasForeignKey(ci => ci.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Usuario)
            .WithMany(u => u.Pedidos)
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Tienda)
            .WithMany(t => t.Pedidos)
            .HasForeignKey(p => p.TiendaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PedidoItem>()
            .HasOne(pi => pi.Pedido)
            .WithMany(p => p.PedidoItems)
            .HasForeignKey(pi => pi.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoItem>()
            .HasOne(pi => pi.Producto)
            .WithMany(p => p.PedidoItems)
            .HasForeignKey(pi => pi.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>()
            .HasOne(c => c.Tienda)
            .WithMany(t => t.Categorias)
            .HasForeignKey(c => c.TiendaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Tienda)
            .WithMany(t => t.Usuarios)
            .HasForeignKey(u => u.TiendaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tienda>()
            .HasOne(t => t.PlanSuscripcion)
            .WithMany(p => p.Tiendas)
            .HasForeignKey(t => t.PlanSuscripcionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistorialSuscripcion>()
            .HasOne(h => h.Tienda)
            .WithMany()
            .HasForeignKey(h => h.TiendaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistorialSuscripcion>()
            .HasOne(h => h.PlanSuscripcion)
            .WithMany()
            .HasForeignKey(h => h.PlanSuscripcionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configurar índice único para subdominios
        modelBuilder.Entity<Tienda>()
            .HasIndex(t => t.Subdominio)
            .IsUnique();

        // Seed de datos iniciales
        // Planes de suscripción
        modelBuilder.Entity<PlanSuscripcion>().HasData(
            new PlanSuscripcion
            {
                Id = 1,
                Nombre = "Plan Básico",
                Descripcion = "Ideal para emprendedores que están comenzando",
                MaxProductos = 20,
                PrecioMensual = 2999.99m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            },
            new PlanSuscripcion
            {
                Id = 2,
                Nombre = "Plan Estándar",
                Descripcion = "Perfecto para negocios en crecimiento",
                MaxProductos = 30,
                PrecioMensual = 4999.99m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            },
            new PlanSuscripcion
            {
                Id = 3,
                Nombre = "Plan Profesional",
                Descripcion = "Para negocios establecidos con catálogo mediano",
                MaxProductos = 50,
                PrecioMensual = 7999.99m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            },
            new PlanSuscripcion
            {
                Id = 4,
                Nombre = "Plan Premium",
                Descripcion = "Sin límites para grandes emprendimientos",
                MaxProductos = 100,
                PrecioMensual = 12999.99m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            }
        );

        // Crear una tienda por defecto
        modelBuilder.Entity<Tienda>().HasData(
            new Tienda
            {
                Id = 1,
                Nombre = "Tienda Demo",
                Subdominio = "demo",
                Activo = true,
                MaxProductos = 100,
                EnvioHabilitado = false,
                FechaCreacion = DateTime.UtcNow,
                PlanSuscripcionId = 4 // Plan Premium
            }
        );

        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Electrónica", Descripcion = "Dispositivos electrónicos", TiendaId = 1 },
            new Categoria { Id = 2, Nombre = "Ropa", Descripcion = "Prendas de vestir", TiendaId = 1 },
            new Categoria { Id = 3, Nombre = "Hogar", Descripcion = "Artículos para el hogar", TiendaId = 1 }
        );

        // Usuario super admin por defecto (password: Admin123!)
        // TiendaId = null porque es super admin que administra todas las tiendas
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Nombre = "Super Administrador",
                Email = "admin@ecommerce.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Rol = "SuperAdmin",
                TiendaId = null,
                FechaCreacion = DateTime.UtcNow
            }
        );
    }
}
