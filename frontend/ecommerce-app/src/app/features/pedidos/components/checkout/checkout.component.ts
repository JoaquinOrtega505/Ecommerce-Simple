import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CarritoService } from '../../../../core/services/carrito.service';
import { CarritoItem } from '../../../../shared/models';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  private carritoService = inject(CarritoService);
  private router = inject(Router);
  private http = inject(HttpClient);

  checkoutForm: FormGroup;
  items: CarritoItem[] = [];
  loading = false;
  procesando = false;
  errorMessage = '';
  subdominioTienda: string = '';

  constructor() {
    this.checkoutForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      nombre: ['', Validators.required],
      direccionEnvio: ['', [Validators.required, Validators.minLength(10)]],
      ciudad: ['', Validators.required],
      codigoPostal: ['', Validators.required],
      telefono: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]]
    });
  }

  ngOnInit(): void {
    this.subdominioTienda = localStorage.getItem('tienda_actual') || '';
    this.cargarCarrito();
  }

  cargarCarrito(): void {
    this.loading = true;
    this.items = this.obtenerCarritoLocal();
    console.log('Items cargados en checkout:', this.items);
    this.loading = false;

    if (this.items.length === 0) {
      console.warn('No hay items en el carrito, redirigiendo...');
      this.router.navigate(['/carrito']);
    }
  }

  private obtenerCarritoLocal(): CarritoItem[] {
    const key = `carrito_${this.subdominioTienda}`;
    const carrito = localStorage.getItem(key);

    if (!carrito) {
      return [];
    }

    try {
      const items = JSON.parse(carrito);
      return items.map((item: any) => ({
        id: item.productoId,
        productoId: item.productoId,
        cantidad: item.cantidad,
        subtotal: item.producto.precio * item.cantidad,
        producto: item.producto,
        precioUnitario: item.producto.precio
      }));
    } catch (error) {
      console.error('Error al parsear carrito:', error);
      return [];
    }
  }

  get total(): number {
    return this.items.reduce((sum, item) => {
      return sum + (item.precioUnitario || item.producto?.precio || 0) * item.cantidad;
    }, 0);
  }

  get cantidadTotal(): number {
    return this.items.reduce((sum, item) => sum + item.cantidad, 0);
  }

  procesarPedido(): void {
    if (this.checkoutForm.invalid) {
      Object.keys(this.checkoutForm.controls).forEach(key => {
        this.checkoutForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.procesando = true;
    this.errorMessage = '';

    const direccion = `${this.checkoutForm.value.direccionEnvio}, ${this.checkoutForm.value.ciudad}, CP: ${this.checkoutForm.value.codigoPostal}`;

    const pedidoData = {
      compradorNombre: this.checkoutForm.value.nombre,
      compradorEmail: this.checkoutForm.value.email,
      direccionEnvio: direccion,
      items: this.items.map(item => ({
        productoId: item.productoId,
        cantidad: item.cantidad
      }))
    };

    this.http.post<any>(`${environment.apiUrl}/pedidos/anonimo`, pedidoData).subscribe({
      next: (pedido) => {
        console.log('Pedido creado:', pedido);

        // Vaciar el carrito
        const key = `carrito_${this.subdominioTienda}`;
        localStorage.removeItem(key);

        this.procesando = false;

        // Redirigir a página de éxito
        this.router.navigate(['/pago/success'], {
          queryParams: { pedidoId: pedido.id }
        });
      },
      error: (error) => {
        console.error('Error al crear pedido:', error);
        this.errorMessage = error.error?.message || 'Error al procesar el pedido';
        this.procesando = false;
      }
    });
  }

  get email() {
    return this.checkoutForm.get('email');
  }

  get nombre() {
    return this.checkoutForm.get('nombre');
  }

  get direccionEnvio() {
    return this.checkoutForm.get('direccionEnvio');
  }

  get ciudad() {
    return this.checkoutForm.get('ciudad');
  }

  get codigoPostal() {
    return this.checkoutForm.get('codigoPostal');
  }

  get telefono() {
    return this.checkoutForm.get('telefono');
  }
}
