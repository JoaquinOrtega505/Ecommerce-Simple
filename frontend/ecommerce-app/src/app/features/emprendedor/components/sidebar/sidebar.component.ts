import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { TiendaService } from '../../../../core/services/tienda.service';
import { Tienda } from '../../../../shared/models/tienda.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private tiendaService = inject(TiendaService);

  isCollapsed = false;
  currentUser = this.authService.currentUser();
  tienda: Tienda | null = null;

  menuItems = [
    {
      icon: '📊',
      label: 'Dashboard',
      route: '/emprendedor/dashboard'
    },
    {
      icon: '🏪',
      label: 'Mi Tienda',
      route: '/emprendedor/mi-tienda'
    },
    {
      icon: '📦',
      label: 'Productos',
      route: '/emprendedor/productos'
    },
    {
      icon: '🛒',
      label: 'Pedidos',
      route: '/emprendedor/pedidos'
    },
    {
      icon: '⚙️',
      label: 'Configuración',
      route: '/emprendedor/configuracion'
    }
  ];

  ngOnInit(): void {
    // Cargar información de la tienda
    if (this.currentUser?.tiendaId) {
      this.tiendaService.getTiendaById(this.currentUser.tiendaId).subscribe({
        next: (tienda) => {
          this.tienda = tienda;
        },
        error: (err) => {
          console.error('Error al cargar tienda:', err);
        }
      });
    }
  }

  toggleSidebar() {
    this.isCollapsed = !this.isCollapsed;
  }

  irAMiTienda(): void {
    if (this.tienda?.subdominio) {
      // Generar URL de la tienda con el subdominio, a la ruta pública /tienda
      const protocol = window.location.protocol;
      const hostname = window.location.hostname;
      const port = window.location.port ? `:${window.location.port}` : '';

      // URL con subdominio: http://subdominio.localhost:4200/tienda
      const tiendaUrl = `${protocol}//${this.tienda.subdominio}.${hostname}${port}/tienda`;
      window.open(tiendaUrl, '_blank');
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
