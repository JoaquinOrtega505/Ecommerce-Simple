import { Component, OnInit, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { PlanesService } from '../../../../core/services/planes.service';
import { SuscripcionesService } from '../../../../core/services/suscripciones.service';
import { PlanSuscripcion } from '../../../../shared/models/plan-suscripcion.model';

declare var MercadoPago: any;

@Component({
  selector: 'app-suscripcion-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './suscripcion-checkout.component.html',
  styleUrls: ['./suscripcion-checkout.component.scss']
})
export class SuscripcionCheckoutComponent implements OnInit, OnDestroy {
  private planesService = inject(PlanesService);
  private suscripcionesService = inject(SuscripcionesService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  planes: PlanSuscripcion[] = [];
  selectedPlan: PlanSuscripcion | null = null;
  loading = false;
  procesando = false;
  errorMessage = '';
  successMessage = '';

  // MercadoPago
  mp: any = null;
  cardForm: any = null;
  publicKey = '';

  // Form data
  payerEmail = '';

  ngOnInit(): void {
    this.cargarPlanes();
    this.cargarPublicKey();

    // Check for preselected plan
    const planIdParam = this.route.snapshot.queryParamMap.get('planId');
    if (planIdParam) {
      this.preseleccionarPlan(parseInt(planIdParam));
    }
  }

  ngOnDestroy(): void {
    if (this.cardForm) {
      this.cardForm.unmount();
    }
  }

  cargarPlanes(): void {
    this.loading = true;
    this.planesService.getPlanes().subscribe({
      next: (planes) => {
        this.planes = planes;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar planes:', err);
        this.errorMessage = 'Error al cargar los planes';
        this.loading = false;
      }
    });
  }

  cargarPublicKey(): void {
    this.suscripcionesService.getPublicKey().subscribe({
      next: (response) => {
        this.publicKey = response.publicKey;
        this.inicializarMercadoPago();
      },
      error: (err) => {
        console.error('Error al obtener public key:', err);
        this.errorMessage = 'MercadoPago no está configurado. Contacte al administrador.';
      }
    });
  }

  inicializarMercadoPago(): void {
    if (!this.publicKey) return;

    // Load MercadoPago SDK
    const script = document.createElement('script');
    script.src = 'https://sdk.mercadopago.com/js/v2';
    script.onload = () => {
      this.mp = new MercadoPago(this.publicKey, { locale: 'es-AR' });
    };
    document.body.appendChild(script);
  }

  preseleccionarPlan(planId: number): void {
    setTimeout(() => {
      const plan = this.planes.find(p => p.id === planId);
      if (plan) {
        this.seleccionarPlan(plan);
      }
    }, 500);
  }

  seleccionarPlan(plan: PlanSuscripcion): void {
    this.selectedPlan = plan;
    this.errorMessage = '';

    // Mount card form after selecting plan
    setTimeout(() => this.montarFormularioTarjeta(), 100);
  }

  montarFormularioTarjeta(): void {
    if (!this.mp || this.cardForm) return;

    const cardFormElement = document.getElementById('card-form');
    if (!cardFormElement) return;

    this.cardForm = this.mp.cardForm({
      amount: this.selectedPlan?.precioMensual?.toString() || '0',
      iframe: true,
      form: {
        id: 'card-form',
        cardNumber: { id: 'card-number', placeholder: 'Número de tarjeta' },
        expirationDate: { id: 'expiration-date', placeholder: 'MM/YY' },
        securityCode: { id: 'security-code', placeholder: 'CVV' },
        cardholderName: { id: 'cardholder-name', placeholder: 'Nombre como aparece en la tarjeta' },
        identificationType: { id: 'identification-type' },
        identificationNumber: { id: 'identification-number', placeholder: 'Número de documento' }
      },
      callbacks: {
        onFormMounted: (error: any) => {
          if (error) {
            console.error('Error mounting card form:', error);
            this.errorMessage = 'Error al cargar el formulario de pago';
          }
        },
        onSubmit: (event: any) => {
          event.preventDefault();
          this.procesarSuscripcion();
        },
        onFetching: (resource: any) => {
          this.procesando = true;
        }
      }
    });
  }

  async procesarSuscripcion(): Promise<void> {
    if (!this.selectedPlan || !this.cardForm) return;

    this.procesando = true;
    this.errorMessage = '';

    try {
      const cardFormData = this.cardForm.getCardFormData();

      if (!cardFormData.token) {
        this.errorMessage = 'Error al procesar la tarjeta. Verifica los datos.';
        this.procesando = false;
        return;
      }

      this.suscripcionesService.crearSuscripcion(
        this.selectedPlan.id,
        cardFormData.token,
        this.payerEmail || undefined
      ).subscribe({
        next: (response) => {
          this.successMessage = response.message;
          this.procesando = false;

          // Redirect based on response
          if (response.initPoint) {
            window.location.href = response.initPoint;
          } else {
            this.router.navigate(['/emprendedor/suscripcion/resultado'], {
              queryParams: { status: 'success', plan: response.plan }
            });
          }
        },
        error: (err) => {
          console.error('Error al crear suscripción:', err);
          this.errorMessage = err.error?.message || 'Error al procesar la suscripción';
          this.procesando = false;
        }
      });
    } catch (error) {
      console.error('Error:', error);
      this.errorMessage = 'Error al procesar el pago';
      this.procesando = false;
    }
  }

  cancelar(): void {
    this.router.navigate(['/emprendedor/dashboard']);
  }
}
