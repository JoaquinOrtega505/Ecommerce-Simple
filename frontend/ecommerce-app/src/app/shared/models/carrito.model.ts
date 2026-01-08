import { Producto } from './producto.model';

export interface CarritoItem {
  id: number;
  productoId: number;
  productoNombre: string;
  productoImagen: string;
  precioUnitario: number;
  cantidad: number;
  subtotal: number;
  producto?: Producto;
}

export interface AgregarCarritoDto {
  productoId: number;
  cantidad: number;
}

export interface ActualizarCarritoDto {
  cantidad: number;
}

export interface CarritoResumen {
  items: CarritoItem[];
  total: number;
  totalItems: number;
}
