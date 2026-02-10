import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  SuperAdminDashboardService,
  MetricasDashboard,
  TiendasSuspendidasResponse,
  TiendasTrialResponse,
  PlanesMetricasResponse
} from '../../../../core/services/superadmin-dashboard.service';

@Component({
  selector: 'app-suscripciones-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './suscripciones-dashboard.component.html',
  styleUrls: ['./suscripciones-dashboard.component.scss']
})
export class SuscripcionesDashboardComponent implements OnInit {
  loading = true;
  errorMessage = '';

  // Datos
  metricas: MetricasDashboard | null = null;
  tiendasSuspendidas: TiendasSuspendidasResponse | null = null;
  tiendasTrial: TiendasTrialResponse | null = null;
  planesMetricas: PlanesMetricasResponse | null = null;

  // Tab activo
  activeTab: 'resumen' | 'suspendidas' | 'trial' | 'planes' = 'resumen';

  constructor(private dashboardService: SuperAdminDashboardService) {}

  ngOnInit(): void {
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.loading = true;
    this.errorMessage = '';

    // Cargar métricas principales
    this.dashboardService.getMetricas().subscribe({
      next: (data) => {
        this.metricas = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar métricas:', err);
        this.errorMessage = 'Error al cargar las métricas del dashboard';
        this.loading = false;
      }
    });
  }

  cambiarTab(tab: 'resumen' | 'suspendidas' | 'trial' | 'planes'): void {
    this.activeTab = tab;

    // Cargar datos según el tab
    if (tab === 'suspendidas' && !this.tiendasSuspendidas) {
      this.cargarTiendasSuspendidas();
    } else if (tab === 'trial' && !this.tiendasTrial) {
      this.cargarTiendasTrial();
    } else if (tab === 'planes' && !this.planesMetricas) {
      this.cargarPlanesMetricas();
    }
  }

  cargarTiendasSuspendidas(): void {
    this.dashboardService.getTiendasSuspendidas().subscribe({
      next: (data) => {
        this.tiendasSuspendidas = data;
      },
      error: (err) => {
        console.error('Error al cargar tiendas suspendidas:', err);
        this.errorMessage = 'Error al cargar las tiendas suspendidas';
      }
    });
  }

  cargarTiendasTrial(): void {
    this.dashboardService.getTiendasTrial().subscribe({
      next: (data) => {
        this.tiendasTrial = data;
      },
      error: (err) => {
        console.error('Error al cargar tiendas trial:', err);
        this.errorMessage = 'Error al cargar las tiendas en trial';
      }
    });
  }

  cargarPlanesMetricas(): void {
    this.dashboardService.getPlanesMetricas().subscribe({
      next: (data) => {
        this.planesMetricas = data;
      },
      error: (err) => {
        console.error('Error al cargar métricas de planes:', err);
        this.errorMessage = 'Error al cargar las métricas de planes';
      }
    });
  }

  refrescarDatos(): void {
    this.metricas = null;
    this.tiendasSuspendidas = null;
    this.tiendasTrial = null;
    this.planesMetricas = null;
    this.cargarDatos();

    if (this.activeTab === 'suspendidas') {
      this.cargarTiendasSuspendidas();
    } else if (this.activeTab === 'trial') {
      this.cargarTiendasTrial();
    } else if (this.activeTab === 'planes') {
      this.cargarPlanesMetricas();
    }
  }

  getEstadoClass(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'activa':
      case 'active':
        return 'badge-success';
      case 'suspendida':
      case 'expired':
        return 'badge-danger';
      case 'trial':
        return 'badge-info';
      case 'pendienteeliminacion':
        return 'badge-warning';
      default:
        return 'badge-secondary';
    }
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('es-AR', {
      style: 'currency',
      currency: 'ARS'
    }).format(value);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('es-AR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }
}
