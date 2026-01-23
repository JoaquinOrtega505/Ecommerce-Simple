import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MercadoPagoOAuthService } from '../../../../core/services/mercadopago-oauth.service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="oauth-callback-container">
      <div class="callback-card">
        <div *ngIf="loading" class="loading-state">
          <div class="spinner"></div>
          <h2>Conectando con MercadoPago...</h2>
          <p>Por favor espera mientras procesamos tu autorización</p>
        </div>

        <div *ngIf="!loading && success" class="success-state">
          <div class="success-icon">✓</div>
          <h2>¡Conexión exitosa!</h2>
          <p>MercadoPago ha sido conectado correctamente a tu tienda.</p>
          <button class="btn-primary" (click)="navigateToConfig()">
            Ir a Configuración
          </button>
        </div>

        <div *ngIf="!loading && error" class="error-state">
          <div class="error-icon">✕</div>
          <h2>Error al conectar</h2>
          <p>{{ errorMessage }}</p>
          <button class="btn-secondary" (click)="navigateToConfig()">
            Volver a Configuración
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .oauth-callback-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 2rem;
    }

    .callback-card {
      background: white;
      border-radius: 12px;
      padding: 3rem;
      max-width: 500px;
      width: 100%;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
      text-align: center;
    }

    .loading-state,
    .success-state,
    .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .spinner {
      width: 50px;
      height: 50px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #667eea;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .success-icon,
    .error-icon {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 3rem;
      font-weight: bold;
    }

    .success-icon {
      background: #d4edda;
      color: #28a745;
    }

    .error-icon {
      background: #f8d7da;
      color: #dc3545;
    }

    h2 {
      color: #333;
      margin: 0;
      font-size: 1.75rem;
    }

    p {
      color: #666;
      margin: 0;
      font-size: 1rem;
    }

    .btn-primary,
    .btn-secondary {
      padding: 0.75rem 2rem;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      cursor: pointer;
      transition: all 0.3s ease;
      margin-top: 1rem;
    }

    .btn-primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
    }

    .btn-secondary:hover {
      background: #5a6268;
    }
  `]
})
export class OAuthCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private mercadoPagoOAuthService = inject(MercadoPagoOAuthService);

  loading = true;
  success = false;
  error = false;
  errorMessage = '';

  ngOnInit(): void {
    this.handleCallback();
  }

  private handleCallback(): void {
    const code = this.route.snapshot.queryParamMap.get('code');
    const state = this.route.snapshot.queryParamMap.get('state');

    if (!code || !state) {
      this.loading = false;
      this.error = true;
      this.errorMessage = 'Parámetros de autorización inválidos';
      return;
    }

    this.mercadoPagoOAuthService.processCallback(code, state).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          this.success = true;
        } else {
          this.error = true;
          this.errorMessage = response.message || 'Error desconocido';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = true;
        this.errorMessage = err.error?.message || 'Error al procesar la autorización';
      }
    });
  }

  navigateToConfig(): void {
    this.router.navigate(['/emprendedor/configuracion']);
  }
}
