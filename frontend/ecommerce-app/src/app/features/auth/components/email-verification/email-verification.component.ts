import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { EmailVerificationService } from '../../../../core/services/email-verification.service';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-email-verification',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './email-verification.component.html',
  styleUrl: './email-verification.component.scss'
})
export class EmailVerificationComponent implements OnInit {
  verificationForm: FormGroup;
  email: string = '';
  loading: boolean = false;
  error: string = '';
  success: string = '';
  resendCooldown: number = 0;
  private cooldownInterval: any;

  constructor(
    private fb: FormBuilder,
    private emailVerificationService: EmailVerificationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.verificationForm = this.fb.group({
      codigo: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnInit(): void {
    // Obtener email desde los query params
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      if (!this.email) {
        // Si no hay email, redirigir al login
        this.router.navigate(['/login']);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }

  onSubmit(): void {
    if (this.verificationForm.valid && !this.loading) {
      this.loading = true;
      this.error = '';
      this.success = '';

      const codigo = this.verificationForm.value.codigo;

      this.emailVerificationService.verificarCodigo(this.email, codigo).subscribe({
        next: (response) => {
          this.success = response.message;
          this.loading = false;

          // Actualizar el usuario en localStorage para marcar email como verificado
          const currentUser = this.authService.currentUser();
          if (currentUser) {
            currentUser.emailVerificado = true;
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
          }

          // Redirigir a productos después de 2 segundos
          setTimeout(() => {
            this.router.navigate(['/productos']);
          }, 2000);
        },
        error: (err) => {
          this.loading = false;
          this.error = err.error?.message || 'Error al verificar el código. Por favor intenta nuevamente.';
        }
      });
    }
  }

  reenviarCodigo(): void {
    if (this.resendCooldown > 0) return;

    this.loading = true;
    this.error = '';
    this.success = '';

    this.emailVerificationService.reenviarCodigo(this.email).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = 'Código reenviado exitosamente. Revisa tu email.';

        // Iniciar cooldown de 60 segundos
        this.resendCooldown = 60;
        this.cooldownInterval = setInterval(() => {
          this.resendCooldown--;
          if (this.resendCooldown <= 0) {
            clearInterval(this.cooldownInterval);
          }
        }, 1000);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Error al reenviar el código. Por favor intenta nuevamente.';
      }
    });
  }

  get codigoControl() {
    return this.verificationForm.get('codigo');
  }
}
