import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Producto, Categoria, ProductoCreateDto } from '../../shared/models';

@Injectable({
  providedIn: 'root'
})
export class ProductoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/productos`;

  getProductos(): Observable<Producto[]> {
    return this.http.get<Producto[]>(this.apiUrl);
  }

  getProductosPorTienda(tiendaId: number): Observable<Producto[]> {
    return this.http.get<Producto[]>(`${this.apiUrl}/tienda/${tiendaId}`);
  }

  getMisProductos(categoriaId?: number, buscar?: string, incluirInactivos: boolean = false): Observable<Producto[]> {
    let url = `${this.apiUrl}/mis-productos`;
    const params: string[] = [];

    if (categoriaId) {
      params.push(`categoriaId=${categoriaId}`);
    }
    if (buscar) {
      params.push(`buscar=${encodeURIComponent(buscar)}`);
    }
    if (incluirInactivos) {
      params.push(`incluirInactivos=true`);
    }

    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    return this.http.get<Producto[]>(url);
  }

  getProductoById(id: number): Observable<Producto> {
    return this.http.get<Producto>(`${this.apiUrl}/${id}`);
  }

  createProducto(producto: ProductoCreateDto): Observable<Producto> {
    return this.http.post<Producto>(this.apiUrl, producto);
  }

  updateProducto(id: number, producto: Partial<ProductoCreateDto>): Observable<Producto> {
    return this.http.put<Producto>(`${this.apiUrl}/${id}`, producto);
  }

  deleteProducto(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
