import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { TiendaService } from '../../core/services/tienda.service';
import { ProductoService } from '../../core/services/producto.service';
import { CarritoService } from '../../core/services/carrito.service';
import { AuthService } from '../../core/services/auth.service';
import { Tienda } from '../../shared/models/tienda.model';
import { Producto } from '../../shared/models/producto.model';

@Component({
  selector: 'app-tienda-publica',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './tienda-publica.component.html',
  styleUrls: ['./tienda-publica.component.scss']
})
export class TiendaPublicaComponent implements OnInit {
  private tiendaService = inject(TiendaService);
  private productoService = inject(ProductoService);
  private carritoService = inject(CarritoService);
  private route = inject(ActivatedRoute);
  authService = inject(AuthService);

  tienda: Tienda | null = null;
  productos: Producto[] = [];
  loading = true;
  errorMessage = '';
  mensaje = '';
  cantidadCarrito = 0;
  productoSeleccionado: Producto | null = null;
  mostrarModal = false;
  imagenActualIndex = 0;

  get imagenesProducto(): string[] {
    if (!this.productoSeleccionado) return [];
    return this.productoSeleccionado.imagenes || [this.productoSeleccionado.imagenUrl];
  }

  get imagenActual(): string {
    return this.imagenesProducto[this.imagenActualIndex] || 'assets/placeholder.jpg';
  }

  ngOnInit(): void {
    this.cargarTiendaPorSubdominio();
    this.cargarCantidadCarrito();
  }

  cargarTiendaPorSubdominio(): void {
    // Obtener el subdominio desde los parámetros de la ruta
    const subdominio = this.route.snapshot.paramMap.get('subdominio');

    if (!subdominio) {
      this.errorMessage = 'No se pudo determinar la tienda';
      this.loading = false;
      return;
    }

    // Cargar la tienda por subdominio
    this.tiendaService.getTiendaPorSubdominio(subdominio).subscribe({
      next: (tienda) => {
        this.tienda = tienda;
        this.cargarProductosDeTienda(tienda.id);
      },
      error: (err) => {
        console.error('Error al cargar tienda:', err);
        this.errorMessage = 'Tienda no encontrada';
        this.loading = false;
      }
    });
  }

  cargarProductosDeTienda(tiendaId: number): void {
    this.productoService.getProductosPorTienda(tiendaId).subscribe({
      next: (productos) => {
        this.productos = productos.filter(p => p.activo);
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar productos:', err);
        this.errorMessage = 'Error al cargar productos';
        this.loading = false;
      }
    });
  }

  cargarCantidadCarrito(): void {
    if (this.authService.isAuthenticated) {
      this.carritoService.getCarrito().subscribe({
        next: (items) => {
          this.cantidadCarrito = items.reduce((sum, item) => sum + item.cantidad, 0);
        },
        error: (err) => {
          console.error('Error al cargar carrito:', err);
        }
      });
    } else {
      // Cargar desde localStorage para usuarios no autenticados
      this.cantidadCarrito = this.obtenerCantidadCarritoLocal();
    }
  }

  private obtenerCarritoLocal(): any[] {
    const carrito = localStorage.getItem('carrito_anonimo');
    return carrito ? JSON.parse(carrito) : [];
  }

  private guardarCarritoLocal(carrito: any[]): void {
    localStorage.setItem('carrito_anonimo', JSON.stringify(carrito));
  }

  private obtenerCantidadCarritoLocal(): number {
    const carrito = this.obtenerCarritoLocal();
    return carrito.reduce((sum: number, item: any) => sum + item.cantidad, 0);
  }

  agregarAlCarrito(producto: Producto): void {
    if (this.authService.isAuthenticated) {
      // Usuario autenticado: guardar en BD
      this.carritoService.agregarAlCarrito({ productoId: producto.id, cantidad: 1 }).subscribe({
        next: () => {
          this.mensaje = `${producto.nombre} agregado al carrito`;
          this.cantidadCarrito++;
          setTimeout(() => this.mensaje = '', 3000);
        },
        error: (error) => {
          this.mensaje = error.error?.message || 'Error al agregar al carrito';
          setTimeout(() => this.mensaje = '', 3000);
        }
      });
    } else {
      // Usuario anónimo: guardar en localStorage
      const carrito = this.obtenerCarritoLocal();
      const itemExistente = carrito.find((item: any) => item.productoId === producto.id);

      if (itemExistente) {
        itemExistente.cantidad++;
      } else {
        carrito.push({
          productoId: producto.id,
          cantidad: 1,
          producto: {
            id: producto.id,
            nombre: producto.nombre,
            precio: producto.precio,
            imagenUrl: producto.imagenUrl,
            stock: producto.stock
          }
        });
      }

      this.guardarCarritoLocal(carrito);
      this.cantidadCarrito = this.obtenerCantidadCarritoLocal();
      this.mensaje = `${producto.nombre} agregado al carrito`;
      setTimeout(() => this.mensaje = '', 3000);
    }
  }

  verDetalle(producto: Producto): void {
    this.productoSeleccionado = producto;
    this.imagenActualIndex = 0;
    this.mostrarModal = true;
  }

  cerrarModal(): void {
    this.mostrarModal = false;
    this.productoSeleccionado = null;
    this.imagenActualIndex = 0;
  }

  anteriorImagen(): void {
    if (this.imagenActualIndex > 0) {
      this.imagenActualIndex--;
    } else {
      this.imagenActualIndex = this.imagenesProducto.length - 1;
    }
  }

  siguienteImagen(): void {
    if (this.imagenActualIndex < this.imagenesProducto.length - 1) {
      this.imagenActualIndex++;
    } else {
      this.imagenActualIndex = 0;
    }
  }

  seleccionarImagen(index: number): void {
    this.imagenActualIndex = index;
  }

  irAlCarrito(): void {
    // Redirigir al carrito sin importar si está autenticado o no
    // El carrito mostrará productos desde localStorage para usuarios anónimos
    // y desde la BD para usuarios autenticados
    window.location.href = '/carrito';
  }
}
