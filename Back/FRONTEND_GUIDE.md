# Guía para el Frontend (Vue) — Reservas, Fechas y Horas

## Regla de oro

La API trabaja con **hora local del negocio**: la misma hora que ves en el reloj de la
cancha/sala, y la misma contra la que se comparan los horarios de apertura y cierre
del recurso. **No es UTC y no lleva zona horaria.**

Por eso las reservas viajan como un **string "de reloj"** en formato:

```
yyyy-MM-ddTHH:mm      ->  "2026-10-10T16:00"
```

- Al **enviar** (POST/PUT): manda ese string, sin `Z` y sin offset.
- Al **recibir** (respuestas): siempre llega en ese mismo formato.
- Nunca conviertas la reserva con `new Date(...)` "a UTC" antes de enviarla.
- Los horarios del recurso son horas cortas `"HH:mm"` (`"08:00"`).

> Para el contrato completo de endpoints, roles y reglas, ver [`README.md`](README.md).
> Para el esquema de datos, ver [`docs/MODELO-DATOS.md`](docs/MODELO-DATOS.md).

---

## 1. Crear una reserva (`POST /api/reservas`)

`fechaHoraInicio` es un **string**. `fechaHoraFin`, el precio y la duración los calcula y
congela el backend a partir del tipo de servicio.

```json
{
  "clienteId": 24,
  "tipoServicioId": 3,
  "fechaHoraInicio": "2026-10-10T16:00",
  "estado": 0,
  "nota": "cliente pidió pelota"
}
```

`nota` es opcional. `estado` es numérico:

| Valor | Estado      |
|------:|-------------|
| 0     | Pendiente   |
| 1     | Confirmada  |
| 2     | Cancelada   |
| 3     | Completada  |

Respuesta (`201`):

```json
{
  "id": 45,
  "clienteId": 24,
  "nombreCliente": "Ana López",
  "recursoReservableId": 2,
  "nombreRecurso": "Cancha 1",
  "tipoServicioId": 3,
  "nombreServicio": "Turno de una hora",
  "fechaHoraInicio": "2026-10-10T16:00",
  "fechaHoraFin": "2026-10-10T17:00",
  "estado": 0,
  "precio": 450.00,
  "duracionMinutos": 60,
  "nota": "cliente pidió pelota",
  "fechaCreacion": "2026-09-26T18:20Z"
}
```

> `fechaCreacion` es la única fecha en UTC (es un instante de auditoría) y por eso
> termina en `Z`. Las fechas de la reserva (`fechaHoraInicio` / `fechaHoraFin`) **no**
> llevan `Z`.
>
> `precio`, `duracionMinutos` y `nombreServicio` son un **snapshot inmutable**: reflejan
> el servicio al momento de crear la reserva, aunque después cambie el catálogo. Úsalos
> para mostrar y cobrar; no los recalcules desde el `tipoServicio` actual.
>
> `nombreCliente`, `nombreRecurso` y `nombreServicio` vienen resueltos: no necesitas
> hacer llamadas extra para mostrar la lista.

## 2. Actualizar una reserva (`PUT /api/reservas/{id}`)

Misma forma que crear, `fechaHoraInicio` vuelve a ser string:

```json
{
  "id": 45,
  "clienteId": 24,
  "tipoServicioId": 3,
  "fechaHoraInicio": "2026-10-10T18:30",
  "estado": 1,
  "nota": null
}
```

Si cambias `fechaHoraInicio`, el backend recalcula fin, duración y precio (nuevo snapshot).

### Cancelar / cambiar solo el estado (`PATCH /api/reservas/{id}/estado`)

Más cómodo que un `PUT` completo para cancelar o confirmar:

```json
{ "estado": 2 }
```

Devuelve la reserva completa. Cancelar (`2`) libera el turno; reactivar revalida que el
horario siga libre (`400`/`409` si ya lo tomaron).

