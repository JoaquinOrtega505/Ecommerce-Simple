import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanesService } from '../../../../core/services/planes.service';
import { PlanSuscripcion } from '../../../../shared/models/plan-suscripcion.model';

interface PlanForm {
  nombre: string;
  descripcion: string;
  maxProductos: number;
  precioMensual: number;
  activo: boolean;
}

@Component({
  selector: 'app-planes-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './planes-admin.component.html',
  styleUrls: ['./planes-admin.component.scss']
})
export class PlanesAdminComponent implements OnInit {
  planes: PlanSuscripcion[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Form variables
  showForm = false;
  isEditing = false;
  editingPlanId?: number;
  planForm: PlanForm = this.resetForm();

  constructor(private planesService: PlanesService) {}

  ngOnInit(): void {
    this.cargarPlanes();
  }

  cargarPlanes(): void {
    this.loading = true;
    this.errorMessage = '';

    // Obtener todos los planes (incluyendo inactivos)
    this.planesService.getTodosLosPlanes().subscribe({
      next: (planes) => {
        this.planes = planes;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar planes:', err);
        this.errorMessage = 'Error al cargar los planes';
        this.loading = false;
      }
    });
  }

  resetForm(): PlanForm {
    return {
      nombre: '',
      descripcion: '',
      maxProductos: 0,
      precioMensual: 0,
      activo: true
    };
  }

  abrirFormularioNuevo(): void {
    this.showForm = true;
    this.isEditing = false;
    this.editingPlanId = undefined;
    this.planForm = this.resetForm();
    this.errorMessage = '';
    this.successMessage = '';
  }

  abrirFormularioEditar(plan: PlanSuscripcion): void {
    this.showForm = true;
    this.isEditing = true;
    this.editingPlanId = plan.id;
    this.planForm = {
      nombre: plan.nombre,
      descripcion: plan.descripcion,
      maxProductos: plan.maxProductos,
      precioMensual: plan.precioMensual,
      activo: plan.activo
    };
    this.errorMessage = '';
    this.successMessage = '';
  }

  cerrarFormulario(): void {
    this.showForm = false;
    this.isEditing = false;
    this.editingPlanId = undefined;
    this.planForm = this.resetForm();
    this.errorMessage = '';
    this.successMessage = '';
  }

  guardarPlan(): void {
    if (!this.validarFormulario()) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.isEditing && this.editingPlanId) {
      // Actualizar plan existente
      this.planesService.actualizarPlan(this.editingPlanId, this.planForm).subscribe({
        next: () => {
          this.successMessage = 'Plan actualizado exitosamente';
          this.loading = false;
          this.cerrarFormulario();
          this.cargarPlanes();
        },
        error: (err) => {
          console.error('Error al actualizar plan:', err);
          this.errorMessage = err.error?.message || 'Error al actualizar el plan';
          this.loading = false;
        }
      });
    } else {
      // Crear nuevo plan
      this.planesService.crearPlan(this.planForm).subscribe({
        next: () => {
          this.successMessage = 'Plan creado exitosamente';
          this.loading = false;
          this.cerrarFormulario();
          this.cargarPlanes();
        },
        error: (err) => {
          console.error('Error al crear plan:', err);
          this.errorMessage = err.error?.message || 'Error al crear el plan';
          this.loading = false;
        }
      });
    }
  }

  eliminarPlan(plan: PlanSuscripcion): void {
    const confirmacion = confirm(
      `¿Estás seguro que deseas eliminar el plan "${plan.nombre}"?\n\nEsta acción no se puede deshacer.`
    );

    if (!confirmacion) return;

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.planesService.eliminarPlan(plan.id).subscribe({
      next: () => {
        this.successMessage = 'Plan eliminado exitosamente';
        this.loading = false;
        this.cargarPlanes();
      },
      error: (err) => {
        console.error('Error al eliminar plan:', err);
        this.errorMessage = err.error?.message || 'Error al eliminar el plan';
        this.loading = false;
      }
    });
  }

  toggleActivo(plan: PlanSuscripcion): void {
    const nuevoEstado = !plan.activo;
    const mensaje = nuevoEstado ? 'activar' : 'desactivar';

    const confirmacion = confirm(
      `¿Estás seguro que deseas ${mensaje} el plan "${plan.nombre}"?`
    );

    if (!confirmacion) return;

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const planActualizado = {
      nombre: plan.nombre,
      descripcion: plan.descripcion,
      maxProductos: plan.maxProductos,
      precioMensual: plan.precioMensual,
      activo: nuevoEstado
    };

    this.planesService.actualizarPlan(plan.id, planActualizado).subscribe({
      next: () => {
        this.successMessage = `Plan ${mensaje}do exitosamente`;
        this.loading = false;
        this.cargarPlanes();
      },
      error: (err) => {
        console.error('Error al actualizar estado del plan:', err);
        this.errorMessage = err.error?.message || 'Error al actualizar el estado del plan';
        this.loading = false;
      }
    });
  }

  validarFormulario(): boolean {
    if (!this.planForm.nombre.trim()) {
      this.errorMessage = 'El nombre del plan es requerido';
      return false;
    }

    if (!this.planForm.descripcion.trim()) {
      this.errorMessage = 'La descripción del plan es requerida';
      return false;
    }

    if (this.planForm.maxProductos <= 0) {
      this.errorMessage = 'El número máximo de productos debe ser mayor a 0';
      return false;
    }

    if (this.planForm.precioMensual < 0) {
      this.errorMessage = 'El precio mensual no puede ser negativo';
      return false;
    }

    return true;
  }

  limpiarMensajes(): void {
    setTimeout(() => {
      this.errorMessage = '';
      this.successMessage = '';
    }, 5000);
  }
}
