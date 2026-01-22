import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EnviarCodigoRequest {
  email: string;
}

export interface VerificarCodigoRequest {
  email: string;
  codigo: string;
}

export interface VerificacionResponse {
  message: string;
  verificado?: boolean;
  email?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmailVerificationService {
  private apiUrl = `${environment.apiUrl}/EmailVerification`;

  constructor(private http: HttpClient) { }

  enviarCodigo(email: string): Observable<VerificacionResponse> {
    return this.http.post<VerificacionResponse>(`${this.apiUrl}/enviar-codigo`, { email });
  }

  verificarCodigo(email: string, codigo: string): Observable<VerificacionResponse> {
    return this.http.post<VerificacionResponse>(`${this.apiUrl}/verificar-codigo`, { email, codigo });
  }

  reenviarCodigo(email: string): Observable<VerificacionResponse> {
    return this.http.post<VerificacionResponse>(`${this.apiUrl}/reenviar-codigo`, { email });
  }
}
