import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UsuarioService, Usuario, CreateUsuarioDto, UpdateUsuarioDto } from '../../../../core/services/usuario.service';
import { TiendaService } from '../../../../core/services/tienda.service';
import { Tienda } from '../../../../shared/models/tienda.model';

@Component({
  selector: 'app-usuarios-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './usuarios-admin.component.html',
  styleUrl: './usuarios-admin.component.scss'
})
export class UsuariosAdminComponent implements OnInit {
  usuarios: Usuario[] = [];
  tiendas: Tienda[] = [];
  loading = false;
  mensaje = '';
  mostrarForm = false;
  editando = false;
  usuarioEditandoId?: number;

  usuarioForm: FormGroup;
  rolesDisponibles = ['Admin', 'Deposito', 'Cliente'];

  constructor(
    private usuarioService: UsuarioService,
    private tiendaService: TiendaService,
    private fb: FormBuilder
  ) {
    this.usuarioForm = this.fb.group({
      nombre: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rol: ['Cliente', Validators.required],
      tiendaId: [null]
    });
  }

  ngOnInit() {
    this.cargarUsuarios();
    this.cargarTiendas();
  }

  cargarUsuarios() {
    this.loading = true;
    this.usuarioService.getUsuarios().subscribe({
      next: (usuarios) => {
        this.usuarios = usuarios;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error cargando usuarios:', error);
        this.mensaje = 'Error al cargar usuarios';
        this.loading = false;
      }
    });
  }

  cargarTiendas() {
    this.tiendaService.getTodasLasTiendas().subscribe({
      next: (tiendas: Tienda[]) => {
        this.tiendas = tiendas;
      },
      error: (error: any) => {
        console.error('Error cargando tiendas:', error);
      }
    });
  }

  toggleForm() {
    this.mostrarForm = !this.mostrarForm;
    if (!this.mostrarForm) {
      this.cancelarEdicion();
    }
  }

  editarUsuario(usuario: Usuario) {
    this.editando = true;
    this.usuarioEditandoId = usuario.id;
    this.mostrarForm = true;

    this.usuarioForm.patchValue({
      nombre: usuario.nombre,
      email: usuario.email,
      rol: usuario.rol,
      tiendaId: usuario.tiendaId
    });

    // Password no es requerido al editar
    this.usuarioForm.get('password')?.clearValidators();
    this.usuarioForm.get('password')?.updateValueAndValidity();
  }

  cancelarEdicion() {
    this.editando = false;
    this.usuarioEditandoId = undefined;
    this.usuarioForm.reset({ rol: 'Cliente', tiendaId: null });
    this.usuarioForm.get('password')?.setValidators(Validators.required);
    this.usuarioForm.get('password')?.updateValueAndValidity();
  }

  guardarUsuario() {
    if (this.usuarioForm.invalid) {
      return;
    }

    const formData = this.usuarioForm.value;

    if (this.editando && this.usuarioEditandoId) {
      // Actualizar usuario existente
      const updateData: UpdateUsuarioDto = {
        nombre: formData.nombre,
        email: formData.email,
        rol: formData.rol,
        tiendaId: formData.tiendaId
      };

      if (formData.password) {
        updateData.password = formData.password;
      }

      this.usuarioService.actualizarUsuario(this.usuarioEditandoId, updateData).subscribe({
        next: () => {
          this.mensaje = 'Usuario actualizado correctamente';
          setTimeout(() => this.mensaje = '', 3000);
          this.cargarUsuarios();
          this.toggleForm();
        },
        error: (error) => {
          console.error('Error actualizando usuario:', error);
          this.mensaje = error.error?.message || 'Error al actualizar usuario';
        }
      });
    } else {
      // Crear nuevo usuario
      const createData: CreateUsuarioDto = {
        nombre: formData.nombre,
        email: formData.email,
        password: formData.password,
        rol: formData.rol,
        tiendaId: formData.tiendaId
      };

      this.usuarioService.crearUsuario(createData).subscribe({
        next: () => {
          this.mensaje = 'Usuario creado correctamente';
          setTimeout(() => this.mensaje = '', 3000);
          this.cargarUsuarios();
          this.toggleForm();
        },
        error: (error) => {
          console.error('Error creando usuario:', error);
          this.mensaje = error.error?.message || 'Error al crear usuario';
        }
      });
    }
  }

  eliminarUsuario(id: number, nombre: string) {
    if (confirm(`¿Estás seguro de eliminar el usuario "${nombre}"?`)) {
      this.usuarioService.eliminarUsuario(id).subscribe({
        next: () => {
          this.mensaje = 'Usuario eliminado correctamente';
          setTimeout(() => this.mensaje = '', 3000);
          this.cargarUsuarios();
        },
        error: (error) => {
          console.error('Error eliminando usuario:', error);
          this.mensaje = error.error?.message || 'Error al eliminar usuario';
        }
      });
    }
  }

  getRolBadgeClass(rol: string): string {
    const clases: { [key: string]: string } = {
      'Admin': 'bg-danger',
      'Deposito': 'bg-warning text-dark',
      'Cliente': 'bg-primary'
    };
    return clases[rol] || 'bg-secondary';
  }

  getUsuariosPorRol(rol: string): number {
    return this.usuarios.filter(u => u.rol === rol).length;
  }
}
