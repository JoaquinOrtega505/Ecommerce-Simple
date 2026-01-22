import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TiendaService } from '../../../../core/services/tienda.service';
import { PlanesService } from '../../../../core/services/planes.service';
import { UploadService } from '../../../../core/services/upload.service';
import { AuthService } from '../../../../core/services/auth.service';
import { PlanSuscripcion } from '../../../../shared/models/plan-suscripcion.model';

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './onboarding-wizard.component.html',
  styleUrl: './onboarding-wizard.component.scss'
})
export class OnboardingWizardComponent implements OnInit {
  private fb = inject(FormBuilder);
  private tiendaService = inject(TiendaService);
  private planesService = inject(PlanesService);
  private uploadService = inject(UploadService);
  private authService = inject(AuthService);
  private router = inject(Router);

  currentStep = 1;
  totalSteps = 3;

  tiendaForm: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';

  planes: PlanSuscripcion[] = [];
  selectedPlanId: number | null = null;

  logoPreview: string | null = null;
  bannerPreview: string | null = null;
  uploadingLogo = false;
  uploadingBanner = false;

  constructor() {
    this.tiendaForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      subdominio: ['', [Validators.required, Validators.minLength(3), Validators.pattern(/^[a-z0-9-]+$/)]],
      descripcion: [''],
      telefonoWhatsApp: ['', [Validators.pattern(/^\+?[0-9]{10,15}$/)]],
      linkInstagram: ['', [Validators.pattern(/^https?:\/\/(www\.)?instagram\.com\/.+/)]],
      logoUrl: [''],
      bannerUrl: ['']
    });
  }

  ngOnInit(): void {
    this.loadPlanes();
  }

  loadPlanes(): void {
    this.planesService.getPlanes().subscribe({
      next: (planes) => {
        this.planes = planes;
      },
      error: (err) => {
        console.error('Error al cargar planes:', err);
      }
    });
  }

  // Navegación entre pasos
  nextStep(): void {
    if (this.currentStep === 1) {
      if (this.tiendaForm.invalid) {
        Object.keys(this.tiendaForm.controls).forEach(key => {
          this.tiendaForm.get(key)?.markAsTouched();
        });
        return;
      }
    }

    if (this.currentStep === 2) {
      if (!this.selectedPlanId) {
        this.errorMessage = 'Por favor selecciona un plan';
        return;
      }
    }

    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
      this.errorMessage = '';
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = '';
    }
  }

  selectPlan(planId: number): void {
    this.selectedPlanId = planId;
    this.errorMessage = '';
  }

  // Subida de imágenes
  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.uploadingLogo = true;

      // Preview local
      const reader = new FileReader();
      reader.onload = (e) => {
        this.logoPreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);

      // Upload a Cloudinary
      this.uploadService.uploadImage(file).subscribe({
        next: (response) => {
          this.tiendaForm.patchValue({ logoUrl: response.url });
          this.uploadingLogo = false;
        },
        error: (err) => {
          console.error('Error al subir logo:', err);
          this.uploadingLogo = false;
          this.errorMessage = 'Error al subir el logo';
        }
      });
    }
  }

  onBannerSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.uploadingBanner = true;

      // Preview local
      const reader = new FileReader();
      reader.onload = (e) => {
        this.bannerPreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);

      // Upload a Cloudinary
      this.uploadService.uploadImage(file).subscribe({
        next: (response) => {
          this.tiendaForm.patchValue({ bannerUrl: response.url });
          this.uploadingBanner = false;
        },
        error: (err) => {
          console.error('Error al subir banner:', err);
          this.uploadingBanner = false;
          this.errorMessage = 'Error al subir el banner';
        }
      });
    }
  }

  // Finalizar onboarding
  finishOnboarding(): void {
    this.loading = true;
    this.errorMessage = '';

    const tiendaData = this.tiendaForm.value;

    // Crear tienda
    this.tiendaService.crearMiTienda(tiendaData).subscribe({
      next: (tienda) => {
        // Si seleccionó un plan, suscribirse
        if (this.selectedPlanId) {
          this.planesService.suscribirseAPlan({
            tiendaId: tienda.id,
            planId: this.selectedPlanId
          }).subscribe({
            next: () => {
              this.completarOnboarding(tienda.id);
            },
            error: (err) => {
              console.error('Error al suscribirse al plan:', err);
              // Aún así completar el onboarding
              this.completarOnboarding(tienda.id);
            }
          });
        } else {
          this.completarOnboarding(tienda.id);
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Error al crear la tienda';
      }
    });
  }

  private completarOnboarding(tiendaId: number): void {
    // Actualizar el usuario en localStorage
    const currentUser = this.authService.currentUser();
    if (currentUser) {
      currentUser.tiendaId = tiendaId;
      localStorage.setItem('currentUser', JSON.stringify(currentUser));
    }

    this.successMessage = 'Tienda creada exitosamente';
    this.loading = false;

    // Redirigir al dashboard después de 2 segundos
    setTimeout(() => {
      this.router.navigate(['/emprendedor/dashboard']);
    }, 2000);
  }

  // Getters para validación
  get nombre() { return this.tiendaForm.get('nombre'); }
  get subdominio() { return this.tiendaForm.get('subdominio'); }
  get telefonoWhatsApp() { return this.tiendaForm.get('telefonoWhatsApp'); }
  get linkInstagram() { return this.tiendaForm.get('linkInstagram'); }
}
