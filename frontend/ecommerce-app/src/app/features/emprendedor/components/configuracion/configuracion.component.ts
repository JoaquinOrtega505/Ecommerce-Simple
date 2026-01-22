import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TiendaService } from '../../../../core/services/tienda.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Tienda } from '../../../../shared/models/tienda.model';

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

  tienda: Tienda | null = null;
  tiendaForm: FormGroup;
  mercadoPagoForm: FormGroup;
  loading = true;
  saving = false;
  successMessage = '';
  errorMessage = '';

  activeTab: 'general' | 'mercadopago' = 'general';

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
    }
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

    const updateData = this.tiendaForm.value;

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

    const updateData = this.mercadoPagoForm.value;

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

  setActiveTab(tab: 'general' | 'mercadopago'): void {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';
  }
}
