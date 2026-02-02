import { Component, OnInit, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-pago-success',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pago-success.component.html',
  styleUrl: './pago-success.component.scss'
})
export class PagoSuccessComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pedidoId: string | null = null;
  redirectTimer: any;
  secondsRemaining = 10;
  tiendaSubdominio: string = '';

  ngOnInit(): void {
    this.pedidoId = this.route.snapshot.queryParamMap.get('pedidoId');
    this.tiendaSubdominio = localStorage.getItem('tienda_actual') || '';

    // Iniciar contador de redirección automática
    this.startRedirectTimer();
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
  }

  startRedirectTimer(): void {
    this.redirectTimer = setInterval(() => {
      this.secondsRemaining--;
      if (this.secondsRemaining <= 0) {
        this.volverATienda();
      }
    }, 1000);
  }

  volverATienda(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
    if (this.tiendaSubdominio) {
      this.router.navigate(['/tienda', this.tiendaSubdominio]);
    } else {
      this.router.navigate(['/']);
    }
  }
}
