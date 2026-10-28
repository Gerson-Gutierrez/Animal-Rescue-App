namespace AnimalRescue.DAL.Entities;

// Los nombres coinciden con los valores guardados en los CHECK de la base de datos.
public enum Rol { USUARIO, ADMIN, SUPERADMIN }

public enum Tamano { GRANDE, MEDIANO, PEQUENO }

public enum EstadoPublicacion { ACTIVA, ENCONTRADA, ELIMINADA }

public enum MotivoReporte
{
    INFORMACION_FALSA,
    CONTENIDO_INAPROPIADO,
    SPAM,
    DUPLICADA,
    MASCOTA_YA_ENCONTRADA,
    OTRO
}

public enum EstadoReporte { PENDIENTE, ACEPTADO, RECHAZADO }

public enum AccionAdmin
{
    ELIMINAR_PUBLICACION,
    ACEPTAR_REPORTE,
    RECHAZAR_REPORTE,
    PROMOVER_ADMIN,
    REVOCAR_ADMIN,
    ELIMINAR_REFUGIO
}
