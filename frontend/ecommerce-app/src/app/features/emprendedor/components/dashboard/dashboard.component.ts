import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardService, DashboardEstadisticas } from '../../../../core/services/dashboard.service';
import { PlanesService } from '../../../../core/services/planes.service';
import { PlanSuscripcion } from '../../../../shared/models/plan-suscripcion.model';
import { TiendaService } from '../../../../core/services/tienda.service';
import { Tienda } from '../../../../shared/models/tienda.model';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private router = inject(Router);
  private planesService = inject(PlanesService);
  private tiendaService = inject(TiendaService);

  estadisticas?: DashboardEstadisticas;
  miTienda?: Tienda;
  planActual?: PlanSuscripcion;
  planesDisponibles: PlanSuscripcion[] = [];
  mostrarPlanes = false;
  loading = true;
  loadingPlanes = false;
  error = '';
  mensajePlan = '';

  ngOnInit(): void {
    this.cargarEstadisticas();
    this.cargarMiTienda();
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

  cargarMiTienda(): void {
    const tiendaId = this.estadisticas?.tienda?.id || 1; // Obtener de auth service

    this.tiendaService.getTiendaById(tiendaId).subscribe({
      next: (tienda: Tienda) => {
        this.miTienda = tienda;

        if (tienda.planSuscripcionId) {
          this.cargarPlanActual(tienda.planSuscripcionId);
        }
      },
      error: (error: any) => {
        console.error('Error cargando tienda:', error);
      }
    });
  }

  cargarPlanActual(planId: number): void {
    this.planesService.getPlanById(planId).subscribe({
      next: (plan: PlanSuscripcion) => {
        this.planActual = plan;
      },
      error: (error: any) => {
        console.error('Error cargando plan:', error);
      }
    });
  }

  togglePlanes(): void {
    this.mostrarPlanes = !this.mostrarPlanes;

    if (this.mostrarPlanes && this.planesDisponibles.length === 0) {
      this.cargarPlanesDisponibles();
    }
  }

  cargarPlanesDisponibles(): void {
    this.loadingPlanes = true;

    this.planesService.getPlanes().subscribe({
      next: (planes: PlanSuscripcion[]) => {
        this.planesDisponibles = planes;
        this.loadingPlanes = false;
      },
      error: (error: any) => {
        console.error('Error cargando planes:', error);
        this.loadingPlanes = false;
      }
    });
  }

  cambiarPlan(plan: PlanSuscripcion): void {
    if (!this.miTienda) return;

    if (confirm(`¿Deseas cambiar al ${plan.nombre}? Esto costará $${plan.precioMensual} por mes.`)) {
      this.loadingPlanes = true;

      this.planesService.suscribirseAPlan({
        tiendaId: this.miTienda.id,
        planId: plan.id
      }).subscribe({
        next: (response: any) => {
          this.mensajePlan = response.message || 'Plan actualizado exitosamente';
          this.planActual = plan;
          this.miTienda!.maxProductos = plan.maxProductos;
          this.loadingPlanes = false;
          this.mostrarPlanes = false;

          // Recargar estadísticas
          this.cargarEstadisticas();

          setTimeout(() => this.mensajePlan = '', 5000);
        },
        error: (error: any) => {
          this.mensajePlan = error.error?.message || 'Error al cambiar de plan';
          this.loadingPlanes = false;

          setTimeout(() => this.mensajePlan = '', 5000);
        }
      });
    }
  }

  navegarAProductos(): void {
    this.router.navigate(['/emprendedor/productos']);
  }

  navegarAPedidos(): void {
    this.router.navigate(['/emprendedor/pedidos']);
  }

  navegarANuevoProducto(): void {
    this.router.navigate(['/emprendedor/productos/nuevo']);
  }
}
