import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  isCollapsed = false;
  currentUser = this.authService.currentUser();

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

  toggleSidebar() {
    this.isCollapsed = !this.isCollapsed;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
