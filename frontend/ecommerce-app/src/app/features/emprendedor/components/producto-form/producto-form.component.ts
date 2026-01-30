import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductoService } from '../../../../core/services/producto.service';
import { CategoriaService } from '../../../../core/services/categoria.service';
import { UploadService } from '../../../../core/services/upload.service';
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
  private uploadService = inject(UploadService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  productoForm: FormGroup;
  categorias: Categoria[] = [];
  isEditMode = false;
  productoId?: number;
  loading = false;
  error = '';
  imagenPreview?: string;

  // Image upload
  imageSource: 'url' | 'upload' = 'url';
  uploadingImage = false;
  uploadProgress = 0;
  dragOver = false;

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

  setImageSource(source: 'url' | 'upload'): void {
    this.imageSource = source;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.uploadImage(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadImage(input.files[0]);
    }
  }

  uploadImage(file: File): void {
    if (!file.type.startsWith('image/')) {
      this.error = 'Por favor selecciona un archivo de imagen válido';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.error = 'La imagen no debe superar los 5MB';
      return;
    }

    this.uploadingImage = true;
    this.uploadProgress = 0;
    this.error = '';

    // Simulate progress
    const progressInterval = setInterval(() => {
      if (this.uploadProgress < 90) {
        this.uploadProgress += 10;
      }
    }, 100);

    this.uploadService.uploadImage(file, 'productos').subscribe({
      next: (response) => {
        clearInterval(progressInterval);
        this.uploadProgress = 100;
        this.productoForm.patchValue({ imagenUrl: response.url });
        this.imagenPreview = response.url;
        this.uploadingImage = false;
      },
      error: () => {
        clearInterval(progressInterval);
        this.error = 'Error al subir la imagen. Intenta de nuevo.';
        this.uploadingImage = false;
        this.uploadProgress = 0;
      }
    });
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
