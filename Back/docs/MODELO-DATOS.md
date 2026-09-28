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
`UsuarioId` guarda quién lo registró, solo informativo.

- Borrado **lógico** (`EliminadoEn`).
- `Email` y `Telefono` únicos **entre activos** (índices parciales), así se puede
  re-registrar un cliente dado de baja.

### RecursoReservable
La cancha/sala.

- `HorarioAperturaDefault` / `HorarioCierreDefault`: `interval` (`"08:00"`, `"22:00"`).
- `Activo` para deshabilitar sin borrar.
- Borrado **lógico** (`EliminadoEn`).

### TipoServicio
Modalidad sobre un recurso (ej. "Turno 1 hora").

- `DuracionMinutos` (1–1440) y `Precio`.
- `Activo` + borrado **lógico**.
- FK a `RecursoReservable` con `ON DELETE RESTRICT`.

### Reserva
Hecho inmutable. **Nunca cambia su `FechaHoraFin` por cambios de catálogo.**

- `FechaHoraInicio` / `FechaHoraFin`: `timestamp without time zone` (hora local).
- `FechaCreacion`: `timestamp with time zone` (UTC de auditoría).
- Snapshot: `Precio`, `DuracionMinutos`, `NombreServicio`, `RecursoReservableId`.
- `Estado`: 0 Pendiente, 1 Confirmada, 2 Cancelada, 3 Completada.
- FK a `Cliente`, `TipoServicio`, `RecursoReservable` con `ON DELETE RESTRICT`.

### HistorialRefreshToken
Rotación de refresh tokens. Se guarda el **hash SHA-256**, no el token en claro.

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
| RecursoReservable | Lógico | Conservar historial y servicios |
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