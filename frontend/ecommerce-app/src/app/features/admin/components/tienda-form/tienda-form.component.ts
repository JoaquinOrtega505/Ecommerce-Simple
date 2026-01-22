import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TiendaService } from '../../../../core/services/tienda.service';
import { UploadService } from '../../../../core/services/upload.service';
import { Tienda } from '../../../../shared/models/tienda.model';

@Component({
  selector: 'app-tienda-form',
  templateUrl: './tienda-form.component.html',
  styleUrls: ['./tienda-form.component.scss']
})
export class TiendaFormComponent implements OnInit {
  tiendaForm!: FormGroup;
  isEditMode = false;
  tiendaId?: number;
  loading = false;
  uploadingLogo = false;
  uploadingBanner = false;
  errorMessage = '';
  successMessage = '';
  logoPreview: string | null = null;
  bannerPreview: string | null = null;

  constructor(
    private fb: FormBuilder,
    private tiendaService: TiendaService,
    private uploadService: UploadService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initializeForm();

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.tiendaId = +params['id'];
        this.cargarTienda();
      }
    });
  }

  initializeForm(): void {
    this.tiendaForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      subdominio: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
      descripcion: [''],
      logoUrl: [''],
      bannerUrl: [''],
      maxProductos: [100, [Validators.required, Validators.min(1)]],
      envioHabilitado: [false],
      mercadoPagoPublicKey: [''],
      mercadoPagoAccessToken: ['']
    });
  }

  cargarTienda(): void {
    if (!this.tiendaId) return;

    this.loading = true;
    this.tiendaService.getTiendaById(this.tiendaId).subscribe({
      next: (tienda) => {
        this.tiendaForm.patchValue(tienda);

        // Cargar vistas previas de imágenes existentes
        if (tienda.logoUrl) {
          this.logoPreview = tienda.logoUrl;
        }
        if (tienda.bannerUrl) {
          this.bannerPreview = tienda.bannerUrl;
        }

        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar tienda:', error);
        this.errorMessage = 'Error al cargar la tienda';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.tiendaForm.invalid) {
      Object.keys(this.tiendaForm.controls).forEach(key => {
        this.tiendaForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const tiendaData = this.tiendaForm.value;

    const action = this.isEditMode && this.tiendaId
      ? this.tiendaService.actualizarTienda(this.tiendaId, tiendaData)
      : this.tiendaService.crearTienda(tiendaData);

    action.subscribe({
      next: () => {
        this.successMessage = this.isEditMode ? 'Tienda actualizada correctamente' : 'Tienda creada correctamente';
        this.loading = false;

        setTimeout(() => {
          this.router.navigate(['/admin/tiendas']);
        }, 1500);
      },
      error: (error) => {
        console.error('Error al guardar tienda:', error);
        this.errorMessage = error.error?.message || 'Error al guardar la tienda. Verifica los datos e intenta nuevamente.';
        this.loading = false;
      }
    });
  }

  cancelar(): void {
    this.router.navigate(['/admin/tiendas']);
  }

  getErrorMessage(fieldName: string): string {
    const control = this.tiendaForm.get(fieldName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return 'Este campo es obligatorio';
    if (control.errors['minlength']) return `Mínimo ${control.errors['minlength'].requiredLength} caracteres`;
    if (control.errors['pattern']) return 'Solo letras minúsculas, números y guiones';
    if (control.errors['min']) return 'El valor debe ser mayor a 0';

    return '';
  }

  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.uploadingLogo = true;
    this.errorMessage = '';

    // Vista previa local
    const reader = new FileReader();
    reader.onload = (e) => {
      this.logoPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    // Subir al servidor
    this.uploadService.uploadImage(file, 'tiendas/logos').subscribe({
      next: (response) => {
        this.tiendaForm.patchValue({ logoUrl: response.url });
        this.uploadingLogo = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Error al subir el logo';
        this.uploadingLogo = false;
        this.logoPreview = null;
      }
    });
  }

  onBannerFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.uploadingBanner = true;
    this.errorMessage = '';

    // Vista previa local
    const reader = new FileReader();
    reader.onload = (e) => {
      this.bannerPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    // Subir al servidor
    this.uploadService.uploadImage(file, 'tiendas/banners').subscribe({
      next: (response) => {
        this.tiendaForm.patchValue({ bannerUrl: response.url });
        this.uploadingBanner = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Error al subir el banner';
        this.uploadingBanner = false;
        this.bannerPreview = null;
      }
    });
  }

  removeLogo(): void {
    this.logoPreview = null;
    this.tiendaForm.patchValue({ logoUrl: '' });
  }

  removeBanner(): void {
    this.bannerPreview = null;
    this.tiendaForm.patchValue({ bannerUrl: '' });
  }
}
