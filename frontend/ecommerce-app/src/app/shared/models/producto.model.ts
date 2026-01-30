export interface Producto {
  id: number;
  nombre: string;
  descripcion: string;
  precio: number;
  stock: number;
  imagenUrl: string;
  imagenUrl2?: string;
  imagenUrl3?: string;
  imagenes?: string[];
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
  imagenUrl2?: string;
  imagenUrl3?: string;
  categoriaId: number;
}
