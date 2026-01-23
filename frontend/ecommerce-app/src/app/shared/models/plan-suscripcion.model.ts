export interface PlanSuscripcion {
  id: number;
  nombre: string;
  descripcion: string;
  maxProductos: number;
  precioMensual: number;
  activo: boolean;
  fechaCreacion: string;
}

export interface SuscripcionDto {
  tiendaId: number;
  planId: number;
  metodoPago?: string;
  transaccionId?: string;
  notas?: string;
}

export interface HistorialSuscripcion {
  id: number;
  planSuscripcionId: number;
  planNombre: string;
  fechaInicio: string;
  fechaFin?: string;
  estado: string;
  metodoPago?: string;
  transaccionId?: string;
  montoTotal: number;
  notas?: string;
  fechaCreacion: string;
}

export interface MiPlanResponse {
  tiendaId: number;
  planActual?: {
    id: number;
    nombre: string;
    descripcion: string;
    maxProductos: number;
    precioMensual: number;
  };
  fechaSuscripcion?: string;
  fechaVencimientoSuscripcion?: string;
  maxProductos: number;
  estadoTienda: string;
}

export interface IniciarPagoDto {
  tiendaId: number;
  planId: number;
  email: string;
}

export interface IniciarPagoResponse {
  preferenceId: string;
  initPoint: string;
  sandboxInitPoint: string;
}

export interface ConfirmarPagoDto {
  tiendaId: number;
  planId: number;
  paymentId?: number;
  preferenceId?: string;
}
