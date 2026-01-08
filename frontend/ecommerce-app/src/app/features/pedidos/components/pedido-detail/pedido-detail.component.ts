import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PedidoService } from '../../../../core/services/pedido.service';
import { Pedido } from '../../../../shared/models';

@Component({
  selector: 'app-pedido-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pedido-detail.component.html',
  styleUrl: './pedido-detail.component.scss'
})
export class PedidoDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private pedidoService = inject(PedidoService);

  pedido: Pedido | null = null;
  loading = false;
  mostrarMensajeExito = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarPedido(+id);
    }

    // Mostrar mensaje si viene de checkout
    this.route.queryParams.subscribe(params => {
      if (params['nuevo'] === 'true') {
        this.mostrarMensajeExito = true;
        setTimeout(() => this.mostrarMensajeExito = false, 5000);
      }
    });
  }

  cargarPedido(id: number): void {
    this.loading = true;
    this.pedidoService.getPedidoById(id).subscribe({
      next: (pedido) => {
        this.pedido = pedido;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar pedido:', error);
        this.loading = false;
        this.router.navigate(['/pedidos']);
      }
    });
  }

  getEstadoClass(estado: string): string {
    const classes: { [key: string]: string } = {
      'Pendiente': 'bg-warning',
      'Procesando': 'bg-info',
      'Enviado': 'bg-primary',
      'Entregado': 'bg-success',
      'Cancelado': 'bg-danger'
    };
    return classes[estado] || 'bg-secondary';
  }

  getEstadoIcono(estado: string): string {
    const iconos: { [key: string]: string } = {
      'Pendiente': 'bi-clock-history',
      'Procesando': 'bi-gear',
      'Enviado': 'bi-truck',
      'Entregado': 'bi-check-circle',
      'Cancelado': 'bi-x-circle'
    };
    return iconos[estado] || 'bi-box';
  }

  getTrackingUrl(numeroSeguimiento: string): string {
    // Genera la URL de tracking de Andreani
    return `https://www.andreani.com/#!/personas/tracking/${numeroSeguimiento}`;
  }
}
