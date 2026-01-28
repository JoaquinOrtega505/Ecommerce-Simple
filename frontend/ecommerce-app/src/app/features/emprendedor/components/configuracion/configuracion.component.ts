import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TiendaService } from '../../../../core/services/tienda.service';
import { AuthService } from '../../../../core/services/auth.service';
import { MercadoPagoOAuthService } from '../../../../core/services/mercadopago-oauth.service';
import { PlanesService } from '../../../../core/services/planes.service';
import { UploadService } from '../../../../core/services/upload.service';
import { Tienda } from '../../../../shared/models/tienda.model';
import { PlanSuscripcion } from '../../../../shared/models/plan-suscripcion.model';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss'
})
export class ConfiguracionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private tiendaService = inject(TiendaService);
  private authService = inject(AuthService);
  private mercadoPagoOAuthService = inject(MercadoPagoOAuthService);
  private planesService = inject(PlanesService);
  private uploadService = inject(UploadService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  tienda: Tienda | null = null;
  tiendaForm: FormGroup;
  mercadoPagoForm: FormGroup;
  loading = true;
  saving = false;
  successMessage = '';
  errorMessage = '';

  activeTab: 'general' | 'mercadopago' | 'suscripcion' = 'general';

  // MercadoPago OAuth
  mercadoPagoConnected = false;
  mercadoPagoPublicKey = '';
  useManualCredentials = false; // OAuth es el método principal
  oauthAvailable = true; // OAuth disponible

  // Suscripción
  mostrarPlanes = false;
  loadingPlanes = false;
  planesDisponibles: PlanSuscripcion[] = [];

  // Upload de imágenes
  uploadingLogo = false;
  uploadingBanner = false;
  logoPreview: string | null = null;
  bannerPreview: string | null = null;

  constructor() {
    this.tiendaForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: [''],
      telefonoWhatsApp: ['', [Validators.pattern(/^\+?[0-9]{10,15}$/)]],
      linkInstagram: ['', [Validators.pattern(/^https?:\/\/(www\.)?instagram\.com\/.+/)]]
    });

    this.mercadoPagoForm = this.fb.group({
      mercadoPagoPublicKey: [''],
      mercadoPagoAccessToken: ['']
    });
  }

  ngOnInit(): void {
    const currentUser = this.authService.currentUser();
    if (currentUser?.tiendaId) {
      this.loadTienda(currentUser.tiendaId);
      this.loadMercadoPagoStatus();
    }

    // Manejar callback de pago de MercadoPago
    this.route.queryParamMap.subscribe(params => {
      const pagoStatus = params.get('pago');
      const tiendaId = params.get('tienda');
      const planId = params.get('plan');
      const paymentId = params.get('payment_id');
      const preferenceId = params.get('preference_id');

      if (pagoStatus === 'success' && tiendaId && planId) {
        this.confirmarPagoSuscripcion(
          parseInt(tiendaId),
          parseInt(planId),
          paymentId ? parseInt(paymentId) : undefined,
          preferenceId || undefined
        );
        // Limpiar query params
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
      } else if (pagoStatus === 'failure') {
        this.errorMessage = 'El pago fue rechazado. Por favor, intenta nuevamente.';
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
      } else if (pagoStatus === 'pending') {
        this.successMessage = 'El pago está pendiente de aprobación. Te notificaremos cuando se confirme.';
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true
        });
      }
    });
  }

  loadTienda(tiendaId: number): void {
    this.tiendaService.getTiendaById(tiendaId).subscribe({
      next: (tienda) => {
        this.tienda = tienda;
        this.tiendaForm.patchValue({
          nombre: tienda.nombre,
          descripcion: tienda.descripcion,
          telefonoWhatsApp: tienda.telefonoWhatsApp,
          linkInstagram: tienda.linkInstagram
        });
        this.mercadoPagoForm.patchValue({
          mercadoPagoPublicKey: tienda.mercadoPagoPublicKey,
          mercadoPagoAccessToken: tienda.mercadoPagoAccessToken
        });
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar la configuración';
        this.loading = false;
      }
    });
  }

  saveGeneralConfig(): void {
    if (this.tiendaForm.invalid || !this.tienda) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Combinar los datos del formulario con los datos existentes de la tienda
    const updateData = {
      ...this.tienda,
      ...this.tiendaForm.value
    };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        this.successMessage = 'Configuración general actualizada correctamente';
        this.saving = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al actualizar la configuración';
        this.saving = false;
      }
    });
  }

  saveMercadoPagoConfig(): void {
    if (this.mercadoPagoForm.invalid || !this.tienda) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Combinar los datos del formulario con los datos existentes de la tienda
    const updateData = {
      ...this.tienda,
      ...this.mercadoPagoForm.value
    };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        this.successMessage = 'Configuración de MercadoPago actualizada correctamente';
        this.saving = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al actualizar la configuración';
        this.saving = false;
      }
    });
  }

  setActiveTab(tab: 'general' | 'mercadopago' | 'suscripcion'): void {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';

    // Cargar planes cuando se activa la pestaña de suscripción
    if (tab === 'suscripcion' && this.planesDisponibles.length === 0) {
      this.cargarPlanes();
    }
  }

  cargarPlanes(): void {
    this.loadingPlanes = true;
    this.planesService.getPlanes().subscribe({
      next: (planes) => {
        this.planesDisponibles = planes;
        this.loadingPlanes = false;
      },
      error: (err) => {
        console.error('Error al cargar planes:', err);
        this.errorMessage = 'Error al cargar los planes disponibles';
        this.loadingPlanes = false;
      }
    });
  }

  cambiarPlan(plan: PlanSuscripcion): void {
    if (!this.tienda) return;

    const confirmacion = confirm(`¿Estás seguro que deseas cambiar al ${plan.nombre}? El costo mensual será de $${plan.precioMensual.toFixed(2)}\n\nSerás redirigido a MercadoPago para completar el pago.`);
    if (!confirmacion) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const currentUser = this.authService.currentUser();
    if (!currentUser?.email) {
      this.errorMessage = 'No se pudo obtener el email del usuario';
      this.saving = false;
      return;
    }

    const pagoDto = {
      tiendaId: this.tienda.id,
      planId: plan.id,
      email: currentUser.email
    };

    this.planesService.iniciarPago(pagoDto).subscribe({
      next: (response) => {
        this.saving = false;
        // Redirigir a MercadoPago (usar sandbox para desarrollo)
        window.location.href = response.sandboxInitPoint || response.initPoint;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al iniciar el pago';
        this.saving = false;
      }
    });
  }

  cancelarSuscripcion(): void {
    if (!this.tienda) return;

    const confirmacion = confirm('¿Estás seguro que deseas cancelar tu suscripción? Tu tienda será suspendida y perderás acceso a todas las funcionalidades.');
    if (!confirmacion) return;

    const confirmacionFinal = confirm('Esta acción suspenderá tu tienda. ¿Confirmas la cancelación?');
    if (!confirmacionFinal) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.planesService.cancelarSuscripcion(this.tienda.id).subscribe({
      next: (response) => {
        this.successMessage = response.message || 'Suscripción cancelada exitosamente';
        this.saving = false;

        // Actualizar la tienda local
        if (this.tienda) {
          this.tienda.planSuscripcionId = undefined;
          this.tienda.planSuscripcion = undefined;
          this.tienda.fechaVencimientoSuscripcion = undefined;
          this.tienda.estadoTienda = 'Suspendida';
        }

        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al cancelar la suscripción';
        this.saving = false;
      }
    });
  }

  confirmarPagoSuscripcion(tiendaId: number, planId: number, paymentId?: number, preferenceId?: string): void {
    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const dto = {
      tiendaId,
      planId,
      paymentId,
      preferenceId
    };

    this.planesService.confirmarPago(dto).subscribe({
      next: (response) => {
        this.successMessage = response.message || '¡Suscripción activada exitosamente!';
        this.saving = false;
        this.activeTab = 'suscripcion';

        // Recargar la tienda para obtener los datos actualizados
        const currentUser = this.authService.currentUser();
        if (currentUser?.tiendaId) {
          this.loadTienda(currentUser.tiendaId);
        }

        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al confirmar el pago';
        this.saving = false;
      }
    });
  }

  // MercadoPago OAuth Methods
  loadMercadoPagoStatus(): void {
    this.mercadoPagoOAuthService.getStatus().subscribe({
      next: (status) => {
        this.mercadoPagoConnected = status.connected;
        this.mercadoPagoPublicKey = status.publicKey || '';
      },
      error: (err) => {
        console.error('Error al cargar estado de MercadoPago:', err);
      }
    });
  }

  connectMercadoPago(): void {
    this.errorMessage = '';
    this.successMessage = '';

    try {
      this.mercadoPagoOAuthService.redirectToMercadoPagoAuth();
    } catch (error) {
      this.errorMessage = 'Error al iniciar conexión con MercadoPago';
    }
  }

  disconnectMercadoPago(): void {
    const confirmacion = confirm('¿Estás seguro que deseas desconectar MercadoPago? No podrás recibir pagos hasta que vuelvas a conectarlo.');
    if (!confirmacion) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.mercadoPagoOAuthService.disconnect().subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.mercadoPagoConnected = false;
        this.mercadoPagoPublicKey = '';
        this.saving = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al desconectar MercadoPago';
        this.saving = false;
      }
    });
  }

  toggleManualCredentials(): void {
    this.useManualCredentials = !this.useManualCredentials;
    this.errorMessage = '';
    this.successMessage = '';
  }

  // Métodos para upload de imágenes
  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!this.validateImageFile(file)) return;

    this.uploadLogo(file);
  }

  onBannerSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!this.validateImageFile(file)) return;

    this.uploadBanner(file);
  }

  private validateImageFile(file: File): boolean {
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.errorMessage = 'Solo se permiten imágenes (JPG, PNG, GIF, WebP)';
      return false;
    }

    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.errorMessage = 'La imagen no puede superar los 5MB';
      return false;
    }

    return true;
  }

  private uploadLogo(file: File): void {
    this.uploadingLogo = true;
    this.errorMessage = '';

    // Preview local
    const reader = new FileReader();
    reader.onload = (e) => {
      this.logoPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    this.uploadService.uploadImage(file, 'tiendas/logos').subscribe({
      next: (response) => {
        if (this.tienda) {
          this.tienda.logoUrl = response.url;
          this.saveImageToTienda('logo', response.url);
        }
        this.uploadingLogo = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al subir el logo';
        this.uploadingLogo = false;
        this.logoPreview = null;
      }
    });
  }

  private uploadBanner(file: File): void {
    this.uploadingBanner = true;
    this.errorMessage = '';

    // Preview local
    const reader = new FileReader();
    reader.onload = (e) => {
      this.bannerPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    this.uploadService.uploadImage(file, 'tiendas/banners').subscribe({
      next: (response) => {
        if (this.tienda) {
          this.tienda.bannerUrl = response.url;
          this.saveImageToTienda('banner', response.url);
        }
        this.uploadingBanner = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al subir el banner';
        this.uploadingBanner = false;
        this.bannerPreview = null;
      }
    });
  }

  private saveImageToTienda(type: 'logo' | 'banner', url: string): void {
    if (!this.tienda) return;

    const updateData = {
      ...this.tienda,
      logoUrl: type === 'logo' ? url : this.tienda.logoUrl,
      bannerUrl: type === 'banner' ? url : this.tienda.bannerUrl
    };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        this.successMessage = type === 'logo' ? 'Logo actualizado correctamente' : 'Banner actualizado correctamente';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || `Error al guardar el ${type}`;
      }
    });
  }

  removeLogo(): void {
    if (!this.tienda) return;

    const updateData = {
      ...this.tienda,
      logoUrl: undefined
    };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        if (this.tienda) {
          this.tienda.logoUrl = undefined;
        }
        this.logoPreview = null;
        this.successMessage = 'Logo eliminado correctamente';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al eliminar el logo';
      }
    });
  }

  removeBanner(): void {
    if (!this.tienda) return;

    const updateData = {
      ...this.tienda,
      bannerUrl: undefined
    };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        if (this.tienda) {
          this.tienda.bannerUrl = undefined;
        }
        this.bannerPreview = null;
        this.successMessage = 'Banner eliminado correctamente';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Error al eliminar el banner';
      }
    });
  }
}

