import { Component, inject, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TiendaService } from '../../../../core/services/tienda.service';
import { ProductoService } from '../../../../core/services/producto.service';
import { AuthService } from '../../../../core/services/auth.service';
import { UploadService } from '../../../../core/services/upload.service';
import { Tienda } from '../../../../shared/models/tienda.model';
import { Producto } from '../../../../shared/models/producto.model';

@Component({
  selector: 'app-mi-tienda',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './mi-tienda.component.html',
  styleUrl: './mi-tienda.component.scss'
})
export class MiTiendaComponent implements OnInit {
  private tiendaService = inject(TiendaService);
  private productoService = inject(ProductoService);
  private authService = inject(AuthService);
  private uploadService = inject(UploadService);

  @ViewChild('logoInput') logoInput!: ElementRef<HTMLInputElement>;
  @ViewChild('bannerInput') bannerInput!: ElementRef<HTMLInputElement>;

  tienda: Tienda | null = null;
  productos: Producto[] = [];
  loading = true;
  errorMessage = '';
  successMessage = '';

  // Estados de edición inline
  editingNombre = false;
  editingDescripcion = false;
  editingContacto = false;

  // Valores temporales para edición
  tempNombre = '';
  tempDescripcion = '';
  tempWhatsApp = '';
  tempInstagram = '';

  // Estados de carga de imágenes
  uploadingLogo = false;
  uploadingBanner = false;
  savingChanges = false;

  // Modal de vista previa de producto
  mostrarModalPreview = false;
  productoPreview: Producto | null = null;
  imagenActualIndex = 0;

  get imagenesProductoPreview(): string[] {
    if (!this.productoPreview) return [];
    return this.productoPreview.imagenes || [this.productoPreview.imagenUrl];
  }

  get imagenActualPreview(): string {
    return this.imagenesProductoPreview[this.imagenActualIndex] || 'assets/placeholder.jpg';
  }

  ngOnInit(): void {
    const currentUser = this.authService.currentUser();
    if (currentUser?.tiendaId) {
      this.loadTienda(currentUser.tiendaId);
      this.loadProductos();
    }
  }

  loadTienda(tiendaId: number): void {
    this.tiendaService.getTiendaById(tiendaId).subscribe({
      next: (tienda) => {
        this.tienda = tienda;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar la tienda';
        this.loading = false;
      }
    });
  }

  loadProductos(): void {
    this.productoService.getMisProductos().subscribe({
      next: (productos: Producto[]) => {
        this.productos = productos;
      },
      error: (err: any) => {
        console.error('Error al cargar productos:', err);
      }
    });
  }

  getSubdominioUrl(): string {
    if (this.tienda) {
      return `${window.location.protocol}//${this.tienda.subdominio}.${window.location.hostname}`;
    }
    return '';
  }

  // ==================== Métodos para edición de imágenes ====================

  triggerLogoUpload(): void {
    this.logoInput.nativeElement.click();
  }

  triggerBannerUpload(): void {
    this.bannerInput.nativeElement.click();
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!this.validateImageFile(file)) return;

