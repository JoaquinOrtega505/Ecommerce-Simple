# Guía de Sistema de Envíos

## ✅ Sistema Listo para Usar - Sin Configuración Adicional

El sistema de envíos está **completamente funcional** sin necesidad de configurar credenciales de Andreani. Usa un servicio simulado que genera números de seguimiento reales y permite probar todo el flujo end-to-end.

## Funcionalidades Disponibles

### 1. Crear Envío Automático

Cuando un pedido está en estado "Pagado", puedes crear un envío:

**Endpoint:** `POST /api/shipping/crear/{pedidoId}`

**Autorización:** Admin o Deposito

**Ejemplo:**
```bash
curl -X POST http://localhost:5090/api/shipping/crear/3 \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```

**Respuesta:**
```json
{
  "message": "Envío creado exitosamente",
  "pedidoId": 3,
  "numeroSeguimiento": "ANDREANI-20260107-001001",
  "servicio": "Andreani (Simulado)",
  "urlTracking": "https://www.andreani.com/#!/personas/tracking/ANDREANI-20260107-001001",
  "etiquetaUrl": "/api/shipping/etiqueta/ANDREANI-20260107-001001"
}
```

**Qué hace:**
- ✅ Genera un número de seguimiento único
- ✅ Actualiza el pedido a estado "Enviado"
- ✅ Registra la fecha de despacho
- ✅ Asigna el servicio de envío

### 2. Consultar Tracking

Cualquier usuario puede consultar el tracking de un envío:

**Endpoint:** `GET /api/shipping/tracking/{numeroSeguimiento}`

**Ejemplo:**
```bash
curl http://localhost:5090/api/shipping/tracking/ANDREANI-20260107-001001 \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```

**Respuesta:**
```json
{
  "numeroSeguimiento": "ANDREANI-20260107-001001",
  "estadoActual": "En tránsito",
  "pedidoId": 3,
  "eventos": [
    {
      "fecha": "2026-01-06T23:00:00Z",
      "estado": "Ingresado",
      "descripcion": "El envío fue ingresado al sistema"
    },
    {
      "fecha": "2026-01-07T03:00:00Z",
      "estado": "En preparación",
      "descripcion": "El envío está siendo preparado"
    },
    {
      "fecha": "2026-01-07T11:00:00Z",
      "estado": "Despachado",
      "descripcion": "El envío fue despachado desde origen"
    },
    {
      "fecha": "2026-01-07T17:00:00Z",
      "estado": "En tránsito",
      "descripcion": "El envío está en camino"
    }
  ]
}
```

### 3. Descargar Etiqueta de Envío

Los usuarios de Deposito y Admin pueden descargar etiquetas:

**Endpoint:** `GET /api/shipping/etiqueta/{numeroSeguimiento}`

**Autorización:** Admin o Deposito

**Ejemplo:**
```bash
curl http://localhost:5090/api/shipping/etiqueta/ANDREANI-20260107-001001 \
  -H "Authorization: Bearer TU_TOKEN_AQUI" \
  --output etiqueta.pdf
```

**Devuelve:** Archivo PDF con la etiqueta de envío

### 4. Simular Entrega (Solo Admin - Para Testing)

Para pruebas, los Admin pueden simular que un pedido fue entregado:

**Endpoint:** `POST /api/shipping/simular-entrega/{numeroSeguimiento}`

**Autorización:** Solo Admin

**Ejemplo:**
```bash
curl -X POST http://localhost:5090/api/shipping/simular-entrega/ANDREANI-20260107-001001 \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```

**Qué hace:**
- ✅ Cambia el estado del pedido a "Entregado"
- ✅ Registra la fecha de entrega
- ✅ Simula el comportamiento del webhook real

## Flujo Completo de Uso

### Desde el Panel de Depósito:

1. **Ver pedidos pagados** 📦
   - Accede a `/deposito`
   - Verás todos los pedidos en estado "Pagado"

2. **Imprimir lista de productos** 🖨️
   - Click en "Imprimir Lista de Productos"
   - Se genera un documento para preparar el pedido

3. **Crear envío** 📮
   - Usa el endpoint `/api/shipping/crear/{pedidoId}`
   - Se genera automáticamente:
     - Número de seguimiento
     - El pedido pasa a "Enviado"

