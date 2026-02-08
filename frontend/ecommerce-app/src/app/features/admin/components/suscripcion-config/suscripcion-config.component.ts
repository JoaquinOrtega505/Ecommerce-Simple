import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SuscripcionConfigService,
  ConfiguracionSuscripciones,
  MercadoPagoCredenciales,
  MercadoPagoConectarManual
} from '../../../../core/services/suscripcion-config.service';

@Component({
  selector: 'app-suscripcion-config',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './suscripcion-config.component.html',
  styleUrls: ['./suscripcion-config.component.scss']
})
export class SuscripcionConfigComponent implements OnInit {
  private configService = inject(SuscripcionConfigService);

  // Estado
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';

  // Configuración de suscripciones
  config: ConfiguracionSuscripciones | null = null;
  configForm = {
    diasPrueba: 7,
    maxReintentosPago: 3,
    diasGraciaSuspension: 3,
    diasAvisoFinTrial: 2
  };

  // MercadoPago
  mpCredenciales: MercadoPagoCredenciales | null = null;
  showMpForm = false;
  mpForm: MercadoPagoConectarManual = {
    accessToken: '',
    publicKey: '',
    esProduccion: false
  };

  ngOnInit(): void {
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.loading = true;
    this.errorMessage = '';

    // Cargar configuración
    this.configService.getConfiguracion().subscribe({
      next: (config) => {
        this.config = config;
        this.configForm = {
          diasPrueba: config.diasPrueba,
          maxReintentosPago: config.maxReintentosPago,
          diasGraciaSuspension: config.diasGraciaSuspension,
          diasAvisoFinTrial: config.diasAvisoFinTrial
        };
      },
      error: (err) => {
        console.error('Error al cargar configuración:', err);
        this.errorMessage = 'Error al cargar la configuración';
      }
    });

    // Cargar estado de MercadoPago
    this.configService.getEstadoMercadoPago().subscribe({
      next: (estado) => {
        this.mpCredenciales = estado;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar estado de MercadoPago:', err);
        this.loading = false;
      }
    });
  }

  guardarConfiguracion(): void {
    // Validaciones
    if (this.configForm.diasPrueba < 0 || this.configForm.diasPrueba > 30) {
      this.errorMessage = 'Los días de prueba deben estar entre 0 y 30';
      return;
    }
    if (this.configForm.maxReintentosPago < 1 || this.configForm.maxReintentosPago > 10) {
      this.errorMessage = 'Los reintentos de pago deben estar entre 1 y 10';
      return;
    }
    if (this.configForm.diasGraciaSuspension < 1 || this.configForm.diasGraciaSuspension > 30) {
      this.errorMessage = 'Los días de gracia deben estar entre 1 y 30';
      return;
    }
    if (this.configForm.diasAvisoFinTrial < 1 || this.configForm.diasAvisoFinTrial > this.configForm.diasPrueba) {
      this.errorMessage = 'Los días de aviso deben estar entre 1 y los días de prueba';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.configService.updateConfiguracion(this.configForm).subscribe({
      next: (config) => {
        this.config = config;
        this.successMessage = 'Configuración guardada exitosamente';
        this.saving = false;
        this.limpiarMensajes();
      },
      error: (err) => {
        console.error('Error al guardar configuración:', err);
        this.errorMessage = err.error?.message || err.error || 'Error al guardar la configuración';
        this.saving = false;
      }
    });
  }

  // MercadoPago
  mostrarFormularioMP(): void {
    this.showMpForm = true;
    this.mpForm = {
      accessToken: '',
      publicKey: '',
      esProduccion: false
    };
    this.errorMessage = '';
    this.successMessage = '';
  }

  cancelarFormularioMP(): void {
    this.showMpForm = false;
    this.mpForm = {
      accessToken: '',
      publicKey: '',
      esProduccion: false
    };
  }

  conectarMercadoPago(): void {
    if (!this.mpForm.accessToken.trim()) {
      this.errorMessage = 'El Access Token es requerido';
      return;
    }
    if (!this.mpForm.publicKey.trim()) {
      this.errorMessage = 'La Public Key es requerida';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.configService.conectarMercadoPago(this.mpForm).subscribe({
      next: (estado) => {
        this.mpCredenciales = estado;
        this.showMpForm = false;
        this.successMessage = 'MercadoPago conectado exitosamente';
        this.saving = false;
        this.limpiarMensajes();
      },
      error: (err) => {
        console.error('Error al conectar MercadoPago:', err);
        this.errorMessage = err.error?.message || err.error || 'Error al conectar MercadoPago. Verifica tus credenciales.';
        this.saving = false;
      }
    });
  }

  desconectarMercadoPago(): void {
    const confirmacion = confirm(
      '¿Estás seguro que deseas desconectar MercadoPago?\n\n' +
      'Esto eliminará las credenciales guardadas y las suscripciones dejarán de funcionar.'
    );

    if (!confirmacion) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.configService.desconectarMercadoPago().subscribe({
      next: () => {
        this.mpCredenciales = {
          id: 0,
          conectado: false,
          esProduccion: false,
          tokenValido: false
        };
        this.successMessage = 'MercadoPago desconectado exitosamente';
        this.saving = false;
        this.limpiarMensajes();
      },
      error: (err) => {
        console.error('Error al desconectar MercadoPago:', err);
        this.errorMessage = err.error?.message || err.error || 'Error al desconectar MercadoPago';
        this.saving = false;
      }
    });
  }

  limpiarMensajes(): void {
    setTimeout(() => {
      this.successMessage = '';
    }, 5000);
  }
}
