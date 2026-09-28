# Reservas API

Guía de integración para el frontend (Vue) de la API de reservas de canchas.

---

## 1. Arquitectura en una vista

```
Cliente (CRM, global)  ──┐
                         │
RecursoReservable ──┐    │
  (Cancha 1)        │    │
                    ▼    ▼
TipoServicio ───► Reserva
 (Turno 1 hora)   (snapshot de precio/duración/recurso)
```

Conceptos clave:

- **Cliente**: registro de contacto (nombre, teléfono, email). Es **global**: cualquier
  Admin o Vendedor lo ve. No tiene login.
- **RecursoReservable**: la cancha/sala. Tiene horario de apertura/cierre y `activo`.
- **TipoServicio**: modalidad sobre una cancha (ej. "Turno 1 hora"), con duración y precio.
- **Reserva**: hecho **inmutable**. Guarda un *snapshot* del precio, duración, nombre del
  servicio y recurso al momento de crearse. Cambiar el catálogo después **no** altera el
  historial.
- **Usuario**: cuentas de staff con rol `Admin` o `Vendedor`. No hay rol Cliente: el
  cliente llama y el staff crea la reserva.

El diagrama de la base de datos y las decisiones de diseño están en
[`docs/MODELO-DATOS.md`](docs/MODELO-DATOS.md).

---

## 2. Ejecutar localmente

```bash
dotnet run
```

| Perfil | URL |
|---|---|
| HTTP | `http://localhost:5020` |
| HTTPS | `https://localhost:7270` |

En Development se crea un Admin si no existe:

- Email: `admin@reservas.local`
- Contraseña: `Admin1234@`

Las propiedades JSON usan camelCase. Swagger/OpenAPI se publica en Development.

### Base de datos

PostgreSQL. El esquema se crea con:

```bash
dotnet ef database update
```

> Este proyecto usa **migraciones desde cero** (una única `LineaBase`). Si vienes de la
> versión anterior, borra y recrea la base:
>
> ```bash
> dropdb SistemaReservass && createdb SistemaReservass
> dotnet ef database update
> ```
>
> La migración crea la extensión `btree_gist` y la exclusion constraint que impide
> doble reserva (ver sección 6).

---

## 3. Autenticación

### Iniciar sesión

`POST /api/auth/login` (anónimo):

```json
{ "email": "admin@reservas.local", "password": "Admin1234@" }
```

Respuesta `200`:

```json
{ "accessToken": "<jwt>", "refreshToken": "<refresh-token>" }
```

En cada endpoint protegido:

```http
Authorization: Bearer <accessToken>
```

### Renovar tokens

`POST /api/auth/refresh` recibe `{ accessToken, refreshToken }` y devuelve un par nuevo.
El refresh anterior deja de ser válido. Duración del access token: 15 min.

### Registrar vendedores

`POST /api/auth/register` (solo Admin). Crea una cuenta con rol `Vendedor`.

```json
{ "nombre": "María Pérez", "email": "maria@example.com", "password": "ClaveSegura123" }
```

---

## 4. Roles

| Operación | Admin | Vendedor |
|---|---:|---:|
| Registrar usuarios | Sí | No |
| Clientes (crear/ver/editar) | Sí | Sí |
| Eliminar clientes (lógico) | Sí | No |
| Reservas (crear/ver/editar) | Sí | Sí |
| Cambiar estado de reserva | Sí | Sí |
| Eliminar reserva (física, emergencia) | Sí | No |
| Ver recursos y tipos de servicio | Sí | Sí |
| Crear/editar/eliminar catálogo | Sí | No |

---

## 5. Endpoints

### Clientes

Clientes **globales**: todos los ven, sin importar quién los creó.

| Método y ruta | Uso | Respuesta |
|---|---|---|
| `GET /api/clientes?pagina=1&cantidad=10` | Listar (paginado) | `200` |
| `GET /api/clientes/{id}` | Obtener uno | `200` / `404` |
| `GET /api/clientes/buscar?email=...` | Buscar por email exacto | `200` / `404` |
| `POST /api/clientes` | Crear | `201` |
| `PUT /api/clientes/{id}` | Actualizar (id del body = ruta) | `200` / `404` |
| `DELETE /api/clientes/{id}` | Borrado **lógico** (Admin) | `204` / `404` |