4. **Imprimir etiqueta de envío** 🏷️
   - Click en "Imprimir Etiqueta de Envío"
   - Usa el endpoint `/api/shipping/etiqueta/{numeroSeguimiento}`

5. **Marcar como enviado** 🚚
   - Click en "Marcar como Enviado"
   - El pedido queda listo para entrega

### Desde el Panel de Cliente:

1. **Ver estado del pedido** 👁️
   - Accede a "Mis Pedidos"
   - Ve el número de seguimiento

2. **Consultar tracking** 🔍
   - Usa el número de seguimiento
   - Ve el historial de eventos

## Formato de Números de Seguimiento

El sistema genera números en el formato:
```
SERVICIO-FECHA-NUMERO
```

Ejemplo: `ANDREANI-20260107-001001`

Donde:
- `ANDREANI`: Nombre del servicio
- `20260107`: Fecha (YYYYMMDD)
- `001001`: Número secuencial de 6 dígitos

## Estados del Pedido

El flujo completo es:

1. **Pendiente** → Cliente crea el pedido
2. **Pagado** → Se confirma el pago
3. **Enviado** → Se crea el envío y se despacha
4. **Entregado** → Se confirma la entrega

## Testing Manual

### Probar creación de envío:

```bash
# 1. Obtener token (como Deposito o Admin)
TOKEN="eyJhbGc..."

# 2. Cambiar un pedido a "Pagado" (como Admin)
curl -X PUT http://localhost:5090/api/pedidos/3/estado \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"estado":"Pagado"}'

# 3. Crear envío
curl -X POST http://localhost:5090/api/shipping/crear/3 \
  -H "Authorization: Bearer $TOKEN"

# 4. Consultar tracking
curl http://localhost:5090/api/shipping/tracking/ANDREANI-20260107-001001 \
  -H "Authorization: Bearer $TOKEN"

# 5. Simular entrega (solo Admin)
curl -X POST http://localhost:5090/api/shipping/simular-entrega/ANDREANI-20260107-001001 \
  -H "Authorization: Bearer $TOKEN"
```

## Integración con Frontend

### Agregar botón "Crear Envío" en el Panel de Depósito

El panel de depósito puede incluir un botón para crear envíos automáticamente:

```typescript
crearEnvio(pedidoId: number) {
  this.http.post(`${this.apiUrl}/shipping/crear/${pedidoId}`, {})
    .subscribe({
      next: (response: any) => {
        console.log('Envío creado:', response.numeroSeguimiento);
        this.cargarPedidos(); // Recargar lista
      }
    });
}
```

### Mostrar Tracking en "Mis Pedidos"

Los clientes pueden ver el tracking:

```typescript
verTracking(numeroSeguimiento: string) {
  this.http.get(`${this.apiUrl}/shipping/tracking/${numeroSeguimiento}`)
    .subscribe({
      next: (tracking: any) => {
        console.log('Eventos:', tracking.eventos);
      }
    });
}
```

## Migrar a Andreani Real

Cuando tengas credenciales de Andreani:

1. Actualiza `appsettings.json` con tus credenciales
2. Cambia en `ShippingController` la inyección:
   - De: `MockShippingService`
   - A: `AndreaniService`
3. El resto del flujo sigue igual

## Ventajas del Sistema Actual

✅ **Funciona inmediatamente** - No requiere configuración
✅ **Testing completo** - Prueba todo el flujo sin APIs externas
✅ **Números únicos** - Genera tracking numbers reales
✅ **Demos realistas** - Perfecto para mostrar a clientes
✅ **Fácil migración** - Cambia a Andreani real cuando quieras

## Preguntas Frecuentes

**¿Los números de seguimiento son únicos?**
Sí, se generan con timestamp + contador secuencial.

**¿Puedo usar esto en producción?**
Es ideal para desarrollo y demos. Para producción, conecta con Andreani real.

**¿Cómo simulo una entrega?**
Usa el endpoint `/api/shipping/simular-entrega/{numeroSeguimiento}` (solo Admin).

**¿Funciona el webhook?**
Sí, el webhook genérico (`/api/webhook/entrega`) sigue funcionando para recibir notificaciones externas.

---

**¡El sistema está listo para usar!** 🚀

Puedes empezar a crear envíos inmediatamente sin necesidad de configuración adicional.
