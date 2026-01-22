import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { EmailVerificationService } from '../../../../core/services/email-verification.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private emailVerificationService = inject(EmailVerificationService);
  private router = inject(Router);

  registerForm: FormGroup;
  verificationForm: FormGroup;
  errorMessage = '';
  loading = false;
  showVerification = false;
  registeredEmail = '';
  verificationError = '';
  verificationSuccess = '';
  resendCooldown = 0;
  private cooldownInterval: any;

  constructor() {
    this.registerForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.verificationForm = this.fb.group({
      codigo: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    return null;
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { confirmPassword, ...registerData } = this.registerForm.value;

    this.authService.register(registerData).subscribe({
      next: (response) => {
        // Mostrar formulario de verificación en la misma página
        this.loading = false;
        this.showVerification = true;
        this.registeredEmail = registerData.email;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Error al registrarse';
        this.loading = false;
      }
    });
  }

  get nombre() {
    return this.registerForm.get('nombre');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get password() {
    return this.registerForm.get('password');
  }

  get confirmPassword() {
    return this.registerForm.get('confirmPassword');
  }

  get codigoControl() {
    return this.verificationForm.get('codigo');
  }

  onVerifyCode(): void {
    if (this.verificationForm.invalid || this.loading) {
      return;
    }

    this.loading = true;
    this.verificationError = '';
    this.verificationSuccess = '';

    const codigo = this.verificationForm.value.codigo;

    this.emailVerificationService.verificarCodigo(this.registeredEmail, codigo).subscribe({
      next: (response) => {
        this.verificationSuccess = response.message;
        this.loading = false;

        // Actualizar el usuario en localStorage
        const currentUser = this.authService.currentUser();
        if (currentUser) {
          currentUser.emailVerificado = true;
          localStorage.setItem('currentUser', JSON.stringify(currentUser));
        }

        // Redirigir después de 2 segundos
        setTimeout(() => {
          // Si es Admin y no tiene tienda, redirigir a onboarding
          if (currentUser?.rol === 'Admin' && !currentUser?.tiendaId) {
            this.router.navigate(['/onboarding']);
          } else {
            this.router.navigate(['/productos']);
          }
        }, 2000);
      },
      error: (err) => {
        this.loading = false;
        this.verificationError = err.error?.message || 'Error al verificar el código. Por favor intenta nuevamente.';
      }
    });
  }

  onResendCode(): void {
    if (this.resendCooldown > 0 || this.loading) {
      return;
    }

    this.loading = true;
    this.verificationError = '';
    this.verificationSuccess = '';

    this.emailVerificationService.reenviarCodigo(this.registeredEmail).subscribe({
      next: (response) => {
        this.loading = false;
        this.verificationSuccess = 'Código reenviado exitosamente. Revisa tu email.';

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
        this.verificationError = err.error?.message || 'Error al reenviar el código. Por favor intenta nuevamente.';
      }
    });
  }
}
