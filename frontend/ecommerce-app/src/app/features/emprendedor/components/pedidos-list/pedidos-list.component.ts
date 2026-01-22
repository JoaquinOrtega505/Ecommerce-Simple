import { Component, OnInit, inject } from '@angular/core';
import { DashboardService, MiPedido } from '../../../../core/services/dashboard.service';

@Component({
  selector: 'app-pedidos-list',
  templateUrl: './pedidos-list.component.html',
  styleUrl: './pedidos-list.component.scss'
})
export class PedidosListComponent implements OnInit {
  private dashboardService = inject(DashboardService);

  pedidos: MiPedido[] = [];
  loading = true;
  error = '';
  estadoFiltro: string = '';

  ngOnInit(): void {
    this.cargarPedidos();
  }

  cargarPedidos(): void {
    this.loading = true;
    this.error = '';

    this.dashboardService.getMisPedidos(this.estadoFiltro || undefined).subscribe({
      next: (data) => {
        this.pedidos = data;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al cargar pedidos';
        this.loading = false;
      }
    });
  }

  filtrarPorEstado(estado: string): void {
    this.estadoFiltro = estado;
    this.cargarPedidos();
  }

  getEstadoClass(estado: string): string {
    switch (estado.toLowerCase()) {
      case 'pendiente':
        return 'estado-pendiente';
      case 'completado':
        return 'estado-completado';
      case 'cancelado':
        return 'estado-cancelado';
      default:
        return '';
    }
  }

  calcularTotalPedidos(): number {
    return this.pedidos.length;
  }

  calcularTotalVentas(): number {
    return this.pedidos
      .filter(p => p.estado.toLowerCase() === 'completado')
      .reduce((sum, p) => sum + p.total, 0);
  }
}
