import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CarritoService } from '../../../../core/services/carrito.service';
import { PedidoService } from '../../../../core/services/pedido.service';
import { CarritoItem } from '../../../../shared/models';

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
  private pedidoService = inject(PedidoService);
  private router = inject(Router);

  checkoutForm: FormGroup;
  items: CarritoItem[] = [];
  loading = false;
  procesando = false;
  errorMessage = '';

  // Simulación de pasarela de pago
  metodoPago: 'tarjeta' | 'mercadopago' | 'transferencia' = 'tarjeta';

  constructor() {
    this.checkoutForm = this.fb.group({
      direccionEnvio: ['', [Validators.required, Validators.minLength(10)]],
      ciudad: ['', Validators.required],
      codigoPostal: ['', Validators.required],
      telefono: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],

      // Datos de tarjeta (simulados)
      numeroTarjeta: ['', [Validators.pattern(/^\d{16}$/)]],
      nombreTitular: [''],
      fechaExpiracion: ['', [Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
      cvv: ['', [Validators.pattern(/^\d{3,4}$/)]]
    });
  }

  ngOnInit(): void {
    this.cargarCarrito();
  }

  cargarCarrito(): void {
    this.loading = true;
    this.carritoService.getCarrito().subscribe({
      next: (items) => {
        this.items = items;
        this.loading = false;

        if (items.length === 0) {
          this.router.navigate(['/carrito']);
        }
      },
      error: (error) => {
        console.error('Error al cargar carrito:', error);
        this.loading = false;
      }
    });
  }

  get total(): number {
    return this.items.reduce((sum, item) => {
      return sum + (item.producto?.precio || 0) * item.cantidad;
    }, 0);
  }

  get cantidadTotal(): number {
    return this.items.reduce((sum, item) => sum + item.cantidad, 0);
  }

  seleccionarMetodoPago(metodo: 'tarjeta' | 'mercadopago' | 'transferencia'): void {
    this.metodoPago = metodo;

    // Actualizar validadores según el método de pago
    const tarjetaControls = ['numeroTarjeta', 'nombreTitular', 'fechaExpiracion', 'cvv'];

    if (metodo === 'tarjeta') {
      tarjetaControls.forEach(control => {
        this.checkoutForm.get(control)?.setValidators([Validators.required]);
      });
    } else {
      tarjetaControls.forEach(control => {
        this.checkoutForm.get(control)?.clearValidators();
      });
    }

    tarjetaControls.forEach(control => {
      this.checkoutForm.get(control)?.updateValueAndValidity();
    });
  }

  procesarPago(): void {
    if (this.checkoutForm.invalid) {
      Object.keys(this.checkoutForm.controls).forEach(key => {
        this.checkoutForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.procesando = true;
    this.errorMessage = '';

    // Simulación de procesamiento de pago
    setTimeout(() => {
      this.crearPedido();
    }, 2000);
  }

  crearPedido(): void {
    const direccion = `${this.checkoutForm.value.direccionEnvio}, ${this.checkoutForm.value.ciudad}, CP: ${this.checkoutForm.value.codigoPostal}`;

    this.pedidoService.crearPedido({ direccionEnvio: direccion }).subscribe({
      next: (pedido) => {
        // Vaciar el carrito después de crear el pedido
        this.carritoService.vaciarCarrito().subscribe();

        this.procesando = false;
        this.router.navigate(['/pedidos', pedido.id], {
          queryParams: { nuevo: 'true' }
        });
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Error al procesar el pedido';
        this.procesando = false;
      }
    });
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

  get numeroTarjeta() {
    return this.checkoutForm.get('numeroTarjeta');
  }

  get nombreTitular() {
    return this.checkoutForm.get('nombreTitular');
  }

  get fechaExpiracion() {
    return this.checkoutForm.get('fechaExpiracion');
  }

  get cvv() {
    return this.checkoutForm.get('cvv');
  }
}
