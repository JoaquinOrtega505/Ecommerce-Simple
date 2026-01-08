import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PedidoService } from '../../../../core/services/pedido.service';
import { Pedido } from '../../../../shared/models/pedido.model';

@Component({
  selector: 'app-pedidos-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './pedidos-admin.component.html',
  styleUrl: './pedidos-admin.component.scss'
})
export class PedidosAdminComponent implements OnInit {
  pedidos: Pedido[] = [];
  pedidosFiltrados: Pedido[] = [];
  loading = false;
  mensaje = '';
  estadoFiltro = 'Todos';
  busqueda = '';

  estadosDisponibles = ['Pendiente', 'Pagado', 'Enviado', 'Entregado', 'Cancelado'];

  constructor(private pedidoService: PedidoService) {}

  ngOnInit() {
    this.cargarPedidos();
  }

  cargarPedidos() {
    this.loading = true;
    this.pedidoService.getPedidos().subscribe({
      next: (pedidos) => {
        this.pedidos = pedidos;
        this.filtrarPedidos();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error cargando pedidos:', error);
        this.mensaje = 'Error al cargar los pedidos';
        this.loading = false;
      }
    });
  }

  filtrarPedidos() {
    this.pedidosFiltrados = this.pedidos.filter(pedido => {
      const cumpleEstado = this.estadoFiltro === 'Todos' || pedido.estado === this.estadoFiltro;
      const cumpleBusqueda = !this.busqueda ||
        pedido.id.toString().includes(this.busqueda) ||
        pedido.usuarioNombre.toLowerCase().includes(this.busqueda.toLowerCase());
      return cumpleEstado && cumpleBusqueda;
    });
  }

  cambiarEstado(pedidoId: number, nuevoEstado: string) {
    if (confirm(`¿Cambiar estado del pedido #${pedidoId} a "${nuevoEstado}"?`)) {
      this.pedidoService.actualizarEstado(pedidoId, { estado: nuevoEstado }).subscribe({
        next: () => {
          this.mensaje = `Estado del pedido #${pedidoId} actualizado a "${nuevoEstado}"`;
          setTimeout(() => this.mensaje = '', 3000);
          this.cargarPedidos();
        },
        error: (error) => {
          console.error('Error actualizando estado:', error);
          this.mensaje = 'Error al actualizar el estado del pedido';
        }
      });
    }
  }

  getEstadoBadgeClass(estado: string): string {
    const clases: { [key: string]: string } = {
      'Pendiente': 'bg-warning text-dark',
      'Pagado': 'bg-info',
      'Enviado': 'bg-primary',
      'Entregado': 'bg-success',
      'Cancelado': 'bg-danger'
    };
    return clases[estado] || 'bg-secondary';
  }

  getTotalPedidos(): number {
    return this.pedidosFiltrados.reduce((sum, p) => sum + p.total, 0);
  }

  getPedidosPorEstado(estado: string): Pedido[] {
    return this.pedidos.filter(p => p.estado === estado);
  }

  getTotalPorEstado(estado: string): number {
    return this.getPedidosPorEstado(estado).reduce((sum, p) => sum + p.total, 0);
  }
}
