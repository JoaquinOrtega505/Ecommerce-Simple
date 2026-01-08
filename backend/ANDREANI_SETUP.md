# Guía de Integración con Andreani

Esta guía te ayudará a integrar tu e-commerce con la API de Andreani para gestión de envíos.

## Paso 1: Crear Cuenta en Andreani

### 1.1 Registro en el Portal de Desarrolladores

1. Visita: https://www.andreani.com/
2. Busca la sección "Soluciones para E-commerce" o "Developers"
3. Haz clic en "Crear cuenta" o "Solicitar acceso a la API"
4. Completa el formulario con:
   - Datos de tu empresa
   - Email de contacto
   - Tipo de negocio (E-commerce)

### 1.2 Solicitar Credenciales de Sandbox

Una vez registrado:

1. Ingresa al portal de desarrolladores
2. Solicita credenciales para el **ambiente de pruebas (Sandbox)**
3. Recibirás por email:
   - **Username** (usuario API)
   - **Password** (contraseña API)
   - **Número de Contrato** (contract number)

**IMPORTANTE:** Las credenciales de sandbox son diferentes a las de producción.

## Paso 2: Configurar Credenciales en tu Proyecto

### 2.1 Actualizar `appsettings.json`

Abre el archivo `backend/EcommerceApi/appsettings.json` y reemplaza los valores de la sección `Andreani`:

```json
{
  "Andreani": {
    "ApiUrl": "https://sandbox.andreani.com/v2",
    "Username": "tu_usuario_sandbox",
    "Password": "tu_password_sandbox",
    "ContractNumber": "tu_numero_contrato",
    "RemitenteNombre": "Tu Empresa SRL",
    "RemitenteEmail": "envios@tuempresa.com",
    "RemitenteDocumento": "30123456789",
    "RemitenteTelefono": "1145678900",
    "CodigoPostalOrigen": "1414",
    "CalleOrigen": "Av. Córdoba",
    "NumeroOrigen": "1500",
    "LocalidadOrigen": "CABA",
    "ProvinciaOrigen": "Buenos Aires"
  }
}
```

**Datos a completar:**
- `Username`, `Password`, `ContractNumber`: Los recibirás de Andreani
- `RemitenteNombre`, `RemitenteEmail`, etc.: Datos de tu empresa (desde dónde envías)

### 2.2 Variables de Entorno (Recomendado para Producción)

Para mayor seguridad, NO guardes las credenciales directamente en `appsettings.json` en producción.

**Opción A: Variables de entorno**
```bash
export Andreani__Username="tu_usuario"
export Andreani__Password="tu_password"
export Andreani__ContractNumber="tu_contrato"
```

**Opción B: User Secrets (desarrollo)**
```bash
cd backend/EcommerceApi
dotnet user-secrets init
dotnet user-secrets set "Andreani:Username" "tu_usuario"
dotnet user-secrets set "Andreani:Password" "tu_password"
dotnet user-secrets set "Andreani:ContractNumber" "tu_contrato"
```

## Paso 3: Probar la Integración

### 3.1 Verificar que el servicio está registrado

El servicio ya está configurado en `Program.cs`. Verifica que esta línea exista:

```csharp
builder.Services.AddHttpClient<EcommerceApi.Services.AndreaniService>();
```

### 3.2 Crear un envío de prueba

Puedes usar Postman o curl para probar:

```bash
# Primero, obtén un token de autenticación (como Admin)
TOKEN="tu_token_jwt_aqui"

# Luego, crea un envío de prueba
curl -X POST http://localhost:5090/api/envios/crear \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pedidoId": 1,
    "codigoPostalDestino": "1406",
    "calleDestino": "Av. Santa Fe",
    "numeroDestino": "1234",
    "localidadDestino": "CABA",
    "provinciaDestino": "Buenos Aires",
    "pesoKg": 1.5,
    "volumenM3": 0.01,
    "valorDeclarado": 5000,
    "destinatarioNombre": "Juan Pérez",
    "destinatarioEmail": "juan@example.com",
    "destinatarioDocumento": "12345678",
    "destinatarioTelefono": "1145678901",
    "descripcion": "Notebook Lenovo"
  }'
```

