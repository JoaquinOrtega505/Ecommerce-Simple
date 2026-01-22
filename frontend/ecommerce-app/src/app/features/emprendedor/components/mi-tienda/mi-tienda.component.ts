import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TiendaService } from '../../../../core/services/tienda.service';
import { ProductoService } from '../../../../core/services/producto.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Tienda } from '../../../../shared/models/tienda.model';
import { Producto } from '../../../../shared/models/producto.model';

@Component({
  selector: 'app-mi-tienda',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './mi-tienda.component.html',
  styleUrl: './mi-tienda.component.scss'
})
export class MiTiendaComponent implements OnInit {
  private tiendaService = inject(TiendaService);
  private productoService = inject(ProductoService);
  private authService = inject(AuthService);

  tienda: Tienda | null = null;
  productos: Producto[] = [];
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    const currentUser = this.authService.currentUser();
    if (currentUser?.tiendaId) {
      this.loadTienda(currentUser.tiendaId);
      this.loadProductos();
    }
  }

  loadTienda(tiendaId: number): void {
    this.tiendaService.getTiendaById(tiendaId).subscribe({
      next: (tienda) => {
        this.tienda = tienda;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar la tienda';
        this.loading = false;
      }
    });
  }

  loadProductos(): void {
    this.productoService.getMisProductos().subscribe({
      next: (productos: Producto[]) => {
        this.productos = productos;
      },
      error: (err: any) => {
        console.error('Error al cargar productos:', err);
      }
    });
  }

  getSubdominioUrl(): string {
    if (this.tienda) {
      return `${window.location.protocol}//${this.tienda.subdominio}.${window.location.hostname}`;
    }
    return '';
  }
}
