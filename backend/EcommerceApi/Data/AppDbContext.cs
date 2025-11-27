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

        // Configurar relaciones
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
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

        modelBuilder.Entity<PedidoItem>()
            .HasOne(pi => pi.Pedido)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoItem>()
            .HasOne(pi => pi.Producto)
            .WithMany(p => p.PedidoItems)
            .HasForeignKey(pi => pi.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed de datos iniciales
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Electrónica", Descripcion = "Dispositivos electrónicos" },
            new Categoria { Id = 2, Nombre = "Ropa", Descripcion = "Prendas de vestir" },
            new Categoria { Id = 3, Nombre = "Hogar", Descripcion = "Artículos para el hogar" }
        );

        // Usuario admin por defecto (password: Admin123!)
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario 
            { 
                Id = 1, 
                Nombre = "Administrador", 
                Email = "admin@ecommerce.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Rol = "Admin",
                FechaCreacion = DateTime.UtcNow
            }
        );
    }
}
