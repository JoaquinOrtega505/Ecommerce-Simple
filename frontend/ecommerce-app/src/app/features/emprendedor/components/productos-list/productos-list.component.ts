import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ProductoService } from '../../../../core/services/producto.service';
import { Producto } from '../../../../shared/models';

@Component({
  selector: 'app-productos-list',
  templateUrl: './productos-list.component.html',
  styleUrl: './productos-list.component.scss'
})
export class ProductosListComponent implements OnInit {
  private productoService = inject(ProductoService);
  private router = inject(Router);

  productos: Producto[] = [];
  loading = true;
  error = '';
  buscarTexto = '';
  incluirInactivos = false;

  ngOnInit(): void {
    this.cargarProductos();
  }

  cargarProductos(): void {
    this.loading = true;
    this.error = '';

    this.productoService.getMisProductos(undefined, this.buscarTexto || undefined, this.incluirInactivos).subscribe({
      next: (data) => {
        this.productos = data;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al cargar productos';
        this.loading = false;
      }
    });
  }

  buscar(): void {
    this.cargarProductos();
  }

  toggleInactivos(): void {
    this.incluirInactivos = !this.incluirInactivos;
    this.cargarProductos();
  }

  nuevoProducto(): void {
    this.router.navigate(['/emprendedor/productos/nuevo']);
  }

  editarProducto(id: number): void {
    this.router.navigate(['/emprendedor/productos/editar', id]);
  }

  eliminarProducto(id: number): void {
    if (!confirm('¿Estás seguro de que deseas eliminar este producto?')) {
      return;
    }

    this.productoService.deleteProducto(id).subscribe({
      next: () => {
        this.cargarProductos();
      },
      error: (error) => {
        alert(error.error?.message || 'Error al eliminar producto');
      }
    });
  }

  getStockClass(stock: number): string {
    if (stock === 0) return 'sin-stock';
    if (stock <= 5) return 'stock-bajo';
    return 'stock-normal';
  }
}
