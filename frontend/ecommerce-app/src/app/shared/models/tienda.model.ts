import { PlanSuscripcion } from './plan-suscripcion.model';

export interface Tienda {
  id: number;
  nombre: string;
  subdominio: string;
  logoUrl?: string;
  bannerUrl?: string;
  descripcion?: string;
  telefonoWhatsApp?: string;
  linkInstagram?: string;
  mercadoPagoPublicKey?: string;
  mercadoPagoAccessToken?: string;
  envioHabilitado: boolean;
  apiEnvioProveedor?: string;
  apiEnvioCredenciales?: string;
  maxProductos: number;
  planSuscripcionId?: number;
  planSuscripcion?: PlanSuscripcion;
  fechaSuscripcion?: string;
  fechaVencimientoSuscripcion?: string;
  estadoTienda: string; // Borrador, Activa, Suspendida, Inactiva
  activo: boolean;
  fechaCreacion: string;
  fechaModificacion?: string;
}

export interface TiendaEstadisticas {
  tiendaId: number;
  nombre: string;
  subdominio: string;
  totalProductos: number;
  maxProductos: number;
  totalPedidos: number;
  totalUsuarios: number;
  totalCategorias: number;
  totalVentas: number;
  envioHabilitado: boolean;
  activo: boolean;
}

export interface CreateTiendaDto {
  nombre: string;
  subdominio: string;
  logoUrl?: string;
  bannerUrl?: string;
  descripcion?: string;
  telefonoWhatsApp?: string;
  linkInstagram?: string;
}
