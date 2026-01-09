import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-pago-return',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pago-return.component.html',
  styleUrl: './pago-return.component.scss'
})
export class PagoReturnComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  ngOnInit(): void {
    // Obtener parámetros de la URL
    const params = this.route.snapshot.queryParamMap;
    const pedidoId = params.get('pedidoId');
    const paymentId = params.get('payment_id');
    const status = params.get('status');
    const collectionStatus = params.get('collection_status');

    // Redirigir según el estado
    if (status === 'approved' || collectionStatus === 'approved') {
      // Pago exitoso
      this.router.navigate(['/pago/success'], {
        queryParams: { pedidoId, payment_id: paymentId }
      });
    } else if (status === 'pending' || collectionStatus === 'pending') {
      // Pago pendiente
      this.router.navigate(['/pago/pending'], {
        queryParams: { pedidoId, payment_id: paymentId }
      });
    } else {
      // Pago rechazado o cancelado
      this.router.navigate(['/pago/failure'], {
        queryParams: { pedidoId, payment_id: paymentId }
      });
    }
  }
}
