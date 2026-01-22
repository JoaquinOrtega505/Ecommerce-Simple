import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductoService } from '../../../../core/services/producto.service';
import { CategoriaService } from '../../../../core/services/categoria.service';
import { Categoria, ProductoCreateDto } from '../../../../shared/models';

@Component({
  selector: 'app-producto-form',
  templateUrl: './producto-form.component.html',
  styleUrl: './producto-form.component.scss'
})
export class ProductoFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private productoService = inject(ProductoService);
  private categoriaService = inject(CategoriaService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  productoForm: FormGroup;
  categorias: Categoria[] = [];
  isEditMode = false;
  productoId?: number;
  loading = false;
  error = '';
  imagenPreview?: string;

  constructor() {
    this.productoForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: ['', [Validators.required, Validators.minLength(10)]],
      precio: [0, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      imagenUrl: ['', Validators.required],
      categoriaId: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.cargarCategorias();

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.productoId = +params['id'];
        this.cargarProducto(this.productoId);
      }
    });
  }

  cargarCategorias(): void {
    this.categoriaService.getCategorias().subscribe({
      next: (data) => {
        this.categorias = data;
      },
      error: (error) => {
        this.error = 'Error al cargar categorías';
      }
    });
  }

  cargarProducto(id: number): void {
    this.loading = true;
    this.productoService.getProductoById(id).subscribe({
      next: (producto) => {
        this.productoForm.patchValue({
          nombre: producto.nombre,
          descripcion: producto.descripcion,
          precio: producto.precio,
          stock: producto.stock,
          imagenUrl: producto.imagenUrl,
          categoriaId: producto.categoriaId
        });
        this.imagenPreview = producto.imagenUrl;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al cargar producto';
        this.loading = false;
      }
    });
  }

  onImageUrlChange(): void {
    const url = this.productoForm.get('imagenUrl')?.value;
    if (url) {
      this.imagenPreview = url;
    }
  }

  onSubmit(): void {
    if (this.productoForm.invalid) {
      Object.keys(this.productoForm.controls).forEach(key => {
        this.productoForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading = true;
    this.error = '';

    const productoData: ProductoCreateDto = this.productoForm.value;

    const request = this.isEditMode && this.productoId
      ? this.productoService.updateProducto(this.productoId, productoData)
      : this.productoService.createProducto(productoData);

    request.subscribe({
      next: () => {
        this.router.navigate(['/emprendedor/productos']);
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al guardar producto';
        this.loading = false;
      }
    });
  }

  cancelar(): void {
    this.router.navigate(['/emprendedor/productos']);
  }

  get nombre() {
    return this.productoForm.get('nombre');
  }

  get descripcion() {
    return this.productoForm.get('descripcion');
  }

  get precio() {
    return this.productoForm.get('precio');
  }

  get stock() {
    return this.productoForm.get('stock');
  }

  get imagenUrl() {
    return this.productoForm.get('imagenUrl');
  }

  get categoriaId() {
    return this.productoForm.get('categoriaId');
  }
}
