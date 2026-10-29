-- =====================================================================
-- Animal Rescue - Esquema
-- Ejecutar conectado a la base animal_rescue:
--   psql -h localhost -U postgres -d animal_rescue -f database/01_esquema.sql
-- Requiere PostgreSQL 13+ (gen_random_uuid() nativo).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- Funciones de triggers
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_set_fecha_actualizacion()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.fecha_actualizacion := now();
    RETURN NEW;
END;
$$;

-- =====================================================================
-- MÓDULO MI CUENTA
-- =====================================================================
CREATE TABLE usuarios (
    id                  uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre              varchar(100) NOT NULL,
    email               varchar(254) NOT NULL,
    password_hash       text         NOT NULL,
    rol                 varchar(20)  NOT NULL DEFAULT 'USUARIO',
    foto_url            text,
    activo              boolean      NOT NULL DEFAULT true,
    promovido_por_id    uuid         REFERENCES usuarios (id) ON DELETE SET NULL,
    fecha_creacion      timestamptz  NOT NULL DEFAULT now(),
    fecha_actualizacion timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_usuarios_email   UNIQUE (email),
    CONSTRAINT ck_usuarios_email   CHECK (email = lower(email) AND position('@' IN email) > 1),
    CONSTRAINT ck_usuarios_nombre  CHECK (btrim(nombre) <> ''),
    CONSTRAINT ck_usuarios_rol     CHECK (rol IN ('USUARIO', 'ADMIN', 'SUPERADMIN'))
);
COMMENT ON TABLE  usuarios IS 'Cuentas de la aplicación';
COMMENT ON COLUMN usuarios.email IS 'Siempre en minúsculas (lo normaliza el backend)';
COMMENT ON COLUMN usuarios.password_hash IS 'Hash BCrypt; nunca la contraseña en claro';
COMMENT ON COLUMN usuarios.promovido_por_id IS 'SUPERADMIN que otorgó el rol ADMIN';

CREATE INDEX ix_usuarios_rol ON usuarios (rol) WHERE rol <> 'USUARIO';

CREATE TRIGGER trg_usuarios_fecha_actualizacion
    BEFORE UPDATE ON usuarios
    FOR EACH ROW EXECUTE FUNCTION fn_set_fecha_actualizacion();


CREATE TABLE refresh_tokens (
    id                 uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id         uuid         NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    token_hash         varchar(128) NOT NULL,
    fecha_expiracion   timestamptz  NOT NULL,
    fecha_revocacion   timestamptz,
    reemplazado_por_id uuid         REFERENCES refresh_tokens (id) ON DELETE SET NULL,
    fecha_creacion     timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_refresh_tokens_hash UNIQUE (token_hash)
);
COMMENT ON TABLE refresh_tokens IS 'Refresh tokens JWT (sólo hash SHA-256), con rotación';

CREATE INDEX ix_refresh_tokens_usuario ON refresh_tokens (usuario_id);

