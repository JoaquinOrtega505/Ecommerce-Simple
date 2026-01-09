import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PedidoService } from '../../../../core/services/pedido.service';
import { Pedido } from '../../../../shared/models';

@Component({
  selector: 'app-pedido-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pedido-list.component.html',
  styleUrl: './pedido-list.component.scss'
})
export class PedidoListComponent implements OnInit {
  private pedidoService = inject(PedidoService);

  pedidos: Pedido[] = [];
  loading = false;

  ngOnInit(): void {
    this.cargarPedidos();
  }

  cargarPedidos(): void {
    this.loading = true;
    this.pedidoService.getPedidos().subscribe({
      next: (pedidos) => {
        this.pedidos = pedidos.sort((a, b) =>
          new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
        );
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar pedidos:', error);
        this.loading = false;
      }
    });
  }

  getEstadoClass(estado: string): string {
    const classes: { [key: string]: string } = {
      'Pendiente': 'bg-warning',
      'Pagado': 'bg-info',
      'Procesando': 'bg-info',
      'Enviado': 'bg-primary',
      'Entregado': 'bg-success',
      'Cancelado': 'bg-danger'
    };
    return classes[estado] || 'bg-secondary';
  }

  getEstadoTexto(estado: string): string {
    const textos: { [key: string]: string } = {
      'Pendiente': 'Pendiente de pago',
      'Pagado': 'Estamos preparando tu pedido',
      'Procesando': 'Estamos preparando tu pedido',
      'Enviado': 'En camino',
      'Entregado': 'Entregado',
      'Cancelado': 'Cancelado'
    };
    return textos[estado] || estado;
  }

  getEstadoIcono(estado: string): string {
    const iconos: { [key: string]: string } = {
      'Pendiente': 'bi-clock',
      'Pagado': 'bi-box-seam',
      'Procesando': 'bi-box-seam',
      'Enviado': 'bi-truck',
      'Entregado': 'bi-check-circle',
      'Cancelado': 'bi-x-circle'
    };
    return iconos[estado] || 'bi-info-circle';
  }
}
