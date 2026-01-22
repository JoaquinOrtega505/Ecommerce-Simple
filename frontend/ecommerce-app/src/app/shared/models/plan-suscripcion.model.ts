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
}