-- =====================================================================
-- MÓDULOS INICIO / PUBLICAR / MIS MASCOTAS / MAPA
-- =====================================================================
CREATE TABLE publicaciones (
    id                  uuid             PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id          uuid             NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    nombre_mascota      varchar(80)      NOT NULL,
    color               varchar(60)      NOT NULL,
    tamano              varchar(10)      NOT NULL,
    edad                varchar(30),
    castrado            boolean          NOT NULL,
    tiene_collar        boolean          NOT NULL DEFAULT false,
    tiene_chapita_qr    boolean          NOT NULL DEFAULT false,
    descripcion         text,
    fecha_perdida       date             NOT NULL,
    recompensa          numeric(12, 2),
    ubicacion_texto     varchar(300)     NOT NULL,
    latitud             double precision NOT NULL,
    longitud            double precision NOT NULL,
    estado              varchar(12)      NOT NULL DEFAULT 'ACTIVA',
    fecha_eliminacion   timestamptz,
    eliminada_por_id    uuid             REFERENCES usuarios (id) ON DELETE SET NULL,
    fecha_creacion      timestamptz      NOT NULL DEFAULT now(),
    fecha_actualizacion timestamptz      NOT NULL DEFAULT now(),
    CONSTRAINT ck_publicaciones_nombre      CHECK (btrim(nombre_mascota) <> ''),
    CONSTRAINT ck_publicaciones_tamano      CHECK (tamano IN ('GRANDE', 'MEDIANO', 'PEQUENO')),
    CONSTRAINT ck_publicaciones_descripcion CHECK (descripcion IS NULL OR length(descripcion) <= 2000),
    CONSTRAINT ck_publicaciones_recompensa  CHECK (recompensa IS NULL OR recompensa >= 0),
    CONSTRAINT ck_publicaciones_latitud     CHECK (latitud  BETWEEN -90  AND 90),
    CONSTRAINT ck_publicaciones_longitud    CHECK (longitud BETWEEN -180 AND 180),
    CONSTRAINT ck_publicaciones_estado      CHECK (estado IN ('ACTIVA', 'ENCONTRADA', 'ELIMINADA')),
    CONSTRAINT ck_publicaciones_eliminacion CHECK ((estado = 'ELIMINADA') = (fecha_eliminacion IS NOT NULL))
);
COMMENT ON TABLE  publicaciones IS 'Publicaciones de mascotas perdidas';
COMMENT ON COLUMN publicaciones.tamano IS 'GRANDE | MEDIANO | PEQUENO';
COMMENT ON COLUMN publicaciones.edad IS 'Texto libre: "2 años", "6 meses", "cachorro"';
COMMENT ON COLUMN publicaciones.estado IS 'ELIMINADA = borrado lógico (por el autor o por un admin)';
COMMENT ON COLUMN publicaciones.eliminada_por_id IS 'Autor o administrador que la eliminó';

-- Feed (Inicio): activas, más recientes primero
CREATE INDEX ix_publicaciones_feed    ON publicaciones (fecha_creacion DESC) WHERE estado = 'ACTIVA';
-- Mis Mascotas
CREATE INDEX ix_publicaciones_usuario ON publicaciones (usuario_id, fecha_creacion DESC) WHERE estado <> 'ELIMINADA';
-- Mapa: consulta por rectángulo visible
CREATE INDEX ix_publicaciones_geo     ON publicaciones (latitud, longitud) WHERE estado = 'ACTIVA';

CREATE TRIGGER trg_publicaciones_fecha_actualizacion
    BEFORE UPDATE ON publicaciones
    FOR EACH ROW EXECUTE FUNCTION fn_set_fecha_actualizacion();


CREATE TABLE imagenes_publicacion (
    id             uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    publicacion_id uuid        NOT NULL REFERENCES publicaciones (id) ON DELETE CASCADE,
    url            text        NOT NULL,
    url_miniatura  text        NOT NULL,
    orden          smallint    NOT NULL DEFAULT 0,
    fecha_creacion timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_imagenes_publicacion_orden CHECK (orden BETWEEN 0 AND 9),
    -- DEFERRABLE para poder reordenar (intercambiar posiciones) en una transacción
    CONSTRAINT uq_imagenes_publicacion_orden UNIQUE (publicacion_id, orden) DEFERRABLE INITIALLY DEFERRED
);
COMMENT ON TABLE  imagenes_publicacion IS 'Fotos de la mascota; orden 0 = imagen principal de la card';

-- =====================================================================
-- MÓDULO MENSAJES
-- =====================================================================
CREATE TABLE conversaciones (
    id                      uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario1_id             uuid        NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    usuario2_id             uuid        NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    publicacion_id          uuid        REFERENCES publicaciones (id) ON DELETE SET NULL,
    usuario1_ultima_lectura timestamptz,
    usuario2_ultima_lectura timestamptz,
    usuario1_eliminada_en   timestamptz,
    usuario2_eliminada_en   timestamptz,
    fecha_ultimo_mensaje    timestamptz,
    fecha_creacion          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_conversaciones_distintos CHECK (usuario1_id <> usuario2_id)
);
COMMENT ON TABLE  conversaciones IS 'Chat directo entre dos usuarios (uno por par)';
COMMENT ON COLUMN conversaciones.usuario1_id IS 'Quien inició la conversación';
COMMENT ON COLUMN conversaciones.publicacion_id IS 'Publicación desde la que se inició el chat';
COMMENT ON COLUMN conversaciones.usuario1_eliminada_en IS 'Momento en que usuario1 pulsó "X"; sólo ve mensajes posteriores';

