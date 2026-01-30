import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductoService } from '../../../../core/services/producto.service';
import { CategoriaService } from '../../../../core/services/categoria.service';
import { UploadService } from '../../../../core/services/upload.service';
import { Categoria, ProductoCreateDto } from '../../../../shared/models';

interface ImageSlot {
  url: string;
  uploading: boolean;
  progress: number;
}

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

  // Multiple images support (up to 3)
  images: ImageSlot[] = [
    { url: '', uploading: false, progress: 0 },
    { url: '', uploading: false, progress: 0 },
    { url: '', uploading: false, progress: 0 }
  ];
  imageSource: 'url' | 'upload' = 'upload';
  dragOverIndex: number | null = null;

  constructor() {
    this.productoForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: ['', [Validators.required, Validators.minLength(10)]],
      precio: [0, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0)]],
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
      error: () => {
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
          categoriaId: producto.categoriaId
        });
        // Load existing images
        if (producto.imagenUrl) this.images[0].url = producto.imagenUrl;
        if (producto.imagenUrl2) this.images[1].url = producto.imagenUrl2;
        if (producto.imagenUrl3) this.images[2].url = producto.imagenUrl3;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Error al cargar producto';
        this.loading = false;
      }
    });
  }

  setImageSource(source: 'url' | 'upload'): void {
    this.imageSource = source;
  }

  onImageUrlChange(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    this.images[index].url = input.value;
  }

  onDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOverIndex = index;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOverIndex = null;
  }

  onDrop(event: DragEvent, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOverIndex = null;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.uploadImage(files[0], index);
    }
  }

  onFileSelected(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadImage(input.files[0], index);
    }
    input.value = ''; // Reset input for re-selection
  }

  uploadImage(file: File, index: number): void {
    if (!file.type.startsWith('image/')) {
      this.error = 'Por favor selecciona un archivo de imagen válido';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.error = 'La imagen no debe superar los 5MB';
      return;
    }

    this.images[index].uploading = true;
    this.images[index].progress = 0;
    this.error = '';

    const progressInterval = setInterval(() => {
      if (this.images[index].progress < 90) {
        this.images[index].progress += 10;
      }
    }, 100);

    this.uploadService.uploadImage(file, 'productos').subscribe({
      next: (response) => {
        clearInterval(progressInterval);
        this.images[index].progress = 100;
        this.images[index].url = response.url;
        this.images[index].uploading = false;
      },
      error: () => {
        clearInterval(progressInterval);
        this.error = 'Error al subir la imagen. Intenta de nuevo.';
        this.images[index].uploading = false;
        this.images[index].progress = 0;
      }
    });
  }

  removeImage(index: number): void {
    this.images[index].url = '';
  }

  hasAtLeastOneImage(): boolean {
    return this.images.some(img => img.url.trim() !== '');
  }

  onSubmit(): void {
    if (this.productoForm.invalid) {
      Object.keys(this.productoForm.controls).forEach(key => {
        this.productoForm.get(key)?.markAsTouched();
      });
      return;
    }

    if (!this.hasAtLeastOneImage()) {
      this.error = 'Debes agregar al menos una imagen del producto';
      return;
    }

    this.loading = true;
    this.error = '';

    const productoData: ProductoCreateDto = {
      ...this.productoForm.value,
      imagenUrl: this.images[0].url || this.images[1].url || this.images[2].url,
      imagenUrl2: this.images[0].url ? (this.images[1].url || undefined) : (this.images[2].url || undefined),
      imagenUrl3: this.images[0].url && this.images[1].url ? (this.images[2].url || undefined) : undefined
    };

    // Reorder images to fill gaps
    const filledImages = this.images.filter(img => img.url.trim() !== '').map(img => img.url);
    productoData.imagenUrl = filledImages[0] || '';
    productoData.imagenUrl2 = filledImages[1] || undefined;
    productoData.imagenUrl3 = filledImages[2] || undefined;

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

  get categoriaId() {
    return this.productoForm.get('categoriaId');
  }
}
