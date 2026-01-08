using System;
using System.Collections.Generic;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data;

public partial class EcommerceDbContext : DbContext
{
    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CarritoItem> CarritoItems { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<PedidoItem> PedidoItems { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoItem>(entity =>
        {
            entity.HasIndex(e => e.ProductoId, "IX_CarritoItems_ProductoId");

            entity.HasIndex(e => e.UsuarioId, "IX_CarritoItems_UsuarioId");

            entity.HasOne(d => d.Producto).WithMany(p => p.CarritoItems).HasForeignKey(d => d.ProductoId);

            entity.HasOne(d => d.Usuario).WithMany(p => p.CarritoItems).HasForeignKey(d => d.UsuarioId);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasIndex(e => e.UsuarioId, "IX_Pedidos_UsuarioId");

            entity.Property(e => e.Total).HasPrecision(18, 2);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.HasIndex(e => e.PedidoId, "IX_PedidoItems_PedidoId");

            entity.HasIndex(e => e.ProductoId, "IX_PedidoItems_ProductoId");

            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(d => d.Pedido).WithMany(p => p.PedidoItems).HasForeignKey(d => d.PedidoId);

            entity.HasOne(d => d.Producto).WithMany(p => p.PedidoItems)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(e => e.CategoriaId, "IX_Productos_CategoriaId");

            entity.Property(e => e.Precio).HasPrecision(18, 2);

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
