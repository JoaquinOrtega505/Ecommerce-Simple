import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductoService } from '../../../../core/services/producto.service';
import { CategoriaService } from '../../../../core/services/categoria.service';
import { Producto, Categoria } from '../../../../shared/models';

@Component({
  selector: 'app-producto-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './producto-admin.component.html',
  styleUrl: './producto-admin.component.scss'
})
export class ProductoAdminComponent implements OnInit {
  private fb = inject(FormBuilder);
  private productoService = inject(ProductoService);
  private categoriaService = inject(CategoriaService);

  productos: Producto[] = [];
  categorias: Categoria[] = [];
  productoForm: FormGroup;
  categoriaForm: FormGroup;

  loading = false;
  editando = false;
  productoEditandoId: number | null = null;
  mensaje = '';
  mostrarFormProducto = false;
  mostrarFormCategoria = false;

  constructor() {
    this.productoForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: ['', [Validators.required, Validators.minLength(10)]],
      precio: [0, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      imagenUrl: ['', Validators.required],
      categoriaId: [0, [Validators.required, Validators.min(1)]],
      activo: [true]
    });

    this.categoriaForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.cargarProductos();
    this.cargarCategorias();
  }

  cargarProductos(): void {
    this.loading = true;
    this.productoService.getProductos().subscribe({
      next: (productos) => {
        this.productos = productos;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error al cargar productos:', error);
        this.loading = false;
      }
    });
  }

  cargarCategorias(): void {
    this.categoriaService.getCategorias().subscribe({
      next: (categorias) => {
        this.categorias = categorias;
      },
      error: (error) => {
        console.error('Error al cargar categorías:', error);
      }
    });
  }

  toggleFormProducto(): void {
    this.mostrarFormProducto = !this.mostrarFormProducto;
    if (!this.mostrarFormProducto) {
      this.cancelarEdicion();
    }
  }

  toggleFormCategoria(): void {
    this.mostrarFormCategoria = !this.mostrarFormCategoria;
    this.categoriaForm.reset();
  }

  editarProducto(producto: Producto): void {
    this.editando = true;
    this.productoEditandoId = producto.id;
    this.mostrarFormProducto = true;

    this.productoForm.patchValue({
      nombre: producto.nombre,
      descripcion: producto.descripcion,
      precio: producto.precio,
      stock: producto.stock,
      imagenUrl: producto.imagenUrl,
      categoriaId: producto.categoriaId,
      activo: producto.activo
    });
  }

  cancelarEdicion(): void {
    this.editando = false;
    this.productoEditandoId = null;
    this.productoForm.reset({ activo: true, precio: 0, stock: 0, categoriaId: 0 });
  }

  guardarProducto(): void {
    if (this.productoForm.invalid) {
      Object.keys(this.productoForm.controls).forEach(key => {
        this.productoForm.get(key)?.markAsTouched();
      });
      return;
    }

    const productoData = this.productoForm.value;

    if (this.editando && this.productoEditandoId) {
      this.productoService.updateProducto(this.productoEditandoId, productoData).subscribe({
        next: () => {
          this.mensaje = 'Producto actualizado correctamente';
          this.cargarProductos();
          this.cancelarEdicion();
          this.mostrarFormProducto = false;
          setTimeout(() => this.mensaje = '', 3000);
        },
        error: (error) => {
          this.mensaje = error.error?.message || 'Error al actualizar producto';
          setTimeout(() => this.mensaje = '', 3000);
        }
      });
    } else {
      this.productoService.createProducto(productoData).subscribe({
        next: () => {
          this.mensaje = 'Producto creado correctamente';
          this.cargarProductos();
          this.cancelarEdicion();
          this.mostrarFormProducto = false;
          setTimeout(() => this.mensaje = '', 3000);
        },
        error: (error) => {
          this.mensaje = error.error?.message || 'Error al crear producto';
          setTimeout(() => this.mensaje = '', 3000);
        }
      });
    }
  }

  eliminarProducto(id: number): void {
    if (confirm('¿Estás seguro de eliminar este producto?')) {
      this.productoService.deleteProducto(id).subscribe({
        next: () => {
          this.mensaje = 'Producto eliminado correctamente';
          this.cargarProductos();
          setTimeout(() => this.mensaje = '', 3000);
        },
        error: (error) => {
          this.mensaje = error.error?.message || 'Error al eliminar producto';
          setTimeout(() => this.mensaje = '', 3000);
        }
      });
    }
  }

  guardarCategoria(): void {
    if (this.categoriaForm.invalid) {
      Object.keys(this.categoriaForm.controls).forEach(key => {
        this.categoriaForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.categoriaService.createCategoria(this.categoriaForm.value).subscribe({
      next: () => {
        this.mensaje = 'Categoría creada correctamente';
        this.cargarCategorias();
        this.categoriaForm.reset();
        this.mostrarFormCategoria = false;
        setTimeout(() => this.mensaje = '', 3000);
      },
      error: (error) => {
        this.mensaje = error.error?.message || 'Error al crear categoría';
        setTimeout(() => this.mensaje = '', 3000);
      }
    });
  }
}