## 3. Consultar por rango de fechas (`GET /api/reservas/por-fechas`)

Pensado para calendarios y comparativas. Devuelve las reservas que **se solapan** con
el rango (no solo las que empiezan dentro). Ambos extremos son opcionales.

```
GET /api/reservas/por-fechas?desde=2026-10-01T00:00&hasta=2026-10-31T23:59
GET /api/reservas/por-fechas?desde=2026-10-10T00:00&recursoReservableId=2
GET /api/reservas/por-fechas            # sin filtros = todas
```

| Query                 | Formato            | Obligatorio |
|-----------------------|--------------------|-------------|
| `desde`               | `yyyy-MM-ddTHH:mm` | No          |
| `hasta`               | `yyyy-MM-ddTHH:mm` | No          |
| `recursoReservableId` | entero             | No          |

Si `desde` es posterior a `hasta`, responde `400`.

## 4. Horarios del recurso (TimeSpan)

`horarioAperturaDefault` y `horarioCierreDefault` se envían y llegan como horas
cortas `"HH:mm"`. Es exactamente lo que produce un `<input type="time">`.

```json
{
  "nombre": "Cancha 1",
  "tipoDeporte": "Fútbol 5",
  "activo": true,
  "horarioAperturaDefault": "08:00",
  "horarioCierreDefault": "23:00"
}
```

Al leerlos también llegan como `"08:00"` / `"23:00"`.

---

## 5. Cómo armarlo en Vue

### Enviar: de `datetime-local` a la API

`<input type="datetime-local">` ya produce `"2026-10-10T16:00"`. Casi siempre puedes
mandarlo tal cual; solo hay que quitar los segundos si el navegador los agrega.

```js
// utils/fechas.js
// Convierte un Date local a "yyyy-MM-ddTHH:mm" SIN pasar por UTC.
export function aFechaApi(date) {
  const pad = (n) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
         `T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

// Acepta "2026-10-10T16:00", "2026-10-10 16:00" o con segundos; deja el formato canónico.
export function limpiarFechaApi(valor) {
  if (!valor) return ''
  return valor.trim().replace(' ', 'T').slice(0, 16) // corta segundos y cualquier sufijo
}
```

```js
// En el componente
import { aFechaApi, limpiarFechaApi } from '@/utils/fechas'

const form = ref({
  clienteId: null,
  tipoServicioId: null,
  fechaHoraInicio: '', // lo llena <input type="datetime-local">
  estado: 0
})

async function crearReserva() {
  const payload = {
    ...form.value,
    fechaHoraInicio: limpiarFechaApi(form.value.fechaHoraInicio)
  }

  try {
    const { data } = await axios.post('/api/reservas', payload)
    console.log('Reserva creada:', data)
  } catch (error) {
    // 400 -> { detail: "..." } (ver sección de errores)
    console.error(error.response?.data?.detail ?? error.message)
  }
}
```

Si partes de un `Date` (por ejemplo de `v-calendar` o un date-picker que devuelve
objetos `Date`), conviértelo con `aFechaApi(date)`.

### Leer: mostrar la fecha sin que se desplace

Como el backend devuelve hora local sin zona, **no uses `new Date(str)`** para
mostrarla: el navegador la interpretaría en UTC y podría restarle horas. Hazlo por
manipulación de string.

```js
export function formatearFecha(isoLocal) {
  // isoLocal: "2026-10-10T16:00"
  if (!isoLocal) return ''
  const [fecha, hora] = isoLocal.split('T')
  const [y, m, d] = fecha.split('-')
  return `${d}/${m}/${y} ${hora}`
  // -> "10/10/2026 16:00"
}
```

```vue
<template>
  <tr v-for="r in reservas" :key="r.id">
    <td>{{ formatearFecha(r.fechaHoraInicio) }}</td>
    <td>{{ formatearFecha(r.fechaHoraFin) }}</td>
    <td>{{ r.estado }}</td>
  </tr>