Crear / actualizar (en update se agrega `id`):

```json
{ "nombre": "Ana López", "telefono": "+525512345678", "email": "ana@example.com" }
```

Respuesta paginada:

```json
{ "items": [], "pagina": 1, "cantidad": 10, "total": 0, "totalPaginas": 0 }
```

### Recursos reservables

| Método y ruta | Uso | Respuesta |
|---|---|---|
| `GET /api/recursos-reservables` | Listar activos | `200` |
| `GET /api/recursos-reservables/{id}` | Obtener | `200` / `404` |
| `GET /api/recursos-reservables/buscar?tipoDeporte=tenis` | Buscar por deporte | `200` |
| `POST /api/recursos-reservables` | Crear (Admin) | `201` |
| `PUT /api/recursos-reservables/{id}` | Actualizar (Admin) | `200` |
| `DELETE /api/recursos-reservables/{id}` | Borrado **lógico** (Admin) | `204` |

```json
{
  "nombre": "Cancha 1",
  "tipoDeporte": "Fútbol 5",
  "activo": true,
  "horarioAperturaDefault": "08:00",
  "horarioCierreDefault": "22:00"
}
```

Los horarios son `"HH:mm"`. El backend rechaza `apertura >= cierre` (`400`).

### Tipos de servicio

| Método y ruta | Uso | Respuesta |
|---|---|---|
| `GET /api/tipos-servicio` | Listar activos | `200` |
| `GET /api/tipos-servicio/{id}` | Obtener | `200` / `404` |
| `GET /api/tipos-servicio/por-recurso/{recursoReservableId}` | Listar por recurso | `200` |
| `POST /api/tipos-servicio` | Crear (Admin) | `201` |
| `PUT /api/tipos-servicio/{id}` | Actualizar (Admin) | `200` |
| `DELETE /api/tipos-servicio/{id}` | Borrado **lógico** (Admin) | `204` |

```json
{
  "recursoReservableId": 2,
  "nombre": "Turno de una hora",
  "duracionMinutos": 60,
  "precio": 450.00,
  "activo": true
}
```

### Reservas

Todas requieren `Admin` o `Vendedor`.

| Método y ruta | Uso | Respuesta |
|---|---|---|
| `GET /api/reservas` | Listar | `200` |
| `GET /api/reservas/{id}` | Obtener | `200` / `404` |
| `GET /api/reservas/por-cliente/{clienteId}` | Listar de un cliente | `200` |
| `GET /api/reservas/por-recurso/{recursoReservableId}` | Listar de un recurso | `200` |
| `GET /api/reservas/por-fechas?desde=&hasta=&recursoReservableId=` | Rango (calendario) | `200` / `400` |
| `POST /api/reservas` | Crear | `201` / `400` |
| `PUT /api/reservas/{id}` | Actualizar | `200` / `400` |
| `PATCH /api/reservas/{id}/estado` | Cambiar solo el estado | `200` / `400` |
| `DELETE /api/reservas/{id}` | Eliminar (Admin, emergencia) | `204` / `404` |

**Crear reserva** — el backend calcula fin, duración y precio:

```json
{
  "clienteId": 3,
  "tipoServicioId": 1,
  "fechaHoraInicio": "2026-10-10T16:00",
  "estado": 0,
  "nota": "cliente pidió pelota"
}
```

`nota` es opcional. `fechaHoraFin` **no se envía**.

**Respuesta de reserva:**

