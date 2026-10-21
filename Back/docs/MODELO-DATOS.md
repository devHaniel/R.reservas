# Modelo de datos

## Diagrama

```
┌──────────────────┐        ┌──────────────────┐
│     Usuario      │        │     Cliente      │
│ (IdentityUser)   │        │  (global, CRM)   │
│ rol: Admin/      │        │  EliminadoEn     │
│ Vendedor         │        └────────┬─────────┘
└────────┬─────────┘                 │ 1
         │ 0..1                      │
         │                           │ N
         │ registra                  ▼
         └──────────────►  ┌──────────────────────┐
                           │       Reserva        │
                           │  (hecho inmutable)   │
                           │  FechaHoraInicio/Fin │
                           │  Estado              │
                           │  ── snapshot ──      │
                           │  Precio              │
                           │  DuracionMinutos     │
                           │  NombreServicio      │
                           │  RecursoReservableId │
                           └────┬────────────┬────┘
                                │ N          │ N
                    ┌───────────┘            └───────────┐
                    ▼ 1                                  ▼ 1
        ┌──────────────────────┐            ┌──────────────────────┐
        │  RecursoReservable   │ 1        N │     TipoServicio     │
        │  horario apert/cierre│───────────►│  duracionMinutos     │
        │  Activo, EliminadoEn │            │  precio, Activo      │
        └──────────────────────┘            │  EliminadoEn         │
                                            └──────────────────────┘
```

## Entidades

### Usuario
Cuenta de staff (Identity). Roles: `Admin`, `Vendedor`. **No hay rol Cliente.**

### Cliente
Contacto para reservar. **Global** (una sola sede): no se filtra por usuario.
`UsuarioId` guarda quién lo registró, solo informativo; es `null` cuando el
cliente se creó desde la landing pública.

- Borrado **lógico** (`EliminadoEn`).
- `Email` y `Telefono` únicos **entre activos** (índices parciales), así se puede
  re-registrar un cliente dado de baja.

### RecursoReservable
El espacio que se reserva (cancha, sala, consultorio, mesa…).

- `Categoria`: texto libre ("Salón", "Deporte", "Consulta") para agrupar espacios.
- `HorarioAperturaDefault` / `HorarioCierreDefault`: `interval` (`"08:00"`, `"22:00"`).
- `Activo` para deshabilitar sin borrar.
- Borrado **lógico** (`EliminadoEn`); al archivar el espacio también se archivan sus opciones.

### TipoServicio
Opción de reserva sobre un espacio (ej. "Turno 1 hora", "Sesión completa").

- `DuracionMinutos` (1–1440) y `Precio`.
- `Activo` + borrado **lógico**.
- FK a `RecursoReservable` con `ON DELETE RESTRICT`.

### Reserva
Hecho inmutable. **Nunca cambia su `FechaHoraFin` por cambios de catálogo.**

- `FechaHoraInicio` / `FechaHoraFin`: `timestamp without time zone` (hora local).
- `FechaCreacion`: `timestamp with time zone` (UTC de auditoría).
- `RecordatorioEnviadoEn`: `timestamp with time zone`, UTC del recordatorio enviado
  (evita duplicados; `null` mientras no se haya enviado).
- Snapshot: `Precio`, `DuracionMinutos`, `NombreServicio`, `RecursoReservableId`.
- `Estado`: 0 Pendiente, 1 Confirmada, 2 Cancelada, 3 Completada.
- FK a `Cliente`, `TipoServicio`, `RecursoReservable` con `ON DELETE RESTRICT`.

### HistorialRefreshToken
Rotación de refresh tokens. Se guarda el **hash SHA-256**, no el token en claro.

### VerificacionCorreo
Soporte del flujo público de reservas sin cuentas de cliente.

- `Email` normalizado a minúsculas.
- `CodigoHash`: HMAC-SHA256 del código de 6 dígitos; nunca se guarda el código.
- `CodigoExpiraEn`, `CodigoUsadoEn` e `Intentos` limitan la vigencia y la fuerza bruta.
- `TokenHash`: token de un solo uso que autoriza crear **una** reserva.
- `TokenExpiraEn`, `TokenUsadoEn`: vigencia y consumo del token.
- No tiene relación directa con `Cliente`: la verificación prueba el correo y la
  reserva crea o actualiza el cliente en el directorio.

## ¿Por qué hora local sin zona?

El negocio piensa en horas de pared ("la cancha abre a las 8"), y los horarios del
recurso son `TimeSpan` locales. Un `timestamptz` mezclaría zonas y desplazaría las
comparaciones. Con `timestamp without time zone` las comparaciones de solapamiento son
exactas y la exclusion constraint es directa.

## Integridad de turnos

```sql
EXCLUDE USING gist (
  "RecursoReservableId" WITH =,
  tsrange("FechaHoraInicio", "FechaHoraFin") WITH &&
) WHERE ("Estado" <> 2);
```

Garantiza a nivel de motor que no existan dos reservas activas solapadas del mismo
recurso, aunque dos peticiones concurrentes pasen la validación del servicio.

## Borrado lógico vs. físico

| Entidad | Borrado | Motivo |
|---|---|---|
| Cliente | Lógico | Conservar historial de reservas |
| Verificación de correo | Expira por tiempo | Datos temporales de seguridad |
| RecursoReservable | Lógico | Conservar historial y opciones |
| TipoServicio | Lógico | Conservar precio/duración históricos |
| Reserva | Físico (solo Admin, emergencia) | El flujo normal es cancelar (estado 2) |

## Decisiones de diseño y su porqué

| Decisión | Alternativa descartada | Por qué |
|---|---|---|
| Snapshot en `Reserva` | Solo FK a `TipoServicio` | Un cambio de precio no debe reescribir el pasado |
| Exclusion constraint | Solo validación en código | Evita doble reserva con peticiones concurrentes |
| Hora local sin zona | UTC | El negocio trabaja en hora de pared |
| Cliente global | Cliente por vendedor | Una sola sede; evita duplicados y choques |
| Borrado lógico | Cascada | No perder contabilidad |