## La plataforma definitiva para gestionar reservas de manera fácil y segura

<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/aeea4ab5-7b25-4808-a8df-52a92c89a519" />
<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/9b736a6d-f8df-48f7-91d4-2e616e9676a7" />
<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/fc5a379b-6309-4dbf-9839-a978245b616f" />



**Reservas** es una solución completa de gestión de reservas diseñada para ayudar a empresas y profesionales a optimizar sus operaciones, reducir conflictos de horarios y ofrecer una experiencia excepcional a sus clientes.

### 🚀 Características principales

| Característica | Beneficio |
|:---|:---|
| **Gestión intuitiva** | Interfaz fácil de usar para crear, editar y seguir reservas en tiempo real |
| **Landing pública** | Los clientes reservan canchas, espacios u opciones en línea sin crear una cuenta |
| **Verificación por correo** | Un código de 6 dígitos confirma que la reserva viene de una persona real |
| **Correos de reserva** | Confirmación al reservar y recordatorio automático antes del turno |
| **Autenticación segura** | Sistema JWT con tokens de acceso y refresco para proteger tus datos |
| **Notificaciones en tiempo real** | SignalR integra notificaciones instantáneas entre el backend y el frontend |
| **Control de roles** | Roles de Admin y Vendedor con permisos diferenciados |
| **Limitación de tasa** | Protección contra abusos con límite de 10 peticiones por 10 segundos por IP |
| **Base de datos robusta** | PostgreSQL para fiabilidad y rendimiento |
| **API REST completa** | Endpoints diseñados para integrarse con cualquier frontend o terceros |

### 🎯 ¿Para quién es?

- **Negocios que alquilan espacios u ofrecen sesiones** y necesitan controlar su disponibilidad
- **Empresas de reservas** (spas, restaurantes, consultorios, salones de eventos)
- **Desarrolladores** que buscan una base sólida para sistemas de reservas
- **Equipos que valoran la seguridad** y la privacidad de los datos

### 🧩 Conceptos

| Concepto | Qué representa |
|:---|:---|
| **Espacio** | Lo que se reserva: una sala, cancha, consultorio, mesa o cabina. Tiene categoría y horario de atención. |
| **Opción de reserva** | Lo que el cliente elige: nombre, duración y precio. Cada opción pertenece a un espacio. |
| **Categoría** | Agrupa espacios por tipo (Sala, Deporte, Consulta…). Es texto libre, no una lista cerrada. |
| **Cliente** | Persona o empresa que reserva. El directorio es global para el negocio. |
| **Reserva** | Ocupación de un espacio en una fecha y hora, con su opción, precio y estado. |

La **landing pública** vive en `/` y permite reservar sin cuenta. El **panel de
gestión** vive en `/panel`; sus secciones son `/reservas`, `/calendario`,
`/clientes`, `/espacios`, `/opciones` y `/usuarios` (solo administradores).

### ✉️ Landing pública y verificación por correo

El flujo público no crea cuentas de cliente. El correo verificado funciona como
prueba mínima de identidad y, al confirmar, el cliente se crea o actualiza en el
directorio interno:

1. El cliente elige espacio, opción, fecha y horario en la landing.
2. La API envía un código de 6 dígitos al correo (`/api/public/verificacion/solicitar`).
3. El cliente captura el código; la API emite un token de un solo uso
   (`/api/public/verificacion/confirmar`).
4. Con ese token se crea la reserva en estado **Pendiente** (`/api/public/reservas`).
5. El personal la revisa y la confirma desde el panel.
6. El sistema envía un correo de confirmación al crear la reserva (o al confirmarla)
   y un recordatorio automático antes del turno (`RecordatorioHorasAntes`, 24 h por
   defecto).

Los códigos y tokens se guardan como **hash HMAC**, caducan y tienen límite de
intentos. El endpoint público solo devuelve catálogo, horarios libres y la reserva
recién creada; nunca expone el directorio de clientes ni la agenda completa.

**Correo en desarrollo:** si no configuras el correo y la contraseña de
aplicación, la API escribe el código en el log y lo devuelve como
`codigoDesarrollo` para probar la landing. En producción el código nunca se
expone: configura Gmail mediante variables de entorno.

#### Configurar Gmail (solo correo y contraseña)

Solo necesitas dos datos:

- Tu correo de Gmail.
- Una **contraseña de aplicación** (no sirve la contraseña normal).

El servidor `smtp.gmail.com`, el puerto `587`, SSL y el remitente ya vienen
configurados por defecto.

