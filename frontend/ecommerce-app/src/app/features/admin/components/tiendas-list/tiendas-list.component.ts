import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TiendaService } from '../../../../core/services/tienda.service';
import { Tienda } from '../../../../shared/models/tienda.model';

@Component({
  selector: 'app-tiendas-list',
  templateUrl: './tiendas-list.component.html',
  styleUrls: ['./tiendas-list.component.scss']
})
export class TiendasListComponent implements OnInit {
  tiendas: Tienda[] = [];
  loading = false;
  errorMessage = '';

  constructor(
    private tiendaService: TiendaService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargarTiendas();
  }

  cargarTiendas(): void {
    this.loading = true;
    this.errorMessage = '';

    this.tiendaService.getTodasLasTiendas().subscribe({
      next: (tiendas) => {
        this.tiendas = tiendas;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar tiendas:', error);
        this.errorMessage = 'Error al cargar las tiendas. Por favor, intenta nuevamente.';
        this.loading = false;
      }
    });
  }

  crearNuevaTienda(): void {
    this.router.navigate(['/admin/tiendas/nueva']);
  }

  editarTienda(id: number): void {
    this.router.navigate(['/admin/tiendas/editar', id]);
  }

  toggleEstadoTienda(tienda: Tienda): void {
    if (confirm(`¿Estás seguro de que quieres ${tienda.activo ? 'desactivar' : 'activar'} la tienda "${tienda.nombre}"?`)) {
      const action = tienda.activo
        ? this.tiendaService.desactivarTienda(tienda.id)
        : this.tiendaService.activarTienda(tienda.id);

      action.subscribe({
        next: () => {
          this.cargarTiendas();
        },
        error: (error) => {
          console.error('Error al cambiar estado de tienda:', error);
          alert('Error al cambiar el estado de la tienda');
        }
      });
    }
  }

  eliminarTienda(tienda: Tienda): void {
    if (confirm(`¿Estás seguro de que quieres ELIMINAR permanentemente la tienda "${tienda.nombre}"? Esta acción no se puede deshacer.`)) {
      this.tiendaService.eliminarTienda(tienda.id).subscribe({
        next: () => {
          this.cargarTiendas();
        },
        error: (error) => {
          console.error('Error al eliminar tienda:', error);
          alert(error.error?.message || 'Error al eliminar la tienda. Puede que tenga datos relacionados.');
        }
      });
    }
  }

  verEstadisticas(id: number): void {
    // TODO: Implementar vista de estadísticas
    console.log('Ver estadísticas de tienda:', id);
  }
}
