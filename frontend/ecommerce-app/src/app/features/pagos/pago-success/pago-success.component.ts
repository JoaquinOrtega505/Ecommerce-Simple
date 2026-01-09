import { Component, OnInit, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PagoService } from '../../../core/services/pago.service';

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
  private pagoService = inject(PagoService);

  pedidoId: string | null = null;
  paymentId: string | null = null;
  redirectTimer: any;
  secondsRemaining = 5;

  ngOnInit(): void {
    this.pedidoId = this.route.snapshot.queryParamMap.get('pedidoId');
    this.paymentId = this.route.snapshot.queryParamMap.get('payment_id');

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
        this.verMisPedidos();
      }
    }, 1000);
  }

  verPedido(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
    if (this.pedidoId) {
      this.router.navigate(['/pedidos', this.pedidoId]);
    }
  }

  verMisPedidos(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
    this.router.navigate(['/pedidos']);
  }

  irAInicio(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
    this.router.navigate(['/']);
  }
}
