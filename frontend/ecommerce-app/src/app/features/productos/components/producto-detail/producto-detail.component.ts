import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductoService } from '../../../../core/services/producto.service';
import { CarritoService } from '../../../../core/services/carrito.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Producto } from '../../../../shared/models';

@Component({
  selector: 'app-producto-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './producto-detail.component.html',
  styleUrl: './producto-detail.component.scss'
})
export class ProductoDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productoService = inject(ProductoService);
  private carritoService = inject(CarritoService);
  authService = inject(AuthService);

  producto: Producto | null = null;
  cantidad = 1;
  loading = false;
  mensaje = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarProducto(+id);
    }
  }

  cargarProducto(id: number): void {
    this.loading = true;
    this.productoService.getProductoById(id).subscribe({
      next: (producto) => {
        this.producto = producto;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar producto:', error);
        this.loading = false;
        this.router.navigate(['/productos']);
      }
    });
  }

  agregarAlCarrito(): void {
    if (!this.authService.isAuthenticated) {
      this.mensaje = 'Debes iniciar sesión para agregar productos al carrito';
      setTimeout(() => this.mensaje = '', 3000);
      return;
    }

    if (!this.producto) return;

    this.carritoService.agregarAlCarrito({
      productoId: this.producto.id,
      cantidad: this.cantidad
    }).subscribe({
      next: () => {
        this.mensaje = `${this.cantidad} unidad(es) agregada(s) al carrito`;
        setTimeout(() => this.mensaje = '', 3000);
      },
      error: (error) => {
        this.mensaje = error.error?.message || 'Error al agregar al carrito';
        setTimeout(() => this.mensaje = '', 3000);
      }
    });
  }

  incrementarCantidad(): void {
    if (this.producto && this.cantidad < this.producto.stock) {
      this.cantidad++;
    }
  }

  decrementarCantidad(): void {
    if (this.cantidad > 1) {
      this.cantidad--;
    }
  }
}
