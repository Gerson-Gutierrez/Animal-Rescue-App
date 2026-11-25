# Animal Rescue — Plataforma Móvil de Búsqueda y Rescate Animal

Aplicación móvil multiplataforma (**Android e iOS**) desarrollada con **React Native** y **TypeScript**, integrada a una API RESTful construida en **C# / .NET** con **Entity Framework Core** y **SQL Server**. El sistema centraliza reportes de mascotas extraviadas, geolocalización interactiva, coordinación de hogares de tránsito y comunicación directa en tiempo real entre dueños y rescatistas.

---

## Descripción funcional y módulos principales

- **Inicio (Feed de reportes):** Listado de animales perdidos con visualización fotográfica y ficha estructurada de identificación (nombre, presencia de chapita QR, estado de castración, última ubicación registrada y estado del caso) para agilizar su reconocimiento.
- **Mapa interactivo:** Visualización geoespacial mediante marcadores personalizados (ícono de patita) en el punto exacto donde se perdió cada mascota o se ubica un refugio. Incluye vista previa rápida con foto y nombre al seleccionar un marcador, navegación al detalle con doble toque y gestión de refugios registrados por el usuario.
- **Publicar (Gestión de reportes):** Formulario estructurado para registrar casos con información crítica de búsqueda, captura de imágenes desde cámara o galería y geocodificación automática de direcciones (`debounce` de `0,7 s`) con ajuste manual mediante arrastre o selección directa sobre el mapa.
- **Mensajes (Chat en tiempo real):** Canal de comunicación bidireccional entre dueño y rescatista impulsado por **SignalR**, con soporte para envío de imágenes de verificación y coordinación segura de puntos de encuentro para el reencuentro.
- **Rescue (Hogares de tránsito y refugios):** Espacio dedicado a voluntarios y espacios de acogida que ofrecen asilo temporal, articulando la red de contención mientras se localiza a los responsables del animal.
- **Administración y moderación:** Panel interno para gestión de reportes, revisión de publicaciones y control de administradores.

---

## Arquitectura y stack tecnológico

### Frontend Móvil
- **Core:** React Native + TypeScript + `expo-router` (enrutamiento basado en archivos para navegación por pestañas, vistas dinámicas y administración).
- **Seguridad y sesión:** Almacenamiento cifrado de credenciales en dispositivo mediante `expo-secure-store` y cliente HTTP centralizado (`fetch`) con renovación automática de tokens JWT.
- **Geolocalización:** Integración de **Leaflet + OpenStreetMap** embebido en `react-native-webview`, eliminando la dependencia de claves propietarias de Google Maps y permitiendo personalización completa de marcadores y teselas (`MAPA_TESELAS`).
- **Comunicación en tiempo real:** Cliente **SignalR** integrado al ciclo de vida de la sesión para mensajería instantánea y transferencia de imágenes.

### Backend y Persistencia
- **API y Lógica de Negocio:** **C# / ASP.NET Core** bajo arquitectura en capas, exponiendo endpoints RESTful tipados mediante contratos DTO (`src/types/api.ts`) y hubs de **SignalR** para eventos en tiempo real.
- **Acceso a Datos:** **Entity Framework Core** como ORM para el modelado relacional, gestión de migraciones, consultas optimizadas e integridad referencial.
- **Base de Datos:** **SQL Server** para la persistencia transaccional de usuarios, publicaciones, coordenadas geográficas, refugios, historial de mensajes y estados de moderación.

---

## Estructura del proyecto

