import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-pago-pending',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pago-pending.component.html',
  styleUrl: './pago-pending.component.scss'
})
export class PagoPendingComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pedidoId: string | null = null;
  paymentId: string | null = null;

  ngOnInit(): void {
    this.pedidoId = this.route.snapshot.queryParamMap.get('pedidoId');
    this.paymentId = this.route.snapshot.queryParamMap.get('payment_id');
  }

  verPedido(): void {
    if (this.pedidoId) {
      this.router.navigate(['/pedidos', this.pedidoId]);
    }
  }

  irAInicio(): void {
    this.router.navigate(['/']);
  }
}
