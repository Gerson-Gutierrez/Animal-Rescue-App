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
src/
├── app/                  # Rutas y pantallas (expo-router)
│   ├── (tabs)/           # Módulos principales: Inicio, Mapa, Publicar, Mensajes, Rescue
│   ├── publicacion/[id]/ # Detalle y edición de reportes de mascotas
│   ├── chat/[id].tsx     # Sala de chat en tiempo real con envío de imágenes
│   ├── mis-mascotas.tsx  # Gestión de publicaciones propias
│   ├── cuenta.tsx        # Perfil, configuración y diagnóstico de conexión API
│   ├── login.tsx         # Autenticación
│   ├── registro.tsx      # Registro de usuarios
│   └── admin/            # Moderación: reportes, publicaciones y administradores
├── api/                  # Cliente HTTP (fetch + refresh token) y consumo de endpoints
├── features/             # Lógica y componentes de dominio (mapa, formularios, rescue, mensajes)
├── components/             # Sistema de componentes UI reutilizables (inputs, modales, avisos)
├── store/auth.ts         # Estado global de autenticación y persistencia segura de tokens
├── lib/                  # Configuración de entorno, tema visual y utilidades
└── types/api.ts          # Interfaces y DTOs sincronizados con el backend .NET
