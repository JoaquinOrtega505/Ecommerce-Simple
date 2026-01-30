import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Tienda, TiendaEstadisticas, CreateTiendaDto } from '../../shared/models/tienda.model';

@Injectable({
  providedIn: 'root'
})
export class TiendaService {
  private apiUrl = `${environment.apiUrl}/tiendas`;

  constructor(private http: HttpClient) {}

  /**
   * Obtiene todas las tiendas activas
   */
  getTiendasActivas(): Observable<Tienda[]> {
    return this.http.get<Tienda[]>(this.apiUrl);
  }

  /**
   * Obtiene todas las tiendas (solo SuperAdmin)
   */
  getTodasLasTiendas(): Observable<Tienda[]> {
    return this.http.get<Tienda[]>(`${this.apiUrl}/todas`);
  }

  /**
   * Obtiene una tienda por ID
   */
  getTiendaById(id: number): Observable<Tienda> {
    return this.http.get<Tienda>(`${this.apiUrl}/${id}`);
  }

  /**
   * Obtiene una tienda por subdominio
   */
  getTiendaPorSubdominio(subdominio: string): Observable<Tienda> {
    return this.http.get<Tienda>(`${this.apiUrl}/subdominio/${subdominio}`);
  }

  /**
   * Crea una nueva tienda
   */
  crearTienda(tienda: CreateTiendaDto): Observable<Tienda> {
    return this.http.post<Tienda>(this.apiUrl, tienda);
  }

  /**
   * Crea la tienda del usuario autenticado (onboarding)
   */
  crearMiTienda(tienda: CreateTiendaDto): Observable<Tienda> {
    return this.http.post<Tienda>(`${this.apiUrl}/mi-tienda`, tienda);
  }

  /**
   * Actualiza una tienda existente (actualización completa)
   */
  actualizarTiendaCompleta(id: number, tienda: Tienda): Observable<Tienda> {
    return this.http.put<Tienda>(`${this.apiUrl}/${id}`, tienda);
  }

  /**
   * Actualiza parcialmente una tienda (PATCH - para edición inline)
   */
  actualizarTienda(id: number, datos: Partial<Tienda>): Observable<Tienda> {
    return this.http.patch<Tienda>(`${this.apiUrl}/${id}`, datos);
  }

  /**
   * Desactiva una tienda
   */
  desactivarTienda(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  /**
   * Activa una tienda
   */
  activarTienda(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/activar`, {});
  }

  /**
   * Elimina una tienda permanentemente
   */
  eliminarTienda(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Obtiene estadísticas de una tienda
   */
  getEstadisticasTienda(id: number): Observable<TiendaEstadisticas> {
    return this.http.get<TiendaEstadisticas>(`${this.apiUrl}/${id}/estadisticas`);
  }

  /**
   * Verifica si una tienda puede agregar más productos
   */
  puedeAgregarProducto(id: number): Observable<{ puedeAgregar: boolean }> {
    return this.http.get<{ puedeAgregar: boolean }>(`${this.apiUrl}/${id}/puede-agregar-producto`);
  }
}
