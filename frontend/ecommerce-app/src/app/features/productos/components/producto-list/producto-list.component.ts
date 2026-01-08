import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductoService } from '../../../../core/services/producto.service';
import { CategoriaService } from '../../../../core/services/categoria.service';
import { CarritoService } from '../../../../core/services/carrito.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Producto, Categoria } from '../../../../shared/models';

@Component({
  selector: 'app-producto-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './producto-list.component.html',
  styleUrl: './producto-list.component.scss'
})
export class ProductoListComponent implements OnInit {
  private productoService = inject(ProductoService);
  private categoriaService = inject(CategoriaService);
  private carritoService = inject(CarritoService);
  authService = inject(AuthService);

  productos: Producto[] = [];
  productosFiltrados: Producto[] = [];
  categorias: Categoria[] = [];

  categoriaSeleccionada: number = 0;
  busqueda: string = '';
  loading = false;
  mensaje = '';

  ngOnInit(): void {
    this.cargarProductos();
    this.cargarCategorias();
  }

  cargarProductos(): void {
    this.loading = true;
    this.productoService.getProductos().subscribe({
      next: (productos) => {
        this.productos = productos.filter(p => p.activo);
        this.productosFiltrados = this.productos;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar productos:', error);
        this.loading = false;
      }
    });
  }

  cargarCategorias(): void {
    this.categoriaService.getCategorias().subscribe({
      next: (categorias) => {
        this.categorias = categorias;
      },
      error: (error) => {
        console.error('Error al cargar categorías:', error);
      }
    });
  }

  filtrarProductos(): void {
    this.productosFiltrados = this.productos.filter(p => {
      const cumpleBusqueda = p.nombre.toLowerCase().includes(this.busqueda.toLowerCase()) ||
                             p.descripcion.toLowerCase().includes(this.busqueda.toLowerCase());
      const cumpleCategoria = this.categoriaSeleccionada === 0 || p.categoriaId === this.categoriaSeleccionada;
      return cumpleBusqueda && cumpleCategoria;
    });
  }

  agregarAlCarrito(producto: Producto): void {
    if (!this.authService.isAuthenticated) {
      this.mensaje = 'Debes iniciar sesión para agregar productos al carrito';
      setTimeout(() => this.mensaje = '', 3000);
      return;
    }

    this.carritoService.agregarAlCarrito({ productoId: producto.id, cantidad: 1 }).subscribe({
      next: () => {
        this.mensaje = `${producto.nombre} agregado al carrito`;
        setTimeout(() => this.mensaje = '', 3000);
      },
      error: (error) => {
        this.mensaje = error.error?.message || 'Error al agregar al carrito';
        setTimeout(() => this.mensaje = '', 3000);
      }
    });
  }
}