</template>
```

Si necesitas un `Date` de verdad (para plugins de calendario), constrúyelo **manual**
para evitar la interpretación UTC:

```js
export function aDateLocal(isoLocal) {
  const [fecha, hora] = isoLocal.split('T')
  const [y, m, d] = fecha.split('-').map(Number)
  const [hh, mm] = hora.split(':').map(Number)
  return new Date(y, m - 1, d, hh, mm) // constructor local, no UTC
}
```

### Calendario por rango

```js
async function cargarMes(anio, mes) {
  const mm = String(mes).padStart(2, '0')
  const ultimoDia = new Date(anio, mes, 0).getDate() // mes es 1-12
  const desde = `${anio}-${mm}-01T00:00`
  const hasta = `${anio}-${mm}-${ultimoDia}T23:59`

  const { data } = await axios.get('/api/reservas/por-fechas', {
    params: { desde, hasta, recursoReservableId: 2 }
  })
  return data
}
```

---

## 6. Errores comunes (y cómo evitarlos)

| ❌ Mal | ✅ Bien | Por qué |
|---|---|---|
| `"2026-10-10T16:00:00Z"` | `"2026-10-10T16:00"` | La `Z` significa UTC; no aplica a la hora local del negocio. |
| `new Date(...).toISOString()` | `aFechaApi(date)` | `toISOString` convierte a UTC y desplaza la hora. |
| `new Date("2026-10-10T16:00")` para mostrar | `formatearFecha(str)` | El navegador puede reinterpretar la zona y restar horas. |
| Enviar `fechaHoraFin` en el POST/PUT | Solo `fechaHoraInicio` | El backend calcula el fin con la duración del servicio. |
| Recalcular el precio desde el catálogo actual | Usar `precio` de la reserva | El `precio` guardado es el acordado (snapshot inmutable). |
| Mandar `"08:00:00"` por costumbre | `"08:00"` | Ambos se aceptan, pero `"HH:mm"` es lo canónico y lo que devuelve la API. |

## 7. Respuestas de error

Un `400` por fecha inválida trae este detalle:

```json
{
  "status": 400,
  "title": "Solicitud inválida",
  "detail": "El campo 'FechaHoraInicio' tiene un formato inválido ('...'). Use el formato yyyy-MM-ddTHH:mm. Ejemplo: 2026-10-10T16:00."
}
```

Códigos que conviene manejar en el front:

| Código | Cuándo | Qué hacer |
|---:|---|---|
| `400` | DTO inválido o regla de negocio (fuera de horario, no disponible) | Mostrar `detail` |
| `401` | Token vencido | Renovar con `/api/auth/refresh` |
| `403` | Rol insuficiente | Ocultar la acción en la UI |
| `404` | Cliente/servicio/recurso/reserva no existe | Refrescar listas |
| `409` | Email/teléfono duplicado o solapamiento detectado en BD | Mostrar `detail` y refrescar calendario |
| `429` | Demasiadas peticiones a `/api/auth` | Esperar y reintentar |

## Resumen

- **Formato único entrada/salida de reservas:** `yyyy-MM-ddTHH:mm` (hora local, sin `.`)
- **`fechaHoraFin`, `precio`, `duracionMinutos`:** solo lectura; snapshot inmutable.
- **`nombreCliente` / `nombreRecurso` / `nombreServicio`:** ya resueltos en la respuesta.
- **`fechaCreacion`:** única con `Z` (UTC de auditoría).
- **Horarios del recurso:** `"HH:mm"`.
- **Cancelar:** `PATCH /api/reservas/{id}/estado` con `{ "estado": 2 }`.
- **Consultas por rango:** `GET /api/reservas/por-fechas?desde=&hasta=&recursoReservableId=`
- **Regla práctica:** manipula las fechas de reserva como strings; solo usa `Date`
  con el constructor local (`new Date(y, m-1, d, hh, mm)`).