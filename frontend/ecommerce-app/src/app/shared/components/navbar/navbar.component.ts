import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CarritoService } from '../../../core/services/carrito.service';
import { Usuario } from '../../models';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent implements OnInit {
  authService = inject(AuthService);
  carritoService = inject(CarritoService);
  private router = inject(Router);

  currentUser$!: Observable<Usuario | null>;
  cantidadCarrito = 0;

  ngOnInit(): void {
    this.currentUser$ = this.authService.currentUser$;

    this.carritoService.carritoItems$.subscribe(items => {
      this.cantidadCarrito = Array.isArray(items)
        ? items.reduce((sum, item) => sum + item.cantidad, 0)
        : 0;
    });

    if (this.authService.isAuthenticated) {
      this.carritoService.getCarrito().subscribe();
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
