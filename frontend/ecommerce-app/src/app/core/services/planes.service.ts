import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PlanSuscripcion,
  SuscripcionDto,
  HistorialSuscripcion,
  MiPlanResponse,
  IniciarPagoDto,
  IniciarPagoResponse,
  ConfirmarPagoDto
} from '../../shared/models/plan-suscripcion.model';

@Injectable({
  providedIn: 'root'
})
export class PlanesService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/planes`;

  /**
   * Obtiene todos los planes de suscripción activos
   */
  getPlanes(): Observable<PlanSuscripcion[]> {
    return this.http.get<PlanSuscripcion[]>(this.apiUrl);
  }

  /**
   * Obtiene un plan de suscripción por ID
   */
  getPlanById(id: number): Observable<PlanSuscripcion> {
    return this.http.get<PlanSuscripcion>(`${this.apiUrl}/${id}`);
  }

  /**
   * Suscribe una tienda a un plan
   */
  suscribirseAPlan(dto: SuscripcionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/suscribirse`, dto);
  }

  /**
   * Obtiene el historial de suscripciones de una tienda
   */
  getHistorial(tiendaId: number): Observable<HistorialSuscripcion[]> {
    return this.http.get<HistorialSuscripcion[]>(`${this.apiUrl}/historial/${tiendaId}`);
  }

  /**
   * Cancela la suscripción de una tienda
   */
  cancelarSuscripcion(tiendaId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/cancelar/${tiendaId}`, {});
  }

  /**
   * Obtiene el plan actual de la tienda del usuario autenticado
   */
  getMiPlan(): Observable<MiPlanResponse> {
    return this.http.get<MiPlanResponse>(`${this.apiUrl}/miplan`);
  }

  /**
   * Inicia el proceso de pago de suscripción con MercadoPago
   */
  iniciarPago(dto: IniciarPagoDto): Observable<IniciarPagoResponse> {
    return this.http.post<IniciarPagoResponse>(`${this.apiUrl}/iniciar-pago`, dto);
  }

  /**
   * Confirma el pago de suscripción después de la aprobación
   */
  confirmarPago(dto: ConfirmarPagoDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/confirmar-pago`, dto);
  }
}
