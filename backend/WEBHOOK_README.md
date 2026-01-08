# Webhook de Confirmación de Entrega

Este documento explica cómo integrar servicios de envío externos con tu sistema de e-commerce usando webhooks.

## Descripción General

El sistema incluye un webhook genérico que permite a los servicios de envío (Andreani, OCA, Correo Argentino, etc.) notificar automáticamente cuando un pedido ha sido entregado.

## Endpoint del Webhook

```
POST /api/webhook/entrega
```

**URL Completa:** `https://tu-dominio.com/api/webhook/entrega`

## Autenticación

El webhook utiliza un `secretKey` para validar que las peticiones provienen de servicios autorizados.

La clave secreta se configura en `appsettings.json`:

```json
{
  "Webhook": {
    "SecretKey": "tu_clave_secreta_aqui"
  }
}
```

**IMPORTANTE:** Cambia esta clave en producción por una clave segura y única.

## Formato de la Petición

### Headers
```
Content-Type: application/json
```

### Body (JSON)

```json
{
  "pedidoId": 123,
  "estado": "entregado",
  "servicioEnvio": "Andreani",
  "numeroSeguimiento": "AND-2026-001234",
  "fechaEntrega": "2026-01-07T15:30:00Z",
  "observaciones": "Entregado correctamente",
  "receptorNombre": "Juan Pérez",
  "receptorDocumento": "12345678",
  "secretKey": "webhook_secret_key_2026_ecommerce_delivery",
  "metadataJson": "{\"latitud\": -34.6037, \"longitud\": -58.3816}"
}
```

### Campos

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `pedidoId` | integer | ✅ | ID del pedido en el sistema |
| `estado` | string | ✅ | Estado de la entrega: `entregado`, `en_transito`, `fallido` |
| `servicioEnvio` | string | ✅ | Nombre del servicio (ej: Andreani, OCA, Correo Argentino) |
| `numeroSeguimiento` | string | ❌ | Número de tracking del envío |
| `fechaEntrega` | datetime | ❌ | Fecha/hora de la entrega (ISO 8601) |
| `observaciones` | string | ❌ | Notas adicionales |
| `receptorNombre` | string | ❌ | Nombre de quien recibió el paquete |
| `receptorDocumento` | string | ❌ | DNI/Documento de quien recibió |
| `secretKey` | string | ✅ | Clave secreta para autenticación |
| `metadataJson` | string | ❌ | Datos adicionales en formato JSON |

## Estados Soportados

- **`entregado` / `delivered`**: Marca el pedido como entregado (solo si está en estado "Enviado")
- **`en_transito` / `in_transit`**: Información adicional, no cambia el estado principal
- **`fallido` / `failed`**: Registra un intento de entrega fallido

## Respuestas del Webhook

### Éxito (200 OK)
```json
{
  "message": "Notificación procesada correctamente",
  "pedidoId": 123,
  "nuevoEstado": "Entregado",
  "fechaProcesamiento": "2026-01-07T15:30:00Z"
}
```

### Error - Secret Key Inválido (401 Unauthorized)
```json
{
  "message": "Secret key inválido"
}
```

### Error - Pedido No Encontrado (404 Not Found)
```json
{
  "message": "Pedido no encontrado"
}
```

### Error - Estado Inválido (400 Bad Request)
```json
{
  "message": "El pedido debe estar en estado 'Enviado' para marcarse como entregado"
}
```

## Ejemplos de Integración

### cURL
```bash
curl -X POST https://tu-dominio.com/api/webhook/entrega \
  -H "Content-Type: application/json" \
  -d '{
    "pedidoId": 123,
    "estado": "entregado",
    "servicioEnvio": "Andreani",
    "numeroSeguimiento": "AND-2026-001234",
    "receptorNombre": "Juan Pérez",
    "secretKey": "tu_clave_secreta_aqui"
  }'
```

### JavaScript/Node.js
```javascript
const notificarEntrega = async (pedidoId, numeroSeguimiento) => {
  const response = await fetch('https://tu-dominio.com/api/webhook/entrega', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      pedidoId: pedidoId,
      estado: 'entregado',
      servicioEnvio: 'Andreani',
      numeroSeguimiento: numeroSeguimiento,
      fechaEntrega: new Date().toISOString(),
      secretKey: 'tu_clave_secreta_aqui'
    })
  });

  return await response.json();
};
```

### Python
```python
import requests
from datetime import datetime

def notificar_entrega(pedido_id, numero_seguimiento):
    url = 'https://tu-dominio.com/api/webhook/entrega'
    data = {
        'pedidoId': pedido_id,
        'estado': 'entregado',
        'servicioEnvio': 'Andreani',
        'numeroSeguimiento': numero_seguimiento,
        'fechaEntrega': datetime.utcnow().isoformat() + 'Z',
        'secretKey': 'tu_clave_secreta_aqui'
    }

    response = requests.post(url, json=data)
    return response.json()
```

## Integración con Servicios de Envío

### Andreani
1. Accede al panel de Andreani
2. Configura el webhook en "Configuración" → "Webhooks"
3. URL: `https://tu-dominio.com/api/webhook/entrega`
4. Mapea los campos de Andreani a los campos del webhook

### OCA
1. Contacta con tu ejecutivo de cuentas OCA
2. Solicita la configuración del webhook
3. Proporciona la URL y el mapeo de campos

### Correo Argentino
1. Accede al panel de Correo Argentino
2. Configura notificaciones automáticas
3. URL: `https://tu-dominio.com/api/webhook/entrega`

## Endpoint de Prueba

Para verificar que el webhook está funcionando:

```bash
curl http://localhost:5090/api/webhook/test
```

Respuesta esperada:
```json
{
  "status": "Webhook funcionando correctamente",
  "timestamp": "2026-01-07T15:30:00Z",
  "endpoints": {
    "entrega": "/api/webhook/entrega"
  }
}
```

## Logs y Monitoreo

Todos los eventos del webhook se registran en los logs de la aplicación:

- **Información**: Pedidos procesados correctamente
- **Advertencia**: Intentos con secret key inválido, estados inválidos
- **Error**: Errores internos del servidor

Ubicación de logs: `E:\Proyecto Programación\Portfolio-Proyectos-Fullstack\Ecommerce-Simple\backend\EcommerceApi\logs\`

## Seguridad

1. **HTTPS**: Siempre usa HTTPS en producción
2. **Secret Key**: Mantén la clave secreta en variables de entorno, no en el código
3. **Validación**: El webhook valida que el pedido exista y esté en el estado correcto
4. **Logs**: Revisa regularmente los intentos fallidos de autenticación

## Soporte

Para dudas o problemas con el webhook, contacta al equipo de desarrollo.

---

**Versión:** 1.0
**Última actualización:** Enero 2026
