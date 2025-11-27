# E-commerce Full Stack
Sistema de comercio electrónico desarrollado con ASP.NET Core 9 y PostgreSQL.

## Descripción
API REST completa para un e-commerce que incluye autenticación JWT, gestión de productos, categorías, carrito de compras y pedidos con control de roles (Admin y Cliente).

## Características Principales
- Autenticación y registro de usuarios con JWT
- CRUD de productos con categorías
- Sistema de carrito de compras persistente
- Gestión completa de pedidos
- Control automático de stock
- Roles de usuario (Admin/Cliente)
- API documentada con Swagger

## Stack Tecnológico
### Backend
- ASP.NET Core 9
- Entity Framework Core 9
- PostgreSQL 15+
- JWT Bearer Authentication
- BCrypt para hash de contraseñas
- Swagger/OpenAPI

## Estructura del Proyecto
Ecommerce/
├── backend/
│   └── EcommerceApi/
│       ├── Controllers/
│       ├── Models/
│       ├── DTOs/
│       ├── Data/
│       └── Program.cs
├── .gitignore
└── README.md

## Instalación Local
### Requisitos previos
- .NET 9 SDK
- PostgreSQL 15+


#### Clonar el repositorio
```bash
git clone https://github.com/JoaquinOrtega505/Ecommerce.git
cd Ecommerce

Crear la base de datos

sqlpsql -U postgres
CREATE DATABASE ecommerce_db;
\q

Configurar la conexión

Crear backend/EcommerceApi/appsettings.Development.json:
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce_db;Username=postgres;Password=TU_PASSWORD"
  }


Aplicar migraciones

bashcd backend/EcommerceApi
dotnet tool install --global dotnet-ef
dotnet ef database update

Ejecutar


dotnet run
La API estará disponible en:

http://localhost:5090
Swagger: http://localhost:5090/swagger

API Endpoints
Autenticación

POST /api/auth/register - Registrar usuario
POST /api/auth/login - Iniciar sesión

Categorías

GET /api/categorias - Listar todas
GET /api/categorias/{id} - Obtener por ID
POST /api/categorias - Crear (Admin)
PUT /api/categorias/{id} - Actualizar (Admin)
DELETE /api/categorias/{id} - Eliminar (Admin)

Productos

GET /api/productos - Listar todos
GET /api/productos/{id} - Obtener por ID
POST /api/productos - Crear (Admin)
PUT /api/productos/{id} - Actualizar (Admin)
DELETE /api/productos/{id} - Eliminar (Admin)

Carrito (Requiere autenticación)

GET /api/carrito - Ver carrito
POST /api/carrito - Agregar producto
PUT /api/carrito/{itemId} - Actualizar cantidad
DELETE /api/carrito/{itemId} - Eliminar producto
DELETE /api/carrito - Vaciar carrito

Pedidos (Requiere autenticación)

GET /api/pedidos - Listar pedidos
GET /api/pedidos/{id} - Obtener pedido
POST /api/pedidos - Crear pedido
PUT /api/pedidos/{id}/estado - Actualizar estado (Admin)

Credenciales de Prueba
Usuario Admin:
Email: admin@ecommerce.com
Password: Admin123!

Modelo de Datos

Usuario

Id
Nombre
Email
PasswordHash
Rol (Admin/Cliente)

Categoria

Id
Nombre
Descripcion

Producto

Id
Nombre
Descripcion
Precio
Stock
ImagenUrl
Activo
CategoriaId

CarritoItem

Id
UsuarioId
ProductoId
Cantidad

Pedido

Id
UsuarioId
Total
Estado
DireccionEnvio
FechaCreacion

PedidoItem

Id
PedidoId
ProductoId
Cantidad
PrecioUnitario
Subtotal

