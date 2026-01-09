import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-pago-failure',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pago-failure.component.html',
  styleUrl: './pago-failure.component.scss'
})
export class PagoFailureComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pedidoId: string | null = null;

  ngOnInit(): void {
    this.pedidoId = this.route.snapshot.queryParamMap.get('pedidoId');
  }

  intentarNuevamente(): void {
    this.router.navigate(['/checkout']);
  }

  irAInicio(): void {
    this.router.navigate(['/']);
  }
}
