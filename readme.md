# E-Commerce Fullstack 🛒

Sistema de comercio electrónico completo desarrollado con **ASP.NET Core 9** (Backend) y **Angular 17** (Frontend).

## 📸 Capturas

> Sistema completo con autenticación, gestión de productos, carrito de compras, checkout con múltiples métodos de pago y panel de administración.

## ✨ Características Principales

### Backend (API REST)
- ✅ Autenticación y registro con JWT
- ✅ CRUD completo de productos y categorías
- ✅ Sistema de carrito de compras persistente
- ✅ Gestión de pedidos con estados
- ✅ Control automático de stock
- ✅ Roles de usuario (Admin/Cliente)
- ✅ Documentación con Swagger/OpenAPI
- ✅ Hash de contraseñas con BCrypt

### Frontend (SPA)
- ✅ Interfaz moderna con Angular 17 + Bootstrap 5
- ✅ Autenticación con guards y interceptors
- ✅ Lista de productos con filtros y búsqueda
- ✅ Carrito de compras interactivo
- ✅ Checkout con 3 métodos de pago simulados:
  - Tarjeta de Crédito/Débito
  - MercadoPago
  - Transferencia Bancaria
- ✅ Seguimiento de pedidos con timeline
- ✅ Panel de administración completo
- ✅ Diseño responsive
- ✅ Lazy loading de componentes

## 🚀 Stack Tecnológico

### Backend
- **ASP.NET Core 9** - Framework web
- **Entity Framework Core 9** - ORM
- **PostgreSQL 15+** - Base de datos
- **JWT Bearer** - Autenticación
- **BCrypt** - Hash de contraseñas
- **Swagger** - Documentación API

### Frontend
- **Angular 17** - Framework SPA (Standalone Components)
- **Bootstrap 5** - Framework CSS
- **Bootstrap Icons** - Iconografía
- **RxJS** - Programación reactiva
- **TypeScript** - Lenguaje tipado

## 📁 Estructura del Proyecto

```
Ecommerce-Simple/
├── backend/
│   └── EcommerceApi/
│       ├── Controllers/        # Endpoints de la API
│       ├── Models/            # Entidades del dominio
│       ├── DTOs/              # Data Transfer Objects
│       ├── Data/              # DbContext y configuración
│       ├── Migrations/        # Migraciones de BD
│       └── Program.cs         # Configuración principal
│
├── frontend/
│   └── ecommerce-app/
│       ├── src/app/
│       │   ├── core/          # Servicios centrales
│       │   ├── shared/        # Componentes compartidos
│       │   ├── features/      # Módulos por funcionalidad
│       │   └── environments/  # Configuración de entornos
│       └── package.json
│
└── README.md
```

## 🛠️ Instalación y Configuración

### Requisitos Previos
- ✅ .NET 9 SDK
- ✅ Node.js 18+
- ✅ PostgreSQL 15+
- ✅ Angular CLI 17

### 1. Clonar el Repositorio
```bash
git clone https://github.com/JoaquinOrtega505/Ecommerce.git
cd Ecommerce-Simple
```

### 2. Configurar Backend

#### Crear Base de Datos
```bash
psql -U postgres
CREATE DATABASE ecommerce_db;
\q
```

#### Configurar Conexión
Crear `backend/EcommerceApi/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce_db;Username=postgres;Password=TU_PASSWORD"
  },
  "Jwt": {
    "Key": "tu_clave_secreta_super_segura_de_al_menos_32_caracteres",
    "Issuer": "EcommerceApi",
    "Audience": "EcommerceClient"
  }
}
```

#### Aplicar Migraciones y Ejecutar
```bash
cd backend/EcommerceApi
dotnet tool install --global dotnet-ef
dotnet ef database update
dotnet run
```

**Backend disponible en:** `http://localhost:5090`
**Swagger:** `http://localhost:5090/swagger`

### 3. Configurar Frontend

```bash
cd frontend/ecommerce-app
npm install
ng serve
```

**Frontend disponible en:** `http://localhost:4200`

## 👤 Credenciales de Prueba

### Usuario Admin
- **Email:** `admin@ecommerce.com`
- **Password:** `Admin123!`
- **Permisos:** Acceso completo al panel de administración

### Usuario Cliente
- Crear nuevo usuario desde `/register`
- **Permisos:** Comprar productos, gestionar carrito, ver pedidos

## 📡 API Endpoints

