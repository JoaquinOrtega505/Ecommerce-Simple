import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { CarritoService } from '../../../../core/services/carrito.service';
import { AuthService } from '../../../../core/services/auth.service';
import { CarritoItem } from '../../../../shared/models';

@Component({
  selector: 'app-carrito',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './carrito.component.html',
  styleUrl: './carrito.component.scss'
})
export class CarritoComponent implements OnInit {
  private carritoService = inject(CarritoService);
  private authService = inject(AuthService);
  private router = inject(Router);

  items: CarritoItem[] = [];
  loading = false;
  mensaje = '';
  esUsuarioAnonimo = false;

  ngOnInit(): void {
    this.esUsuarioAnonimo = !this.authService.isAuthenticated;
    this.cargarCarrito();

    // Suscribirse a los cambios del carrito en tiempo real solo si está autenticado
    if (this.authService.isAuthenticated) {
      this.carritoService.carritoItems$.subscribe({
        next: (items) => {
          this.items = Array.isArray(items) ? items : [];
        }
      });
    }
  }

  cargarCarrito(): void {
    this.loading = true;

    if (this.authService.isAuthenticated) {
      // Usuario autenticado: cargar desde BD
      this.carritoService.getCarrito().subscribe({
        next: (items) => {
          this.items = Array.isArray(items) ? items : [];
          this.loading = false;
        },
        error: (error) => {
          console.error('Error al cargar carrito:', error);
          this.items = [];
          this.loading = false;
        }
      });
    } else {
      // Usuario anónimo: cargar desde localStorage
      this.items = this.obtenerCarritoLocal();
      this.loading = false;
    }
  }

  private obtenerCarritoLocal(): CarritoItem[] {
    const carrito = localStorage.getItem('carrito_anonimo');
    if (!carrito) return [];

    try {
      const items = JSON.parse(carrito);
      return items.map((item: any) => ({
        id: item.productoId, // Usamos productoId como id temporal
        productoId: item.productoId,
        cantidad: item.cantidad,
        subtotal: item.producto.precio * item.cantidad,
        producto: item.producto
      }));
    } catch {
      return [];
    }
  }

  private guardarCarritoLocal(items: CarritoItem[]): void {
    const carrito = items.map(item => ({
      productoId: item.productoId,
      cantidad: item.cantidad,
      producto: item.producto
    }));
    localStorage.setItem('carrito_anonimo', JSON.stringify(carrito));
  }

  get total(): number {
    return this.items.reduce((sum, item) => sum + item.subtotal, 0);
  }

  get cantidadTotal(): number {
    return this.items.reduce((sum, item) => sum + item.cantidad, 0);
  }

  actualizarCantidad(item: CarritoItem, nuevaCantidad: number): void {
    if (nuevaCantidad < 1) return;

    if (this.authService.isAuthenticated) {
      // Usuario autenticado: actualizar en BD
      this.carritoService.actualizarCantidad(item.id, { cantidad: nuevaCantidad }).subscribe({
        next: () => {
          this.cargarCarrito();
        },
        error: (error) => {
          this.mensaje = error.error?.message || 'Error al actualizar cantidad';
          setTimeout(() => this.mensaje = '', 3000);
        }
      });
    } else {
      // Usuario anónimo: actualizar en localStorage
      const itemLocal = this.items.find(i => i.productoId === item.productoId);
      if (itemLocal && itemLocal.producto) {
        itemLocal.cantidad = nuevaCantidad;
        itemLocal.subtotal = itemLocal.producto.precio * nuevaCantidad;
        this.guardarCarritoLocal(this.items);
        this.mensaje = 'Cantidad actualizada';
        setTimeout(() => this.mensaje = '', 3000);
      }
    }
  }

  eliminarItem(item: CarritoItem): void {
    if (confirm('¿Estás seguro de eliminar este producto del carrito?')) {
      if (this.authService.isAuthenticated) {
        // Usuario autenticado: eliminar de BD
        this.carritoService.eliminarItem(item.id).subscribe({
          next: () => {
            this.mensaje = 'Producto eliminado del carrito';
            setTimeout(() => this.mensaje = '', 3000);
            this.cargarCarrito();
          },
          error: (error) => {
            this.mensaje = error.error?.message || 'Error al eliminar producto';
            setTimeout(() => this.mensaje = '', 3000);
          }
        });
      } else {
        // Usuario anónimo: eliminar de localStorage
        this.items = this.items.filter(i => i.productoId !== item.productoId);
        this.guardarCarritoLocal(this.items);
        this.mensaje = 'Producto eliminado del carrito';
        setTimeout(() => this.mensaje = '', 3000);
      }
    }
  }

  vaciarCarrito(): void {
    if (confirm('¿Estás seguro de vaciar todo el carrito?')) {
      if (this.authService.isAuthenticated) {
        // Usuario autenticado: vaciar en BD
        this.carritoService.vaciarCarrito().subscribe({
          next: () => {
            this.mensaje = 'Carrito vaciado';
            setTimeout(() => this.mensaje = '', 3000);
            this.cargarCarrito();
          },
          error: (error) => {
            this.mensaje = error.error?.message || 'Error al vaciar carrito';
            setTimeout(() => this.mensaje = '', 3000);
          }
        });
      } else {
        // Usuario anónimo: vaciar localStorage
        localStorage.removeItem('carrito_anonimo');
        this.items = [];
        this.mensaje = 'Carrito vaciado';
        setTimeout(() => this.mensaje = '', 3000);
      }
    }
  }

  irACheckout(): void {
    console.log('Navegando a checkout...');
    console.log('Items en carrito:', this.items);

    // Verificar que haya items en el carrito
    if (this.items.length === 0) {
      alert('El carrito está vacío');
      return;
    }

    // Navegar al checkout
    this.router.navigate(['/checkout']).then(
      () => console.log('Navegación exitosa'),
      (error) => console.error('Error en navegación:', error)
    );
  }
}
