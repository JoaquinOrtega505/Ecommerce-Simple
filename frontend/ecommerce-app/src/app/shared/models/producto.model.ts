export interface Producto {
  id: number;
  nombre: string;
  descripcion: string;
  precio: number;
  stock: number;
  imagenUrl: string;
  activo: boolean;
  categoriaId: number;
  categoria?: Categoria;
}

export interface Categoria {
  id: number;
  nombre: string;
  descripcion: string;
}

export interface ProductoCreateDto {
  nombre: string;
  descripcion: string;
  precio: number;
  stock: number;
  imagenUrl: string;
  categoriaId: number;
}