## Paso 4: Configurar Webhook de Andreani

Para que Andreani notifique automáticamente cuando un pedido es entregado:

### 4.1 En el Panel de Andreani

1. Ingresa al portal de desarrolladores de Andreani
2. Ve a "Configuración" → "Webhooks"
3. Agrega un nuevo webhook:
   - **URL:** `https://tu-dominio.com/api/webhook/entrega`
   - **Eventos:** Selecciona "Entrega completada"
   - **Secret:** Usa el mismo valor de `Webhook:SecretKey` en tu appsettings.json

### 4.2 Formato del Webhook de Andreani

Andreani enviará algo similar a esto cuando entregue un pedido:

```json
{
  "numeroAndreani": "AND-2026-001234",
  "numeroDeOrden": "123",
  "estado": "Entregado",
  "fechaEntrega": "2026-01-08T14:30:00Z",
  "receptor": {
    "nombre": "Juan Pérez",
    "documento": "12345678"
  }
}
```

### 4.3 Mapeo al Webhook Genérico

Necesitarás transformar los datos de Andreani al formato de tu webhook genérico. Esto se hace automáticamente si usas el adaptador incluido.

## Paso 5: URLs de Andreani

### Sandbox (Pruebas)
- **API Base:** `https://sandbox.andreani.com/v2`
- **Portal:** Solicita acceso a través de tu ejecutivo de cuenta

### Producción
- **API Base:** `https://api.andreani.com/v2`
- **Documentación:** https://developers.andreani.com/

## Paso 6: Funcionalidades Disponibles

Una vez configurado, podrás:

### ✅ Crear Envíos
- Crear un envío en Andreani al marcar un pedido como "Pagado"
- Obtener número de seguimiento automáticamente

### ✅ Tracking
- Consultar el estado del envío en tiempo real
- Obtener historial de eventos (despacho, en tránsito, entregado)

### ✅ Etiquetas
- Descargar etiquetas de envío en PDF
- Imprimir directamente desde el panel de depósito

### ✅ Webhooks
- Recibir notificaciones automáticas de entrega
- Actualizar estado del pedido sin intervención manual

## Códigos Postales de Ejemplo para Testing

Algunos CP para probar en el sandbox:

- **CABA:** 1414, 1406, 1425, 1428
- **GBA Norte:** 1636 (Olivos), 1642 (San Isidro)
- **GBA Sur:** 1826 (Remedios de Escalada), 1832 (Lomas de Zamora)
- **Interior:** 5000 (Córdoba), 2000 (Rosario), 7600 (Mar del Plata)

## Errores Comunes

### Error: "Unauthorized" / "Invalid credentials"
- Verifica que Username y Password sean correctos
- Asegúrate de estar usando las credenciales de **sandbox** (no producción)
- Verifica que el Contract Number sea válido

### Error: "Invalid postal code"
- Algunos códigos postales no están disponibles en sandbox
- Usa los códigos de ejemplo listados arriba

### Error: "Invalid weight" o "Invalid volume"
- El peso debe ser en kilogramos (mínimo 0.1 kg)
- El volumen debe ser en metros cúbicos (mínimo 0.001 m³)

### Webhook no recibe notificaciones
- Verifica que la URL sea accesible públicamente (no localhost)
- Para desarrollo local, usa ngrok: `ngrok http 5090`
- Verifica que el Secret Key coincida

## Próximos Pasos

1. **Registrarte** en el portal de Andreani
2. **Solicitar credenciales** de sandbox
3. **Configurar** las credenciales en `appsettings.json`
4. **Probar** creando un envío de prueba
5. **Configurar webhook** para recibir notificaciones automáticas

## Soporte

- **Documentación oficial:** https://developers.andreani.com/
- **Soporte técnico:** Contacta a tu ejecutivo de cuenta Andreani
- **Consultas del proyecto:** Revisa el archivo `WEBHOOK_README.md`

---

**NOTA IMPORTANTE:** Esta integración está preparada pero requiere que completes el registro en Andreani y obtengas las credenciales. Mientras tanto, puedes usar el webhook genérico con simulaciones manuales.
