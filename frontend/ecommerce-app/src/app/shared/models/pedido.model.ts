import { Producto } from './producto.model';

export interface Pedido {
  id: number;
  usuarioId: number;
  usuarioNombre: string;
  total: number;
  estado: 'Pendiente' | 'Procesando' | 'Enviado' | 'Entregado' | 'Cancelado' | 'Pagado';
  direccionEnvio: string;
  fechaCreacion: string;
  fechaPago?: string;
  items: PedidoItem[];
  numeroSeguimiento?: string;
  servicioEnvio?: string;
  fechaDespacho?: string;
  fechaEntrega?: string;
  metodoPago?: string;
  transaccionId?: string;
}

export interface PedidoItem {
  id?: number;
  pedidoId?: number;
  productoId: number;
  productoNombre: string;
  productoImagen?: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  producto?: Producto;
}

export interface CrearPedidoDto {
  direccionEnvio: string;
}

export interface ActualizarEstadoPedidoDto {
  estado: string;
}