```text
AnimalRescue/                           # Solución .NET (Arquitectura en 3 capas)
├── AnimalRescue.API/                   # Capa de Presentación (REST API + SignalR)
│   ├── Auth/                           # Contexto de usuario actual y políticas de autorización
│   │   ├── CurrentUser.cs
│   │   └── Policies.cs
│   ├── Controllers/                    # Endpoints HTTP organizados por módulo
│   │   ├── Admin/
│   │   │   └── AdminControllers.cs
│   │   ├── AuthController.cs
│   │   ├── ConversacionesController.cs
│   │   ├── PublicacionesController.cs
│   │   └── RescueControllers.cs
│   ├── Hubs/                           # Comunicación bidireccional en tiempo real
│   │   └── ChatHub.cs
│   ├── Middleware/                     # Manejo global centralizado de excepciones
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Models/                         # Modelos de entrada y formularios multipart
│   │   └── Formularios.cs
│   ├── uploads/                        # Almacenamiento local de archivos multimedia
│   │   ├── mensajes/
│   │   └── publicaciones/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                      # Configuración de pipeline, DI, JWT y SignalR
│
├── AnimalRescue.BLL/                   # Capa de Lógica de Negocio
│   ├── DTOs/                           # Objetos de transferencia de datos por dominio
│   │   ├── Admin/AdminDtos.cs
│   │   ├── Auth/AuthDtos.cs
│   │   ├── Comun/ComunDtos.cs
│   │   ├── Mensajeria/MensajeriaDtos.cs
│   │   ├── Publicaciones/PublicacionDtos.cs
│   │   ├── Reportes/ReportesDtos.cs
│   │   └── Rescue/RescueDtos.cs
│   ├── Exceptions/                     # Excepciones personalizadas de negocio
│   │   └── Excepciones.cs
│   ├── Interfaces/                     # Contratos de servicios
│   │   └── IServicios.cs
│   ├── Mapping/                        # Transformación entre entidades y DTOs
│   │   └── PublicacionMapeo.cs
│   ├── Options/                        # Configuración fuertemente tipada (JWT)
│   │   └── JwtOptions.cs
│   ├── Services/                       # Implementación de reglas de negocio
│   │   ├── AdminService.cs
│   │   ├── AuthService.cs
│   │   ├── GeocodificacionService.cs
│   │   ├── MensajeriaService.cs
│   │   ├── ProcesadorImagenes.cs
│   │   ├── PublicacionService.cs
│   │   ├── ReporteService.cs
│   │   ├── RescueServices.cs
│   │   └── TokenService.cs
│   ├── Validators/                     # Reglas de validación de entrada
│   │   └── Validadores.cs
│   └── DependencyInjection.cs          # Registro de servicios de la capa BLL
│
└── AnimalRescue.DAL/                   # Capa de Acceso a Datos (EF Core + SQL Server)
    ├── Configurations/                 # Configuración Fluent API de entidades
    │   ├── CuentaConfigurations.cs
    │   ├── MensajeriaConfigurations.cs
    │   ├── PublicacionConfigurations.cs
    │   └── RescueConfigurations.cs
    ├── Context/                        # DbContext y configuración de esquema inicial
    │   ├── AnimalRescueDbContext.cs
    │   └── EsquemaInicial.cs
    ├── Entities/                       # Entidades del modelo relacional y enumeraciones
    │   ├── Enums.cs
    │   ├── Mensajeria.cs
    │   ├── Moderacion.cs
    │   ├── Publicacion.cs
    │   ├── Rescue.cs
    │   └── Usuario.cs
    ├── External/                       # Integraciones con servicios externos
    │   ├── Geocoding/
    │   │   ├── IGeocodificador.cs
    │   │   └── NominatimGeocodificador.cs
    │   └── Storage/
    ├── Repositories/                   # Implementación del patrón Repository genérico
    │   ├── IRepository.cs
    │   └── Repository.cs
    ├── Scripts/                        # Scripts SQL de inicialización
    │   └── 01_esquema.sql
    ├── UnitOfWork/                     # Gestión transaccional coordinada
    │   ├── IUnitOfWork.cs
    │   └── UnitOfWork.cs
    └── DependencyInjection.cs          # Registro de DbContext, repositorios y Unit of Work
```[cite: 1, 2, 3]