1. Activa la verificación en dos pasos en la cuenta de Google.
2. Entra a <https://myaccount.google.com/apppasswords> y crea una contraseña de
   aplicación (16 caracteres). Cópiala sin espacios.
3. Ejecuta el script; te pedirá únicamente el correo y la contraseña. La
   contraseña no se muestra en pantalla ni se guarda en Git:

   ```bash
   ./Back/scripts/configurar-gmail.sh
   ```

   También puedes editar el `.env` de la raíz y añadir las dos variables
   (tienes una plantilla en `.env.example`):

   ```env
   SMTP_USER=tu-correo@gmail.com
   SMTP_PASSWORD=la-contraseña-de-aplicación
   ```

4. Reinicia la API (`cd Back && dotnet run`). La API carga las variables SMTP
   del `.env` de la raíz automáticamente; Docker Compose lee el mismo archivo.

El script guarda solo `SMTP_USER` y `SMTP_PASSWORD` en `.env`, sin tocar las
demás variables. Ese archivo está ignorado por Git y las variables SMTP tienen
prioridad sobre `appsettings.json`. Gmail tiene límites de envío diario; para
volúmenes mayores usa un proveedor transaccional (Resend, SendGrid, Amazon SES,
etc.).

Variables opcionales (normalmente no hacen falta):

```env
SMTP_FROM_NAME=Reservas
# Solo si usas otro proveedor SMTP:
# SMTP_HOST=smtp.gmail.com
# SMTP_PORT=587
# SMTP_FROM=tu-correo@gmail.com
# SMTP_ENABLE_SSL=true
```

No escribas la contraseña en `docker-compose.yaml` ni en `appsettings.json`.


### 🛠️ Tecnología

| Capa | Tecnología |
|:---|:---|
| **Backend** | .NET 10 con ASP.NET Core |
| **Base de datos** | PostgreSQL |
| **Autenticación** | JWT (JSON Web Tokens) |
| **Tiempo real** | SignalR |
| **Frontend** | Vue 3 + Vite |
| **Control de versiones** | Git |

### 📦 Empezando rápido

```bash
# 1. Clona el repositorio
git clone https://github.com/tu-usuario/reservas.git

# 2. Configura las variables de entorno
# Copia .env.example a .env y completa tus valores seguros

# 3. Ejecuta el backend
cd Back
dotnet run

# 4. Ejecuta el frontend
cd Front
npm install
npm run dev
```

## 📋 Información del proyecto

### Arquitectura

El sistema sigue una arquitectura cliente-servidor moderna:

- **Backend (.NET 10)**: Expone una API RESTful con controladores, servicios layer y Entity Framework Core con PostgreSQL. Incluye autenticación JWT, rate limiting, y integración con SignalR para comunicaciones en tiempo real.

- **Frontend (Vue 3 + Vite)**: Interfaz de usuario reactiva que consume la API y mantiene conexión persistente con el backend mediante SignalR para recibir notificaciones instantáneas. La moneda y el formato regional se configuran con `VITE_CURRENCY` y `VITE_LOCALE` (por defecto `MXN` y `es-MX`).

### Flujo de trabajo

1. **Cliente** entra a la landing pública, consulta disponibilidad y reserva en línea
2. **Sistema** verifica el correo con un código de 6 dígitos y crea la reserva pendiente
3. **Usuario del panel** inicia sesión con credenciales JWT y revisa la agenda
4. **Sistema** valida el token en cada petición y envía notificaciones en tiempo real
5. **Administrador** supervisa operaciones y gestiona roles de usuario

### Seguridad

- Las **credenciales sensibles** (contraseñas de base de datos, claves JWT, tokens, SMTP) nunca se deben committear
- Usa el archivo `.env` local o secretos del sistema para configuraciones sensibles
- El `.gitignore` protege automáticamente archivos de configuración delicados
- Las conexiones deben realizarse siempre por HTTPS en producción
- La landing pública tiene límite de solicitudes; los códigos de correo caducan,
  admiten pocos intentos y los tokens de reserva son de un solo uso

### Despliegue

El proyecto incluye configuración `docker-compose.yaml` para despliegue con Docker, separando el backend API, la base de datos PostgreSQL y el frontend Vue en contenedores aislados.

---

**¿Listo para transformar tu forma de gestionar reservas?** ¡Empieza hoy y descubre la diferencia que Reservas puede hacer en tu negocio! 🚀
