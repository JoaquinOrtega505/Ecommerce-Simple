import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { CarritoService } from '../../../../core/services/carrito.service';
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
  private router = inject(Router);

  items: CarritoItem[] = [];
  loading = false;
  mensaje = '';

  ngOnInit(): void {
    this.cargarCarrito();

    // Suscribirse a los cambios del carrito en tiempo real
    this.carritoService.carritoItems$.subscribe({
      next: (items) => {
        this.items = Array.isArray(items) ? items : [];
      }
    });
  }

  cargarCarrito(): void {
    this.loading = true;
    this.carritoService.getCarrito().subscribe({
      next: (items) => {
        this.items = Array.isArray(items) ? items : [];
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar carrito:', error);
        this.items = [];
        this.loading = false;
        // Si hay error de autenticación, mostrar mensaje
        if (error.status === 401) {
          this.mensaje = 'Debes iniciar sesión para ver tu carrito';
        }
      }
    });
  }

  get total(): number {
    return this.items.reduce((sum, item) => sum + item.subtotal, 0);
  }

  get cantidadTotal(): number {
    return this.items.reduce((sum, item) => sum + item.cantidad, 0);
  }

  actualizarCantidad(item: CarritoItem, nuevaCantidad: number): void {
    if (nuevaCantidad < 1) return;

    this.carritoService.actualizarCantidad(item.id, { cantidad: nuevaCantidad }).subscribe({
      next: () => {
        this.cargarCarrito();
      },
      error: (error) => {
        this.mensaje = error.error?.message || 'Error al actualizar cantidad';
        setTimeout(() => this.mensaje = '', 3000);
      }
    });
  }

  eliminarItem(itemId: number): void {
    if (confirm('¿Estás seguro de eliminar este producto del carrito?')) {
      this.carritoService.eliminarItem(itemId).subscribe({
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
    }
  }

  vaciarCarrito(): void {
    if (confirm('¿Estás seguro de vaciar todo el carrito?')) {
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
    }
  }

  irACheckout(): void {
    this.router.navigate(['/checkout']);
  }
}
