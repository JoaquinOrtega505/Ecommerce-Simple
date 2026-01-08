# E-Commerce Frontend - Angular 17

Frontend desarrollado con Angular 17 + Bootstrap 5 para el sistema de e-commerce.

## Tecnologías

- **Angular 17** - Framework principal (Standalone Components)
- **Bootstrap 5** - Framework CSS
- **Bootstrap Icons** - Iconografía
- **RxJS** - Programación reactiva
- **TypeScript** - Lenguaje tipado

## Características

### Autenticación
- Login y registro de usuarios
- Autenticación JWT
- Guards para protección de rutas
- Interceptor HTTP para agregar token automáticamente

### Productos
- Lista de productos con filtros por categoría y búsqueda
- Detalle de producto con selector de cantidad
- Panel de administración (solo Admin)

### Carrito de Compras
- Agregar/eliminar productos
- Actualizar cantidades
- Persistencia en el servidor

### Checkout y Pago
- Formulario de datos de envío
- **3 métodos de pago simulados:**
  - Tarjeta de Crédito/Débito
  - MercadoPago
  - Transferencia Bancaria
- Validaciones de formularios

### Pedidos
- Listado de pedidos del usuario
- Detalle de pedido con timeline de estados
- Estados: Pendiente → Procesando → Enviado → Entregado

### Administración (Solo Admin)
- CRUD completo de productos
- Creación de categorías
- Gestión de stock
- Activar/desactivar productos

## Estructura del Proyecto

```
src/app/
├── core/                      # Servicios centrales
│   ├── guards/               # Guards de autenticación
│   ├── interceptors/         # HTTP interceptors
│   └── services/             # Servicios de API
│
├── shared/                    # Recursos compartidos
│   ├── components/           # Componentes compartidos (navbar)
│   └── models/               # Interfaces y modelos
│
├── features/                  # Módulos por funcionalidad
│   ├── auth/                 # Autenticación
│   │   └── components/       # Login y Registro
│   ├── productos/            # Productos
│   │   └── components/       # Lista, Detalle, Admin
│   ├── carrito/              # Carrito de compras
│   │   └── components/       # Vista de carrito
│   └── pedidos/              # Pedidos
│       └── components/       # Lista, Detalle, Checkout
│
└── environments/              # Configuración de entornos
```

## Instalación

### Requisitos Previos
- Node.js 18+
- Angular CLI 17
- Backend ejecutándose en `http://localhost:5090`

### Pasos de Instalación

1. **Navegar a la carpeta del frontend**
```bash
cd frontend/ecommerce-app
```

2. **Instalar dependencias**
```bash
npm install
```

3. **Configurar la URL del backend** (opcional)

Si tu backend está en otra URL, edita `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5090/api'  // Cambiar si es necesario
};
```

4. **Ejecutar la aplicación**
```bash
ng serve
```

5. **Abrir en el navegador**
```
http://localhost:4200
```

## 👤 Credenciales de Prueba

### Usuario Admin
- **Email:** admin@ecommerce.com
- **Password:** Admin123!
- **Permisos:** Administración completa

### Usuario Cliente (crear nuevo)
- Registrarse desde `/register`
- **Permisos:** Comprar productos, ver pedidos

## 🎯 Rutas Principales

| Ruta | Descripción | Requiere Auth | Requiere Admin |
|------|-------------|---------------|----------------|
| `/` | Redirige a productos | No | No |
| `/login` | Iniciar sesión | No | No |
| `/register` | Registrarse | No | No |
| `/productos` | Lista de productos | No | No |
| `/productos/:id` | Detalle de producto | No | No |
| `/carrito` | Carrito de compras | Sí | No |
| `/checkout` | Finalizar compra | Sí | No |
| `/pedidos` | Mis pedidos | Sí | No |
| `/pedidos/:id` | Detalle de pedido | Sí | No |
| `/admin/productos` | Administración | Sí | Sí |

## Scripts Disponibles

```bash
# Desarrollo
ng serve                    # Iniciar servidor de desarrollo
ng serve --open            # Iniciar y abrir navegador

# Build
ng build                   # Build de producción
ng build --configuration development  # Build de desarrollo

# Tests
ng test                    # Ejecutar tests unitarios
ng e2e                     # Ejecutar tests e2e

# Análisis
ng lint                    # Verificar código con ESLint
```

## Componentes Principales

### Servicios

#### AuthService
```typescript
- login(credentials): Observable<AuthResponse>
- register(userData): Observable<AuthResponse>
- logout(): void
- currentUser$: Observable<Usuario | null>
- isAuthenticated: boolean
- isAdmin: boolean
```

#### ProductoService
```typescript
- getProductos(): Observable<Producto[]>
- getProductoById(id): Observable<Producto>
- createProducto(producto): Observable<Producto>
- updateProducto(id, producto): Observable<Producto>
- deleteProducto(id): Observable<void>
```

#### CarritoService
```typescript
- getCarrito(): Observable<CarritoItem[]>
- agregarAlCarrito(item): Observable<CarritoItem>
- actualizarCantidad(itemId, cantidad): Observable<CarritoItem>
- eliminarItem(itemId): Observable<void>
- vaciarCarrito(): Observable<void>
- carritoItems$: Observable<CarritoItem[]>
```

#### PedidoService
```typescript
- getPedidos(): Observable<Pedido[]>
- getPedidoById(id): Observable<Pedido>
- crearPedido(pedido): Observable<Pedido>
- actualizarEstado(id, estado): Observable<Pedido>
```

## Personalización

### Cambiar Colores (Bootstrap)
Edita `src/styles.scss` para personalizar los colores de Bootstrap:

```scss
$primary: #your-color;
$secondary: #your-color;

@import 'bootstrap/scss/bootstrap';
```

### Agregar Nuevos Componentes
```bash
ng generate component features/nueva-feature/components/mi-componente --skip-tests
```

## Seguridad

- **JWT Token**: Almacenado en localStorage
- **HTTP Interceptor**: Agrega automáticamente el token a todas las peticiones
- **Auth Guard**: Protege rutas que requieren autenticación
- **Admin Guard**: Protege rutas de administración
- **Validación de formularios**: Todos los formularios tienen validaciones

## Solución de Problemas

### Error de CORS
Si ves errores de CORS, asegúrate de que el backend tenga configurado CORS para `http://localhost:4200`

### Token expirado
Si el token JWT expira, el usuario será redirigido al login automáticamente.

### Puerto ocupado
Si el puerto 4200 está ocupado:
```bash
ng serve --port 4300
```

## Notas de Desarrollo

- **Standalone Components**: Este proyecto usa la nueva arquitectura de Angular 17
- **Lazy Loading**: Todos los componentes usan lazy loading para optimizar la carga inicial
- **RxJS**: Se usa programación reactiva para el manejo de estados
- **Bootstrap 5**: No requiere jQuery

## Mejoras Futuras

- [ ] Integración real con Stripe/MercadoPago
- [ ] Sistema de notificaciones en tiempo real
- [ ] Búsqueda avanzada con filtros
- [ ] Sistema de favoritos
- [ ] Reseñas y calificaciones de productos
- [ ] Historial de búsquedas
- [ ] Modo oscuro
- [ ] PWA (Progressive Web App)
- [ ] Internacionalización (i18n)


