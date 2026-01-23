import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardService, DashboardEstadisticas } from '../../../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private router = inject(Router);

  estadisticas?: DashboardEstadisticas;
  loading = true;
  error = '';

  ngOnInit(): void {
    this.cargarEstadisticas();
  }

  cargarEstadisticas(): void {
    this.loading = true;
    this.error = '';

    this.dashboardService.getEstadisticas().subscribe({
      next: (data) => {
        this.estadisticas = data;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al cargar estadísticas';
        this.loading = false;
      }
    });
  }

  navegarAProductos(): void {
    this.router.navigate(['/emprendedor/productos']);
  }

  navegarAPedidos(): void {
    this.router.navigate(['/emprendedor/pedidos']);
  }
}
