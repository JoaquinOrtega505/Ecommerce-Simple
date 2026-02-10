import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface MiSuscripcion {
  tiendaId: number;
  plan: {
    id: number;
    nombre: string;
    descripcion: string;
    maxProductos: number;
    precioMensual: number;
  } | null;
  estadoSuscripcion: string;
  estadoMercadoPago: string | null;
  fechaSuscripcion: string | null;
  fechaInicioTrial: string | null;
  fechaFinTrial: string | null;
  fechaVencimiento: string | null;
  enTrial: boolean;
  diasRestantesTrial: number;
}

export interface CrearSuscripcionResponse {
  message: string;
  preapprovalId: string;
  status: string;
  initPoint: string | null;
  plan: string;
  diasTrial: number;
  fechaFinTrial: string;
}

@Injectable({
  providedIn: 'root'
})
export class SuscripcionesService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/suscripciones`;

  /**
   * Obtiene la Public Key de MercadoPago para crear tokens de tarjeta
   */
  getPublicKey(): Observable<{ publicKey: string }> {
    return this.http.get<{ publicKey: string }>(`${this.apiUrl}/mercadopago/public-key`);
  }

  /**
   * Crea una nueva suscripción
   */
  crearSuscripcion(planId: number, cardTokenId: string, payerEmail?: string): Observable<CrearSuscripcionResponse> {
    return this.http.post<CrearSuscripcionResponse>(`${this.apiUrl}/crear`, {
      planId,
      cardTokenId,
      payerEmail
    });
  }

  /**
   * Obtiene el estado de la suscripción actual
   */
  getMiSuscripcion(): Observable<MiSuscripcion> {
    return this.http.get<MiSuscripcion>(`${this.apiUrl}/mi-suscripcion`);
  }
}
