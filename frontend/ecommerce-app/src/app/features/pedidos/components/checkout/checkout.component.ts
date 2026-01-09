import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CarritoService } from '../../../../core/services/carrito.service';
import { PedidoService } from '../../../../core/services/pedido.service';
import { PagoService } from '../../../../core/services/pago.service';
import { CarritoItem } from '../../../../shared/models';

declare var MercadoPago: any;

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
  private pagoService = inject(PagoService);
  private router = inject(Router);

  checkoutForm: FormGroup;
  items: CarritoItem[] = [];
  loading = false;
  procesando = false;
  errorMessage = '';

  // Método de pago seleccionado
  metodoPago: 'mercadopago' | 'transferencia' = 'mercadopago';

  constructor() {
    this.checkoutForm = this.fb.group({
      direccionEnvio: ['', [Validators.required, Validators.minLength(10)]],
      ciudad: ['', Validators.required],
      codigoPostal: ['', Validators.required],
      telefono: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]]
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
      return sum + (item.precioUnitario || item.producto?.precio || 0) * item.cantidad;
    }, 0);
  }

  get cantidadTotal(): number {
    return this.items.reduce((sum, item) => sum + item.cantidad, 0);
  }

  seleccionarMetodoPago(metodo: 'mercadopago' | 'transferencia'): void {
    this.metodoPago = metodo;
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

    if (this.metodoPago === 'mercadopago') {
      this.crearPedidoYRedirigirMercadoPago();
    } else {
      // Para transferencia, crear pedido directamente
      this.crearPedido();
    }
  }

  crearPedidoYRedirigirMercadoPago(): void {
    const direccion = `${this.checkoutForm.value.direccionEnvio}, ${this.checkoutForm.value.ciudad}, CP: ${this.checkoutForm.value.codigoPostal}`;

    this.pedidoService.crearPedido({ direccionEnvio: direccion }).subscribe({
      next: (pedido) => {
        // Crear preferencia de pago en MercadoPago
        this.pagoService.crearPreferencia(pedido.id).subscribe({
          next: (preferencia) => {
            // Vaciar el carrito
            this.carritoService.vaciarCarrito().subscribe();

            // Redirigir a MercadoPago para completar el pago
            window.location.href = preferencia.sandboxInitPoint || preferencia.initPoint;
          },
          error: (error) => {
            console.error('Error al crear preferencia:', error);
            this.errorMessage = 'Error al iniciar el proceso de pago';
            this.procesando = false;
          }
        });
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Error al procesar el pedido';
        this.procesando = false;
      }
    });
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
}
