## La plataforma definitiva para gestionar reservas de manera fácil y segura

<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/aeea4ab5-7b25-4808-a8df-52a92c89a519" />
<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/9b736a6d-f8df-48f7-91d4-2e616e9676a7" />
<img width="260" height="200" alt="image" src="https://github.com/user-attachments/assets/fc5a379b-6309-4dbf-9839-a978245b616f" />



**Reservas** es una solución completa de gestión de reservas diseñada para ayudar a empresas y profesionales a optimizar sus operaciones, reducir conflictos de horarios y ofrecer una experiencia excepcional a sus clientes.

### 🚀 Características principales

| Característica | Beneficio |
|:---|:---|
| **Gestión intuitiva** | Interfaz fácil de usar para crear, editar y seguir reservas en tiempo real |
| **Autenticación segura** | Sistema JWT con tokens de acceso y refresco para proteger tus datos |
| **Notificaciones en tiempo real** | SignalR integra notificaciones instantáneas entre el backend y el frontend |
| **Control de roles** | Roles de Admin y Vendedor con permisos diferenciados |
| **Limitación de tasa** | Protección contra abusos con límite de 10 peticiones por 10 segundos por IP |
| **Base de datos robusta** | PostgreSQL para fiabilidad y rendimiento |
| **API REST completa** | Endpoints diseñados para integrarse con cualquier frontend o terceros |

### 🎯 ¿Para quién es?

- **Gestores de servicios** que necesitan controlar la disponibilidad de recursos
- **Empresas de reservas** (spas, restaurantes, consultorios, salones de eventos)
- **Desarrolladores** que buscan una base sólida para sistemas de reservas
- **Equipos que valoran la seguridad** y la privacidad de los datos

### 🛠️ Tecnología

| Capa | Tecnología |
|:---|:---|
| **Backend** | .NET 8 con ASP.NET Core |
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

- **Backend (.NET 8)**: Expone una API RESTful con controladores, servicios layer y Entity Framework Core con PostgreSQL. Incluye autenticación JWT, rate limiting, y integración con SignalR para comunicaciones en tiempo real.

- **Frontend (Vue 3 + Vite)**: Interfaz de usuario reactiva que consume la API y mantiene conexión persistente con el backend mediante SignalR para recibir notificaciones instantáneas.

### Flujo de trabajo

1. **Usuario** accede a la aplicación y se autentica con credenciales JWT
2. **Sistema** valida el token en cada petición
3. **Usuario** gestiona reservas (crear, consultar, modificar, cancelar)
4. **Sistema** envía notificaciones en tiempo real sobre cambios de estado
5. **Administrador** supervisa operaciones y gestiona roles de usuario

### Seguridad

- Las **credenciales sensibles** (contraseñas de base de datos, claves JWT, tokens) nunca se deben committear
- Usa el archivo `.env` local o secretos del sistema para configuraciones sensibles
- El `.gitignore` protege automáticamente archivos de configuración delicados
- Las conexiones deben realizarse siempre por HTTPS en producción

### Despliegue

El proyecto incluye configuración `docker-compose.yaml` para despliegue con Docker, separando el backend API, la base de datos PostgreSQL y el frontend Vue en contenedores aislados.

---

**¿Listo para transformar tu forma de gestionar reservas?** ¡Empieza hoy y descubre la diferencia que Reservas puede hacer en tu negocio! 🚀
