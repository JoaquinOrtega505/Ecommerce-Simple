export interface Usuario {
  id: number;
  nombre: string;
  email: string;
  rol: 'Admin' | 'Cliente' | 'Deposito';
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  nombre: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  usuarioId: number;
  nombre: string;
  email: string;
  rol: 'Admin' | 'Cliente' | 'Deposito';
}