### Autenticación
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/auth/register` | Registrar usuario |
| POST | `/api/auth/login` | Iniciar sesión (retorna JWT) |

### Productos
| Método | Endpoint | Descripción | Requiere |
|--------|----------|-------------|----------|
| GET | `/api/productos` | Listar todos | - |
| GET | `/api/productos/{id}` | Obtener por ID | - |
| POST | `/api/productos` | Crear producto | Admin |
| PUT | `/api/productos/{id}` | Actualizar | Admin |
| DELETE | `/api/productos/{id}` | Eliminar | Admin |

### Categorías
| Método | Endpoint | Descripción | Requiere |
|--------|----------|-------------|----------|
| GET | `/api/categorias` | Listar todas | - |
| GET | `/api/categorias/{id}` | Obtener por ID | - |
| POST | `/api/categorias` | Crear categoría | Admin |
| PUT | `/api/categorias/{id}` | Actualizar | Admin |
| DELETE | `/api/categorias/{id}` | Eliminar | Admin |

### Carrito
| Método | Endpoint | Descripción | Requiere |
|--------|----------|-------------|----------|
| GET | `/api/carrito` | Ver carrito actual | Auth |
| POST | `/api/carrito` | Agregar producto | Auth |
| PUT | `/api/carrito/{itemId}` | Actualizar cantidad | Auth |
| DELETE | `/api/carrito/{itemId}` | Eliminar producto | Auth |
| DELETE | `/api/carrito` | Vaciar carrito | Auth |

### Pedidos
| Método | Endpoint | Descripción | Requiere |
|--------|----------|-------------|----------|
| GET | `/api/pedidos` | Listar mis pedidos | Auth |
| GET | `/api/pedidos/{id}` | Obtener detalle | Auth |
| POST | `/api/pedidos` | Crear pedido | Auth |
| PUT | `/api/pedidos/{id}/estado` | Actualizar estado | Admin |

## 🎯 Rutas del Frontend

| Ruta | Componente | Requiere Auth | Requiere Admin |
|------|------------|---------------|----------------|
| `/` | Redirect → `/productos` | No | No |
| `/login` | Login | No | No |
| `/register` | Registro | No | No |
| `/productos` | Lista de Productos | No | No |
| `/productos/:id` | Detalle Producto | No | No |
| `/carrito` | Carrito de Compras | Sí | No |
| `/checkout` | Finalizar Compra | Sí | No |
| `/pedidos` | Mis Pedidos | Sí | No |
| `/pedidos/:id` | Detalle de Pedido | Sí | No |
| `/admin/productos` | Admin Productos | Sí | Sí |

## 🗄️ Modelo de Datos

### Usuario
```typescript
{
  id: number
  nombre: string
  email: string
  passwordHash: string
  rol: 'Admin' | 'Cliente'
}
```

### Producto
```typescript
{
  id: number
  nombre: string
  descripcion: string
  precio: number
  stock: number
  imagenUrl: string
  activo: boolean
  categoriaId: number
}
```

### Pedido
```typescript
{
  id: number
  usuarioId: number
  total: number
  estado: 'Pendiente' | 'Procesando' | 'Enviado' | 'Entregado' | 'Cancelado'
  direccionEnvio: string
  fechaCreacion: Date
  items: PedidoItem[]
}
```

## 🔐 Seguridad

- **JWT Authentication**: Tokens firmados con HS256
- **Password Hashing**: BCrypt con salt automático
- **HTTP Interceptor**: Agrega token JWT automáticamente
- **Guards**: Protección de rutas en frontend
- **CORS**: Configurado para desarrollo local
- **Validación**: En backend y frontend

## 🧪 Testing

### Backend
```bash
cd backend/EcommerceApi
dotnet test
```

### Frontend
```bash
cd frontend/ecommerce-app
ng test
```

## 📦 Build para Producción

### Backend
```bash
cd backend/EcommerceApi
dotnet publish -c Release -o ./publish
```

### Frontend
```bash
cd frontend/ecommerce-app
ng build --configuration production
```

## 🚀 Despliegue

### Opciones de Hosting

**Backend:**
- Azure App Service
- AWS Elastic Beanstalk
- Heroku
- Railway

**Frontend:**
- Vercel
- Netlify
- Firebase Hosting
- Azure Static Web Apps

**Base de Datos:**
- Azure Database for PostgreSQL
- AWS RDS
- ElephantSQL
- Supabase

## 🔮 Roadmap / Mejoras Futuras

- [ ] Integración real con Stripe/MercadoPago
- [ ] Sistema de notificaciones push
- [ ] Chat de soporte en vivo
- [ ] Sistema de reseñas y calificaciones
- [ ] Wishlist / Lista de deseos
- [ ] Comparador de productos
- [ ] Sistema de cupones y descuentos
- [ ] Búsqueda avanzada con Elasticsearch
- [ ] Panel de analytics para admin
- [ ] Exportación de reportes (PDF/Excel)
- [ ] Multi-idioma (i18n)
- [ ] Modo oscuro
- [ ] PWA (Progressive Web App)
- [ ] Sistema de recomendaciones con ML

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Ver archivo `LICENSE` para más detalles.

## 👨‍💻 Autor

**Joaquín Ortega**
- GitHub: [@JoaquinOrtega505](https://github.com/JoaquinOrtega505)

## 📞 Soporte

Si encuentras algún problema o tienes sugerencias, por favor abre un issue en GitHub.

---

⭐ Si te gustó este proyecto, ¡dale una estrella en GitHub!
