import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PreferenciaResponse {
  preferenceId: string;
  initPoint: string;
  sandboxInitPoint: string;
}

export interface EstadoPagoResponse {
  pedidoId: number;
  estado: string;
  fechaPago?: string;
  metodoPago?: string;
  transaccionId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PagoService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Crea una preferencia de pago en MercadoPago para un pedido
   */
  crearPreferencia(pedidoId: number): Observable<PreferenciaResponse> {
    return this.http.post<PreferenciaResponse>(
      `${this.apiUrl}/pagos/crear-preferencia/${pedidoId}`,
      {}
    );
  }

  /**
   * Consulta el estado del pago de un pedido
   */
  consultarEstadoPago(pedidoId: number): Observable<EstadoPagoResponse> {
    return this.http.get<EstadoPagoResponse>(
      `${this.apiUrl}/pagos/estado/${pedidoId}`
    );
  }

  /**
   * Confirma un pago manualmente (para desarrollo sin webhook)
   */
  confirmarPago(pedidoId: number, paymentId?: string): Observable<any> {
    const params: { [key: string]: string } = {};
    if (paymentId) {
      params['paymentId'] = paymentId;
    }
    return this.http.post(
      `${this.apiUrl}/pagos/confirmar-pago/${pedidoId}`,
      {},
      { params }
    );
  }
}
