import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardEstadisticas {
  tienda: {
    id: number;
    nombre: string;
    subdominio: string;
    logoUrl?: string;
    maxProductos: number;
  };
  productos: {
    total: number;
    maximo: number;
    stockBajo: number;
    disponibles: number;
  };
  pedidos: {
    total: number;
    pendientes: number;
    completados: number;
  };
  ventas: {
    totales: number;
    delMes: number;
  };
  pedidosRecientes: Array<{
    id: number;
    total: number;
    estado: string;
    fechaCreacion: Date;
    cliente: string;
  }>;
  productosMasVendidos: Array<{
    productoId: number;
    nombre: string;
    cantidadVendida: number;
  }>;
}

export interface MiTienda {
  id: number;
  nombre: string;
  subdominio: string;
  descripcion?: string;
  logoUrl?: string;
  bannerUrl?: string;
  mercadoPagoPublicKey?: string;
  envioHabilitado: boolean;
  apiEnvioProveedor?: string;
  maxProductos: number;
  activo: boolean;
}

export interface MiPedido {
  id: number;
  total: number;
  estado: string;
  fechaCreacion: Date;
  fechaEntrega?: Date;
  direccionEnvio: string;
  metodoPago?: string;
  cliente: {
    id: number;
    nombre: string;
    email: string;
  };
  items: Array<{
    productoId: number;
    productoNombre: string;
    cantidad: number;
    precioUnitario: number;
    subtotal: number;
  }>;
  cantidadItems: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/dashboard`;

  getEstadisticas(): Observable<DashboardEstadisticas> {
    return this.http.get<DashboardEstadisticas>(`${this.apiUrl}/estadisticas`);
  }

  getMiTienda(): Observable<MiTienda> {
    return this.http.get<MiTienda>(`${this.apiUrl}/mi-tienda`);
  }

  getMisPedidos(estado?: string, limit?: number): Observable<MiPedido[]> {
    let url = `${this.apiUrl}/mis-pedidos`;
    const params: string[] = [];

    if (estado) {
      params.push(`estado=${encodeURIComponent(estado)}`);
    }
    if (limit) {
      params.push(`limit=${limit}`);
    }

    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    return this.http.get<MiPedido[]>(url);
  }
}
