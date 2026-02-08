import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ConfiguracionSuscripciones {
  id: number;
  diasPrueba: number;
  maxReintentosPago: number;
  diasGraciaSuspension: number;
  diasAvisoFinTrial: number;
  activo: boolean;
  fechaCreacion: string;
  fechaModificacion?: string;
}

export interface UpdateConfiguracionSuscripciones {
  diasPrueba: number;
  maxReintentosPago: number;
  diasGraciaSuspension: number;
  diasAvisoFinTrial: number;
}

export interface MercadoPagoCredenciales {
  id: number;
  conectado: boolean;
  mercadoPagoEmail?: string;
  fechaConexion?: string;
  esProduccion: boolean;
  tokenValido: boolean;
}

export interface MercadoPagoConectarManual {
  accessToken: string;
  publicKey: string;
  esProduccion: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SuscripcionConfigService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/suscripcionconfig`;

  /**
   * Obtiene la configuración actual de suscripciones
   */
  getConfiguracion(): Observable<ConfiguracionSuscripciones> {
    return this.http.get<ConfiguracionSuscripciones>(`${this.apiUrl}/configuracion`);
  }

  /**
   * Actualiza la configuración de suscripciones
   */
  updateConfiguracion(config: UpdateConfiguracionSuscripciones): Observable<ConfiguracionSuscripciones> {
    return this.http.put<ConfiguracionSuscripciones>(`${this.apiUrl}/configuracion`, config);
  }

  /**
   * Obtiene el estado de conexión de MercadoPago
   */
  getEstadoMercadoPago(): Observable<MercadoPagoCredenciales> {
    return this.http.get<MercadoPagoCredenciales>(`${this.apiUrl}/mercadopago/estado`);
  }

  /**
   * Conecta MercadoPago con credenciales manuales
   */
  conectarMercadoPago(dto: MercadoPagoConectarManual): Observable<MercadoPagoCredenciales> {
    return this.http.post<MercadoPagoCredenciales>(`${this.apiUrl}/mercadopago/conectar`, dto);
  }

  /**
   * Desconecta MercadoPago
   */
  desconectarMercadoPago(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/mercadopago/desconectar`, {});
  }

  /**
   * Obtiene la URL de OAuth para MercadoPago (uso futuro)
   */
  getOAuthUrl(redirectUri: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${this.apiUrl}/mercadopago/oauth-url?redirectUri=${encodeURIComponent(redirectUri)}`);
  }
}
