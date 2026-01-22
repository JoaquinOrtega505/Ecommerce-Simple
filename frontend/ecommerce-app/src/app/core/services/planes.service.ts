import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlanSuscripcion, SuscripcionDto } from '../../shared/models/plan-suscripcion.model';

@Injectable({
  providedIn: 'root'
})
export class PlanesService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/planes`;

  getPlanes(): Observable<PlanSuscripcion[]> {
    return this.http.get<PlanSuscripcion[]>(this.apiUrl);
  }

  getPlanById(id: number): Observable<PlanSuscripcion> {
    return this.http.get<PlanSuscripcion>(`${this.apiUrl}/${id}`);
  }

  suscribirseAPlan(dto: SuscripcionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/suscribirse`, dto);
  }
}
