import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PedidoService } from '../../../../core/services/pedido.service';
import { Pedido } from '../../../../shared/models/pedido.model';

@Component({
  selector: 'app-deposito-panel',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './deposito-panel.component.html',
  styleUrl: './deposito-panel.component.scss'
})
export class DepositoPanelComponent implements OnInit {
  pedidosPagados: Pedido[] = [];
  loading = false;
  mensaje = '';

  constructor(private pedidoService: PedidoService) {}

  ngOnInit() {
    this.cargarPedidosPagados();
  }

  cargarPedidosPagados() {
    this.loading = true;
    this.pedidoService.getPedidos().subscribe({
      next: (pedidos) => {
        // Filtrar solo pedidos pagados (listos para despachar)
        this.pedidosPagados = pedidos.filter(p => p.estado === 'Pagado');
        this.loading = false;
      },
      error: (error) => {
        console.error('Error cargando pedidos:', error);
        this.mensaje = 'Error al cargar pedidos';
        this.loading = false;
      }
    });
  }

  marcarComoEnviado(pedidoId: number) {
    if (confirm(`¿Confirmar que el pedido #${pedidoId} ha sido despachado?`)) {
      this.pedidoService.actualizarEstado(pedidoId, { estado: 'Enviado' }).subscribe({
        next: () => {
          this.mensaje = `Pedido #${pedidoId} marcado como enviado`;
          setTimeout(() => this.mensaje = '', 3000);
          this.cargarPedidosPagados();
        },
        error: (error) => {
          console.error('Error actualizando pedido:', error);
          this.mensaje = 'Error al actualizar el estado del pedido';
        }
      });
    }
  }

  imprimirListaProductos(pedido: Pedido) {
    const contenido = `
      <html>
        <head>
          <title>Lista de Productos - Pedido #${pedido.id}</title>
          <style>
            body { font-family: Arial, sans-serif; padding: 20px; }
            h1 { border-bottom: 2px solid #333; padding-bottom: 10px; }
            table { width: 100%; border-collapse: collapse; margin-top: 20px; }
            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
            th { background-color: #f2f2f2; }
            .total { font-weight: bold; font-size: 1.2em; margin-top: 20px; }
          </style>
        </head>
        <body>
          <h1>Lista de Productos - Pedido #${pedido.id}</h1>
          <p><strong>Cliente:</strong> ${pedido.usuarioNombre}</p>
          <p><strong>Dirección de Envío:</strong> ${pedido.direccionEnvio}</p>
          <p><strong>Fecha:</strong> ${new Date(pedido.fechaCreacion).toLocaleDateString('es-ES')}</p>

          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Cantidad</th>
                <th>Precio Unitario</th>
                <th>Subtotal</th>
              </tr>
            </thead>
            <tbody>
              ${pedido.items.map(item => `
                <tr>
                  <td>${item.productoNombre}</td>
                  <td>${item.cantidad}</td>
                  <td>$${item.precioUnitario.toFixed(2)}</td>
                  <td>$${item.subtotal.toFixed(2)}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>

          <p class="total">TOTAL: $${pedido.total.toFixed(2)}</p>
        </body>
      </html>
    `;

    const ventana = window.open('', '_blank');
    if (ventana) {
      ventana.document.write(contenido);
      ventana.document.close();
      ventana.print();
    }
  }

  imprimirEtiquetaEnvio(pedido: Pedido) {
    const contenido = `
      <html>
        <head>
          <title>Etiqueta de Envío - Pedido #${pedido.id}</title>
          <style>
            body {
              font-family: Arial, sans-serif;
              padding: 20px;
              display: flex;
              justify-content: center;
              align-items: center;
              min-height: 100vh;
            }
            .etiqueta {
              border: 3px solid #000;
              padding: 30px;
              max-width: 600px;
              width: 100%;
            }
            h1 {
              text-align: center;
              border-bottom: 2px solid #000;
              padding-bottom: 15px;
              margin-top: 0;
            }
            .seccion {
              margin: 20px 0;
              padding: 15px;
              border: 1px solid #ccc;
            }
            .label {
              font-weight: bold;
              display: block;
              margin-bottom: 5px;
            }
            .valor {
              font-size: 1.2em;
              display: block;
              margin-bottom: 10px;
            }
            .pedido-id {
              font-size: 2em;
              font-weight: bold;
              text-align: center;
              margin: 20px 0;
              padding: 15px;
              border: 2px solid #000;
            }
          </style>
        </head>
        <body>
          <div class="etiqueta">
            <h1>ETIQUETA DE ENVÍO</h1>

            <div class="pedido-id">
              PEDIDO #${pedido.id}
            </div>

            <div class="seccion">
              <span class="label">DESTINATARIO:</span>
              <span class="valor">${pedido.usuarioNombre}</span>
            </div>

            <div class="seccion">
              <span class="label">DIRECCIÓN DE ENTREGA:</span>
              <span class="valor">${pedido.direccionEnvio}</span>
            </div>

            <div class="seccion">
              <span class="label">FECHA DE DESPACHO:</span>
              <span class="valor">${new Date().toLocaleDateString('es-ES')}</span>
            </div>

            <div class="seccion">
              <span class="label">CANTIDAD DE ARTÍCULOS:</span>
              <span class="valor">${pedido.items.reduce((sum, item) => sum + item.cantidad, 0)} unidades</span>
            </div>

            <div class="seccion">
              <span class="label">TOTAL DEL PEDIDO:</span>
              <span class="valor">$${pedido.total.toFixed(2)}</span>
            </div>
          </div>
        </body>
      </html>
    `;

    const ventana = window.open('', '_blank');
    if (ventana) {
      ventana.document.write(contenido);
      ventana.document.close();
      ventana.print();
    }
  }

  getEstadoBadgeClass(estado: string): string {
    const clases: { [key: string]: string } = {
      'Pendiente': 'bg-warning text-dark',
      'Pagado': 'bg-success',
      'Enviado': 'bg-info',
      'Entregado': 'bg-primary',
      'Cancelado': 'bg-danger'
    };
    return clases[estado] || 'bg-secondary';
  }
}