```json
{
  "id": 45,
  "clienteId": 3,
  "nombreCliente": "Ana López",
  "recursoReservableId": 2,
  "nombreRecurso": "Cancha 1",
  "tipoServicioId": 1,
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

**Estados** (`estado` es numérico):

| Valor | Estado |
|---:|---|
| 0 | Pendiente |
| 1 | Confirmada |
| 2 | Cancelada |
| 3 | Completada |

**Cambiar estado** (`PATCH`):

```json
{ "estado": 2 }
```

Cancelar (`estado: 2`) libera el turno. Reactivar una cancelada revalida el horario.

---

## 6. Manejo de fechas y horas

Regla de oro: **hora local del negocio, sin zona horaria**.

```
yyyy-MM-ddTHH:mm      ->  "2026-10-10T16:00"
```

- Al **enviar**: string sin `Z` ni offset.
- Al **recibir**: siempre ese formato.
- `fechaCreacion` es la excepción: es UTC de auditoría y termina en `Z`.
- Los horarios de recurso (`horarioAperturaDefault`, `horarioCierreDefault`) son `"HH:mm"`.

El frontend **no debe** usar `new Date(...).toISOString()` para las reservas: convierte a
UTC y desplaza la hora. Ver ejemplos completos de Vue en [`FRONTEND_GUIDE.md`](FRONTEND_GUIDE.md).

---

## 7. Reglas de negocio (validación)

| Regla | Respuesta |
|---|---|
| Reserva dentro de `horarioApertura`–`horarioCierre` del recurso | `400` |
| Sin solapamiento con otra reserva activa del mismo recurso | `400` / `409` |
| Cliente existente y activo | `404` |
| Tipo de servicio existente, activo y no eliminado | `400` |
| Recurso activo y no eliminado | `400` |
| `desde <= hasta` en consulta por rango | `400` |
| Email o teléfono único entre clientes activos | `409` |

El solapamiento se valida **dos veces**: en el servicio (mensaje amigable) y con una
**exclusion constraint de PostgreSQL** que impide el doble agendamiento incluso si dos
peticiones concurrentes pasan la primera validación.

---

## 8. Protección contra doble reserva

La tabla `Reservas` tiene:

```sql
EXCLUDE USING gist (
  "RecursoReservableId" WITH =,
  tsrange("FechaHoraInicio", "FechaHoraFin") WITH &&
) WHERE ("Estado" <> 2);   -- las canceladas no bloquean
```

Esto significa:

- Nunca pueden coexistir dos reservas activas solapadas del mismo recurso.
- Las reservas **canceladas** (estado 2) no bloquean el turno.
- Requiere la extensión `btree_gist`, creada por la migración.

---

## 9. Errores y estados HTTP

| Código | Significado |
|---:|---|
| `200` | Correcto con contenido |
| `201` | Creado |
| `204` | Borrado correcto, sin body |
| `400` | DTO inválido, id ruta/body distinto o regla de negocio |
| `401` | Sin autenticación o credenciales/token inválidos |
| `403` | Autenticado sin el rol requerido |
| `404` | No existe |
| `409` | Conflicto: duplicado único o solapamiento detectado en BD |
| `429` | Límite de solicitudes (`/api/auth`: 10 por IP cada 10s) |
| `500` | Error inesperado |

El middleware responde `application/problem+json`:

```json
{
  "type": "about:blank",
  "title": "Solicitud inválida",
  "status": 400,
  "detail": "La reserva está fuera del horario permitido para este recurso.",
  "instance": "/api/reservas",
  "traceId": "..."
}
```

---

## 10. SignalR

Hub: `/hubs/notificaciones`. Evento `ReservaActualizado` (sin payload) al crear,
actualizar, cambiar estado o eliminar una reserva:

```javascript
connection.on("ReservaActualizado", () => { recargarReservas(); });
```

---

## 11. CORS

En Development se permiten `http://localhost:3000`, `http://localhost:4200` y
`http://localhost:5173`. En producción, configura los orígenes exactos:

```text
Cors__AllowedOrigins__0=https://reservas.example.com
```

---

## 12. Migraciones y esquema

- Una única migración: `LineaBase`.
- Incluye la extensión `btree_gist` y la exclusion constraint.
- Borrado **lógico** (`EliminadoEn`) en `Cliente`, `RecursoReservable` y `TipoServicio`.
  Las FK de `Reserva` usan `ON DELETE RESTRICT`: nunca se borra físicamente un registro
  con historial.
- `Reserva` guarda snapshot de `Precio`, `DuracionMinutos`, `NombreServicio` y
  `RecursoReservableId`.