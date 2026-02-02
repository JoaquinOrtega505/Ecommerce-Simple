import { Component, OnInit, AfterViewInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CarritoService } from '../../../../core/services/carrito.service';
import { PedidoService } from '../../../../core/services/pedido.service';
import { PagoService } from '../../../../core/services/pago.service';
import { AuthService } from '../../../../core/services/auth.service';
import { CarritoItem } from '../../../../shared/models';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

declare var MercadoPago: any;

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private carritoService = inject(CarritoService);
  private pedidoService = inject(PedidoService);
  private pagoService = inject(PagoService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  checkoutForm: FormGroup;
  items: CarritoItem[] = [];
  loading = false;
  procesando = false;
  errorMessage = '';
  esUsuarioAnonimo = false;
  subdominioTienda: string = '';

  // Método de pago seleccionado
  metodoPago: 'tarjeta' | 'transferencia' = 'tarjeta';

  // MercadoPago
  mp: any;
  cardForm: any;
  cardFormReady = false;

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
    this.esUsuarioAnonimo = true; // Siempre anónimo en checkout de tienda pública
    // Obtener el subdominio de la tienda actual desde localStorage
    this.subdominioTienda = localStorage.getItem('tienda_actual') || '';
    this.cargarCarrito();
    this.inicializarMercadoPago();
  }

  ngAfterViewInit(): void {
    // El cardForm se inicializará cuando el usuario seleccione pago con tarjeta
  }


  inicializarMercadoPago(): void {
    // Inicializar MercadoPago con la Public Key desde environment
    const publicKey = environment.mercadoPagoPublicKey;

    console.log('Environment:', environment);
    console.log('Public Key:', publicKey);

    if (!publicKey) {
      console.error('Public Key no está configurada en environment');
      this.errorMessage = 'Error de configuración: Public Key de MercadoPago no encontrada';
      return;
    }

    try {
      this.mp = new MercadoPago(publicKey, {
        locale: 'es-AR'
      });
      console.log('MercadoPago inicializado correctamente');
    } catch (error: any) {
      console.error('Error al inicializar MercadoPago:', error);
      this.errorMessage = 'Error al inicializar sistema de pagos: ' + error.message;
    }
  }

  inicializarCardForm(): void {
    if (!this.mp) {
      console.error('MercadoPago no está inicializado');
      return;
    }

    // Verificar que el elemento del formulario exista
    const formElement = document.getElementById('form-checkout');
    if (!formElement) {
      console.error('Elemento form-checkout no encontrado');
      return;
    }

    // Si ya existe un cardForm, no crear uno nuevo
    if (this.cardFormReady) {
      console.log('CardForm ya está inicializado');
      return;
    }

    try {
      console.log('Inicializando CardForm...');

      this.cardForm = this.mp.cardForm({
        amount: this.total.toString(),
        iframe: true,
        form: {
          id: 'form-checkout',
          cardNumber: {
            id: 'form-checkout__cardNumber',
            placeholder: 'Número de tarjeta'
          },
          expirationDate: {
            id: 'form-checkout__expirationDate',
            placeholder: 'MM/YY'
          },
          securityCode: {
            id: 'form-checkout__securityCode',
            placeholder: 'CVV'
          },
          cardholderName: {
            id: 'form-checkout__cardholderName',
            placeholder: 'Titular de la tarjeta'
          },
          issuer: {
            id: 'form-checkout__issuer',
            placeholder: 'Banco emisor'
          },
          installments: {
            id: 'form-checkout__installments',
            placeholder: 'Cuotas'
          },
          identificationType: {
            id: 'form-checkout__identificationType',
            placeholder: 'Tipo de documento'
          },
          identificationNumber: {
            id: 'form-checkout__identificationNumber',
            placeholder: 'Número de documento'
          },
          cardholderEmail: {
            id: 'form-checkout__cardholderEmail',
            placeholder: 'Email'
          }
        },
        callbacks: {
          onFormMounted: (error: any) => {
            if (error) {
              console.error('Error al montar el formulario:', error);
              this.errorMessage = 'Error al cargar el formulario de pago: ' + error.message;
              this.cardFormReady = false;
            } else {
              this.cardFormReady = true;
              this.errorMessage = '';
              console.log('Formulario de pago montado correctamente');
            }
          },
          onSubmit: (event: any) => {
            event.preventDefault();
            console.log('onSubmit llamado, pero se maneja manualmente');
          }
        }
      });
    } catch (error: any) {
      console.error('Error al inicializar CardForm:', error);
      this.errorMessage = 'Error al inicializar el formulario de pago: ' + error.message;
      this.cardFormReady = false;
    }
  }

  cargarCarrito(): void {
    this.loading = true;

    // Siempre cargar desde localStorage (compras anónimas)
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
    console.log('Carrito raw de localStorage:', carrito);
    console.log('Key usada:', key);

    if (!carrito) {
      console.warn('No hay carrito en localStorage');
      return [];
    }

    try {
      const items = JSON.parse(carrito);
      console.log('Items parseados:', items);

      const mappedItems = items.map((item: any) => ({
        id: item.productoId,
        productoId: item.productoId,
        cantidad: item.cantidad,
        subtotal: item.producto.precio * item.cantidad,
        producto: item.producto,
        precioUnitario: item.producto.precio
      }));

      console.log('Items mapeados para checkout:', mappedItems);
      return mappedItems;
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

  seleccionarMetodoPago(metodo: 'tarjeta' | 'transferencia'): void {
    this.metodoPago = metodo;
    this.cardFormReady = false;

    // Si selecciona pago con tarjeta, inicializar el CardForm después de un delay
    if (metodo === 'tarjeta') {
      // Esperar a que el DOM se actualice
      setTimeout(() => {
        this.inicializarCardForm();
      }, 300);
    }
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

    if (this.metodoPago === 'tarjeta') {
      // Usar checkout transparente con tarjeta
      this.procesarPagoTransparente();
    } else {
      // Para transferencia, crear pedido directamente
      this.crearPedido();
    }
  }

  async procesarPagoTransparente(): Promise<void> {
    if (!this.cardForm) {
      this.errorMessage = 'El formulario de pago no está inicializado';
      this.procesando = false;
      return;
    }

    this.procesando = true;
    this.errorMessage = '';

    try {
      // Obtener los datos del formulario usando createCardToken
      console.log('Generando token de pago...');

      const tokenData = await this.cardForm.createCardToken();
      console.log('Token generado:', tokenData);

      const token = tokenData?.id || tokenData?.token;

      if (!token) {
        this.errorMessage = 'No se pudo generar el token de pago. Verifica los datos de la tarjeta.';
        this.procesando = false;
        return;
      }

      // Obtener datos adicionales del formulario
      const cardholderEmail = (document.getElementById('form-checkout__cardholderEmail') as HTMLInputElement)?.value;
      const identificationType = (document.getElementById('form-checkout__identificationType') as HTMLSelectElement)?.value;
      const identificationNumber = (document.getElementById('form-checkout__identificationNumber') as HTMLInputElement)?.value;
      const issuerId = (document.getElementById('form-checkout__issuer') as HTMLSelectElement)?.value;
      const installments = (document.getElementById('form-checkout__installments') as HTMLSelectElement)?.value;

      if (!cardholderEmail || !identificationType || !identificationNumber) {
        this.errorMessage = 'Por favor completa todos los campos del formulario';
        this.procesando = false;
        return;
      }

      // Crear el pedido anónimo
      const direccion = `${this.checkoutForm.value.direccionEnvio}, ${this.checkoutForm.value.ciudad}, CP: ${this.checkoutForm.value.codigoPostal}`;

      // Siempre usar endpoint anónimo
      this.http.post<any>(`${environment.apiUrl}/pedidos/anonimo`, {
        compradorNombre: this.checkoutForm.value.nombre,
        compradorEmail: this.checkoutForm.value.email,
        direccionEnvio: direccion,
        items: this.items.map(item => ({
          productoId: item.productoId,
          cantidad: item.cantidad
        }))
      }).subscribe({
        next: (pedido) => {
          console.log('Pedido creado:', pedido);

          // Procesar el pago anónimo con el token de la tarjeta
          this.http.post(`${environment.apiUrl}/pagos/procesar-pago-anonimo`, {
            pedidoId: pedido.id,
            token: token,
            paymentMethodId: tokenData.payment_method_id || 'visa',
            issuerId: issuerId || null,
            installments: parseInt(installments) || 1,
            payer: {
              email: cardholderEmail,
              identification: {
                type: identificationType,
                number: identificationNumber
              }
            }
          }).subscribe({
            next: (paymentResponse: any) => {
              console.log('Pago procesado exitosamente:', paymentResponse);

              // Vaciar el carrito de localStorage
              const key = `carrito_${this.subdominioTienda}`;
              localStorage.removeItem(key);

              this.procesando = false;

              // Redirigir a la tienda con confirmación de pago
              this.router.navigate(['/tienda'], {
                queryParams: { pagado: 'true', pedidoId: pedido.id }
              });
            },
            error: (error) => {
              console.error('Error al procesar pago:', error);
              this.errorMessage = error.error?.message || 'Error al procesar el pago';
              this.procesando = false;
            }
          });
        },
        error: (error) => {
          console.error('Error al crear pedido:', error);
          this.errorMessage = error.error?.message || 'Error al crear el pedido';
          this.procesando = false;
        }
      });
    } catch (error: any) {
      console.error('Error al procesar pago transparente:', error);
      this.errorMessage = error.message || 'Error al tokenizar la tarjeta. Verifica que todos los campos estén completos.';
      this.procesando = false;
    }
  }


  crearPedido(): void {
    const direccion = `${this.checkoutForm.value.direccionEnvio}, ${this.checkoutForm.value.ciudad}, CP: ${this.checkoutForm.value.codigoPostal}`;

    // Siempre crear pedido anónimo
    const pedidoAnonimo = {
      compradorNombre: this.checkoutForm.value.nombre,
      compradorEmail: this.checkoutForm.value.email,
      direccionEnvio: direccion,
      items: this.items.map(item => ({
        productoId: item.productoId,
        cantidad: item.cantidad
      }))
    };

    this.http.post(`${environment.apiUrl}/pedidos/anonimo`, pedidoAnonimo).subscribe({
      next: (pedido: any) => {
        // Vaciar el carrito de localStorage
        localStorage.removeItem('carrito_anonimo');

        this.procesando = false;
        // Redirigir a la tienda con confirmación
        this.router.navigate(['/tienda'], {
          queryParams: { pedidoCreado: pedido.id }
        });
      },
      error: (error) => {
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
