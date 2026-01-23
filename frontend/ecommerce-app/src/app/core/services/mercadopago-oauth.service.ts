import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface MercadoPagoOAuthStatus {
  connected: boolean;
  publicKey?: string;
}

export interface MercadoPagoAuthUrl {
  authorizationUrl: string;
}

export interface MercadoPagoCallbackResponse {
  success: boolean;
  message: string;
  publicKey?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MercadoPagoOAuthService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/MercadoPagoOAuth`;

  private connectionStatusSubject = new BehaviorSubject<MercadoPagoOAuthStatus>({
    connected: false
  });
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  /**
   * Obtiene el estado de conexión de MercadoPago
   */
  getStatus(): Observable<MercadoPagoOAuthStatus> {
    return this.http.get<MercadoPagoOAuthStatus>(`${this.apiUrl}/status`).pipe(
      tap(status => this.connectionStatusSubject.next(status))
    );
  }

  /**
   * Inicia el flujo OAuth de MercadoPago
   * Retorna la URL de autorización a la que el usuario debe ser redirigido
   */
  initiateOAuth(): Observable<MercadoPagoAuthUrl> {
    return this.http.get<MercadoPagoAuthUrl>(`${this.apiUrl}/authorize`);
  }

  /**
   * Redirige al usuario a la página de autorización de MercadoPago
   */
  redirectToMercadoPagoAuth(): void {
    this.initiateOAuth().subscribe({
      next: (response) => {
        // Redirigir a la URL de autorización de MercadoPago
        window.location.href = response.authorizationUrl;
      },
      error: (error) => {
        console.error('Error al iniciar OAuth:', error);
        throw error;
      }
    });
  }

  /**
   * Procesa el callback de OAuth con el código de autorización
   */
  processCallback(code: string, state: string): Observable<MercadoPagoCallbackResponse> {
    return this.http.post<MercadoPagoCallbackResponse>(`${this.apiUrl}/callback`, {
      code,
      state
    }).pipe(
      tap(response => {
        if (response.success) {
          this.connectionStatusSubject.next({
            connected: true,
            publicKey: response.publicKey
          });
        }
      })
    );
  }

  /**
   * Desconecta MercadoPago eliminando las credenciales
   */
  disconnect(): Observable<MercadoPagoCallbackResponse> {
    return this.http.post<MercadoPagoCallbackResponse>(`${this.apiUrl}/disconnect`, {}).pipe(
      tap(response => {
        if (response.success) {
          this.connectionStatusSubject.next({
            connected: false
          });
        }
      })
    );
  }
}
