import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, switchMap, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CarritoItem, AgregarCarritoDto, ActualizarCarritoDto, CarritoResumen } from '../../shared/models';

@Injectable({
  providedIn: 'root'
})
export class CarritoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/carrito`;

  private carritoItemsSubject = new BehaviorSubject<CarritoItem[]>([]);
  public carritoItems$ = this.carritoItemsSubject.asObservable();

  get cantidadTotal(): number {
    return this.carritoItemsSubject.value.reduce((sum, item) => sum + item.cantidad, 0);
  }

  get total(): number {
    return this.carritoItemsSubject.value.reduce(
      (sum, item) => sum + (item.producto?.precio || 0) * item.cantidad,
      0
    );
  }

  getCarrito(): Observable<CarritoItem[]> {
    return this.http.get<CarritoResumen>(this.apiUrl).pipe(
      map(resumen => resumen.items || []),
      tap(items => {
        this.carritoItemsSubject.next(items);
      })
    );
  }

  agregarAlCarrito(item: AgregarCarritoDto): Observable<CarritoItem> {
    return this.http.post<CarritoItem>(this.apiUrl, item).pipe(
      switchMap(result =>
        this.getCarrito().pipe(
          map(() => result)
        )
      )
    );
  }

  actualizarCantidad(itemId: number, cantidad: ActualizarCarritoDto): Observable<CarritoItem> {
    return this.http.put<CarritoItem>(`${this.apiUrl}/${itemId}`, cantidad).pipe(
      switchMap(result =>
        this.getCarrito().pipe(
          map(() => result)
        )
      )
    );
  }

  eliminarItem(itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${itemId}`).pipe(
      switchMap(result =>
        this.getCarrito().pipe(
          map(() => result)
        )
      )
    );
  }

  vaciarCarrito(): Observable<void> {
    return this.http.delete<void>(this.apiUrl).pipe(
      tap(() => this.carritoItemsSubject.next([]))
    );
  }
}