-- Un único chat por par de usuarios, sin importar quién lo inició
CREATE UNIQUE INDEX ux_conversaciones_par
    ON conversaciones (LEAST(usuario1_id, usuario2_id), GREATEST(usuario1_id, usuario2_id));
CREATE INDEX ix_conversaciones_usuario1 ON conversaciones (usuario1_id, fecha_ultimo_mensaje DESC);
CREATE INDEX ix_conversaciones_usuario2 ON conversaciones (usuario2_id, fecha_ultimo_mensaje DESC);


CREATE TABLE mensajes (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    conversacion_id uuid        NOT NULL REFERENCES conversaciones (id) ON DELETE CASCADE,
    remitente_id    uuid        NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    contenido       text,
    imagen_url      text,
    fecha_envio     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_mensajes_no_vacio CHECK (nullif(btrim(contenido), '') IS NOT NULL OR imagen_url IS NOT NULL),
    CONSTRAINT ck_mensajes_longitud CHECK (contenido IS NULL OR length(contenido) <= 4000)
);
COMMENT ON TABLE mensajes IS 'Mensajes de texto y/o imagen';

CREATE INDEX ix_mensajes_conversacion ON mensajes (conversacion_id, fecha_envio DESC);

-- Valida que el remitente participe en la conversación
CREATE OR REPLACE FUNCTION fn_mensajes_validar_remitente()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM conversaciones c
        WHERE c.id = NEW.conversacion_id
          AND NEW.remitente_id IN (c.usuario1_id, c.usuario2_id)
    ) THEN
        RAISE EXCEPTION 'El remitente % no participa en la conversación %', NEW.remitente_id, NEW.conversacion_id
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_mensajes_validar_remitente
    BEFORE INSERT ON mensajes
    FOR EACH ROW EXECUTE FUNCTION fn_mensajes_validar_remitente();

-- Mantiene la fecha del último mensaje (orden de la lista de chats)
CREATE OR REPLACE FUNCTION fn_mensajes_actualizar_conversacion()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE conversaciones
       SET fecha_ultimo_mensaje = GREATEST(COALESCE(fecha_ultimo_mensaje, NEW.fecha_envio), NEW.fecha_envio)
     WHERE id = NEW.conversacion_id;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_mensajes_actualizar_conversacion
    AFTER INSERT ON mensajes
    FOR EACH ROW EXECUTE FUNCTION fn_mensajes_actualizar_conversacion();

-- =====================================================================
-- MÓDULO RESCUE
-- =====================================================================
CREATE TABLE hogares_transito (
    id             uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id     uuid         NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    nombre         varchar(100) NOT NULL,
    telefono       varchar(30)  NOT NULL,
    email          varchar(254) NOT NULL,
    ciudad         varchar(100) NOT NULL,
    pais           varchar(100) NOT NULL,
    fecha_creacion timestamptz  NOT NULL DEFAULT now(),
    -- Cada usuario puede postularse una sola vez
    CONSTRAINT uq_hogares_transito_usuario UNIQUE (usuario_id),
    CONSTRAINT ck_hogares_transito_email   CHECK (position('@' IN email) > 1),
    CONSTRAINT ck_hogares_transito_campos  CHECK (btrim(nombre) <> '' AND btrim(telefono) <> ''
                                                  AND btrim(ciudad) <> '' AND btrim(pais) <> '')
);
COMMENT ON TABLE hogares_transito IS 'Postulaciones de hogares de tránsito (una por usuario)';