    this.uploadLogo(file);
  }

  onBannerSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!this.validateImageFile(file)) return;

    this.uploadBanner(file);
  }

  private validateImageFile(file: File): boolean {
    const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!validTypes.includes(file.type)) {
      this.showError('Solo se permiten imágenes JPG, PNG, GIF o WebP');
      return false;
    }

    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.showError('La imagen no puede superar los 5MB');
      return false;
    }

    return true;
  }

  private uploadLogo(file: File): void {
    this.uploadingLogo = true;
    this.clearMessages();

    this.uploadService.uploadImage(file, 'tiendas/logos').subscribe({
      next: (response) => {
        if (this.tienda) {
          this.tienda.logoUrl = response.url;
          this.saveImageToTienda('logo', response.url);
        }
      },
      error: (err) => {
        this.showError(err.error?.message || 'Error al subir el logo');
        this.uploadingLogo = false;
      }
    });
  }

  private uploadBanner(file: File): void {
    this.uploadingBanner = true;
    this.clearMessages();

    this.uploadService.uploadImage(file, 'tiendas/banners').subscribe({
      next: (response) => {
        if (this.tienda) {
          this.tienda.bannerUrl = response.url;
          this.saveImageToTienda('banner', response.url);
        }
      },
      error: (err) => {
        this.showError(err.error?.message || 'Error al subir el banner');
        this.uploadingBanner = false;
      }
    });
  }

  private saveImageToTienda(tipo: 'logo' | 'banner', url: string): void {
    if (!this.tienda) return;

    const updateData = tipo === 'logo'
      ? { logoUrl: url }
      : { bannerUrl: url };

    this.tiendaService.actualizarTienda(this.tienda.id, updateData).subscribe({
      next: () => {
        this.showSuccess(`${tipo === 'logo' ? 'Logo' : 'Banner'} actualizado correctamente`);
        this.uploadingLogo = false;
        this.uploadingBanner = false;
      },
      error: () => {
        this.showError('Error al guardar la imagen');
        this.uploadingLogo = false;
        this.uploadingBanner = false;
      }
    });
  }

  // ==================== Métodos para edición de texto ====================

  startEditingNombre(): void {
    if (this.tienda) {
      this.tempNombre = this.tienda.nombre;
      this.editingNombre = true;
    }
  }

  cancelEditingNombre(): void {
    this.editingNombre = false;
    this.tempNombre = '';
  }

  saveNombre(): void {
    if (!this.tienda || !this.tempNombre.trim()) return;

    if (this.tempNombre.trim().length < 3) {
      this.showError('El nombre debe tener al menos 3 caracteres');
      return;
    }

    this.savingChanges = true;
    this.tiendaService.actualizarTienda(this.tienda.id, { nombre: this.tempNombre.trim() }).subscribe({
      next: () => {
        if (this.tienda) {
          this.tienda.nombre = this.tempNombre.trim();
        }
        this.editingNombre = false;
        this.savingChanges = false;
        this.showSuccess('Nombre actualizado correctamente');
      },
      error: () => {
        this.showError('Error al actualizar el nombre');
        this.savingChanges = false;
      }
    });
  }

  startEditingDescripcion(): void {
    if (this.tienda) {
      this.tempDescripcion = this.tienda.descripcion || '';
      this.editingDescripcion = true;
    }
  }

  cancelEditingDescripcion(): void {
    this.editingDescripcion = false;
    this.tempDescripcion = '';
  }

  saveDescripcion(): void {
    if (!this.tienda) return;

    this.savingChanges = true;
    this.tiendaService.actualizarTienda(this.tienda.id, { descripcion: this.tempDescripcion.trim() }).subscribe({
      next: () => {
        if (this.tienda) {
          this.tienda.descripcion = this.tempDescripcion.trim();
        }
        this.editingDescripcion = false;
        this.savingChanges = false;
        this.showSuccess('Descripción actualizada correctamente');
      },
      error: () => {
        this.showError('Error al actualizar la descripción');
        this.savingChanges = false;
      }
    });
  }

  startEditingContacto(): void {
    if (this.tienda) {
      this.tempWhatsApp = this.tienda.telefonoWhatsApp || '';
      this.tempInstagram = this.tienda.linkInstagram || '';
      this.editingContacto = true;
    }
  }

  cancelEditingContacto(): void {
    this.editingContacto = false;
    this.tempWhatsApp = '';
    this.tempInstagram = '';
  }

  saveContacto(): void {
    if (!this.tienda) return;

    this.savingChanges = true;
    this.tiendaService.actualizarTienda(this.tienda.id, {
      telefonoWhatsApp: this.tempWhatsApp.trim() || undefined,
      linkInstagram: this.tempInstagram.trim() || undefined
    }).subscribe({
      next: () => {
        if (this.tienda) {
          this.tienda.telefonoWhatsApp = this.tempWhatsApp.trim() || undefined;
          this.tienda.linkInstagram = this.tempInstagram.trim() || undefined;
        }
        this.editingContacto = false;
        this.savingChanges = false;
        this.showSuccess('Datos de contacto actualizados correctamente');
      },
      error: () => {
        this.showError('Error al actualizar los datos de contacto');
        this.savingChanges = false;
      }
    });
  }

  // ==================== Métodos auxiliares ====================

  private showSuccess(message: string): void {
    this.successMessage = message;
    this.errorMessage = '';
    setTimeout(() => this.successMessage = '', 4000);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.successMessage = '';
    setTimeout(() => this.errorMessage = '', 5000);
  }

  private clearMessages(): void {
    this.successMessage = '';
    this.errorMessage = '';
  }

  // ==================== Métodos para productos ====================

  verDetalleProducto(producto: Producto): void {
    this.productoPreview = producto;
    this.imagenActualIndex = 0;
    this.mostrarModalPreview = true;
  }

  cerrarModalPreview(): void {
    this.mostrarModalPreview = false;
    this.productoPreview = null;
    this.imagenActualIndex = 0;
  }

  anteriorImagenPreview(): void {
    if (this.imagenActualIndex > 0) {
      this.imagenActualIndex--;
    } else {
      this.imagenActualIndex = this.imagenesProductoPreview.length - 1;
    }
  }

  siguienteImagenPreview(): void {
    if (this.imagenActualIndex < this.imagenesProductoPreview.length - 1) {
      this.imagenActualIndex++;
    } else {
      this.imagenActualIndex = 0;
    }
  }

  seleccionarImagenPreview(index: number): void {
    this.imagenActualIndex = index;
  }

  eliminarProducto(producto: Producto): void {
    if (!confirm(`¿Estás seguro de que deseas eliminar "${producto.nombre}"?`)) {
      return;
    }

    this.productoService.deleteProducto(producto.id).subscribe({
      next: () => {
        this.productos = this.productos.filter(p => p.id !== producto.id);
        this.showSuccess('Producto eliminado correctamente');
      },
      error: (err) => {
        console.error('Error al eliminar producto:', err);
        this.showError(err.error?.message || 'Error al eliminar el producto');
      }
    });
  }
}
