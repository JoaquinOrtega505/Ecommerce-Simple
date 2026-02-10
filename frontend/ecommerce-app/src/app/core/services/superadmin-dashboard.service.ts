import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ResumenMetricas {
  totalTiendas: number;
  tiendasActivas: number;
  tiendasEnTrial: number;
  tiendasSuspendidas: number;
  tiendasPendienteEliminacion: number;
  trialsPorExpirar: number;
  nuevasSuscripcionesMes: number;
}

export interface EstadoCount {
  estado: string;
  cantidad: number;
}

export interface MercadoPagoStatus {
  conectado: boolean;
  email?: string;
}

export interface MetricasDashboard {
  resumen: ResumenMetricas;
  ingresos: {
    mensualesEstimados: number;
  };
  tiendasPorEstadoSuscripcion: EstadoCount[];
  tiendasPorEstadoTienda: EstadoCount[];
  mercadoPago: MercadoPagoStatus;
}

export interface PropietarioInfo {
  nombre: string;
  email: string;
}

export interface TiendaSuspendida {
  id: number;
  nombre: string;
  subdominio: string;
  estadoTienda: string;
  estadoSuscripcion: string;
  plan: string;
  fechaSuspension: string;
  diasEnSuspension: number;
  diasRestantesGracia: number;
  propietario: PropietarioInfo;
  reintentosPago: number;
}

export interface TiendasSuspendidasResponse {
  total: number;
  diasGraciaConfiguracion: number;
  tiendas: TiendaSuspendida[];
}

export interface TiendaTrial {
  id: number;
  nombre: string;
  subdominio: string;
  plan: string;
  fechaInicioTrial: string;
  fechaFinTrial: string;
  diasRestantes: number;
  tieneMercadoPago: boolean;
  propietario: PropietarioInfo;
}

export interface TiendasTrialResponse {
  total: number;
  tiendas: TiendaTrial[];
}

export interface PlanMetrica {
  id: number;
  nombre: string;
  precioMensual: number;
  maxProductos: number;
  activo: boolean;
  sincronizadoMP: boolean;
  tiendasActivas: number;
  tiendasTrial: number;
  totalTiendas: number;
}

export interface PlanesMetricasResponse {
  planes: PlanMetrica[];
  ingresosMensualesTotal: number;
}

@Injectable({
  providedIn: 'root'
})
export class SuperAdminDashboardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/superadmin/dashboard`;

  /**
   * Obtiene las métricas generales del dashboard
   */
  getMetricas(): Observable<MetricasDashboard> {
    return this.http.get<MetricasDashboard>(`${this.apiUrl}/metricas`);
  }

  /**
   * Obtiene la lista de tiendas suspendidas
   */
  getTiendasSuspendidas(): Observable<TiendasSuspendidasResponse> {
    return this.http.get<TiendasSuspendidasResponse>(`${this.apiUrl}/tiendas-suspendidas`);
  }

  /**
   * Obtiene la lista de tiendas en período de prueba
   */
  getTiendasTrial(): Observable<TiendasTrialResponse> {
    return this.http.get<TiendasTrialResponse>(`${this.apiUrl}/tiendas-trial`);
  }

  /**
   * Obtiene las métricas de los planes
   */
  getPlanesMetricas(): Observable<PlanesMetricasResponse> {
    return this.http.get<PlanesMetricasResponse>(`${this.apiUrl}/planes`);
  }
}