CREATE INDEX ix_hogares_transito_ubicacion ON hogares_transito (pais, ciudad);


CREATE TABLE refugios (
    id             uuid             PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre         varchar(150),
    direccion      varchar(300)     NOT NULL,
    latitud        double precision NOT NULL,
    longitud       double precision NOT NULL,
    creado_por_id  uuid             REFERENCES usuarios (id) ON DELETE SET NULL,
    fecha_creacion timestamptz      NOT NULL DEFAULT now(),
    CONSTRAINT ck_refugios_direccion CHECK (btrim(direccion) <> ''),
    CONSTRAINT ck_refugios_latitud   CHECK (latitud  BETWEEN -90  AND 90),
    CONSTRAINT ck_refugios_longitud  CHECK (longitud BETWEEN -180 AND 180)
);
COMMENT ON TABLE  refugios IS 'Refugios mostrados en el mapa de Rescue';
COMMENT ON COLUMN refugios.creado_por_id IS 'Puede eliminarlo (mantener presionado) quien lo creó o un admin';

CREATE INDEX ix_refugios_geo         ON refugios (latitud, longitud);
CREATE INDEX ix_refugios_creado_por  ON refugios (creado_por_id);

-- =====================================================================
-- REPORTES Y ADMINISTRACIÓN
-- =====================================================================
CREATE TABLE reportes (
    id               uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    publicacion_id   uuid        NOT NULL REFERENCES publicaciones (id) ON DELETE CASCADE,
    reportado_por_id uuid        NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
    motivo           varchar(30) NOT NULL,
    comentario       varchar(500),
    estado           varchar(12) NOT NULL DEFAULT 'PENDIENTE',
    revisado_por_id  uuid        REFERENCES usuarios (id) ON DELETE SET NULL,
    fecha_revision   timestamptz,
    nota_revision    varchar(500),
    fecha_creacion   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_reportes_usuario_publicacion UNIQUE (publicacion_id, reportado_por_id),
    CONSTRAINT ck_reportes_motivo  CHECK (motivo IN ('INFORMACION_FALSA', 'CONTENIDO_INAPROPIADO', 'SPAM',
                                                     'DUPLICADA', 'MASCOTA_YA_ENCONTRADA', 'OTRO')),
    CONSTRAINT ck_reportes_estado  CHECK (estado IN ('PENDIENTE', 'ACEPTADO', 'RECHAZADO')),
    CONSTRAINT ck_reportes_revision CHECK ((estado = 'PENDIENTE') = (fecha_revision IS NULL))
);
COMMENT ON TABLE  reportes IS 'Reportes de publicaciones indebidas o incorrectas';
COMMENT ON COLUMN reportes.estado IS 'ACEPTADO = la publicación fue eliminada; RECHAZADO = se mantuvo';

CREATE INDEX ix_reportes_pendientes  ON reportes (fecha_creacion) WHERE estado = 'PENDIENTE';
CREATE INDEX ix_reportes_reportado_por ON reportes (reportado_por_id);


CREATE TABLE auditoria_admin (
    id         bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id   uuid        REFERENCES usuarios (id) ON DELETE SET NULL,
    accion     varchar(40) NOT NULL,
    entidad    varchar(40) NOT NULL,
    entidad_id uuid,
    detalle    jsonb,
    fecha      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_auditoria_admin_accion CHECK (accion IN ('ELIMINAR_PUBLICACION', 'ACEPTAR_REPORTE',
                                                           'RECHAZAR_REPORTE', 'PROMOVER_ADMIN',
                                                           'REVOCAR_ADMIN', 'ELIMINAR_REFUGIO'))
);
COMMENT ON TABLE auditoria_admin IS 'Bitácora de acciones de administradores y superadministradores';

CREATE INDEX ix_auditoria_admin_fecha ON auditoria_admin (fecha DESC);
CREATE INDEX ix_auditoria_admin_admin ON auditoria_admin (admin_id, fecha DESC);

COMMIT;
