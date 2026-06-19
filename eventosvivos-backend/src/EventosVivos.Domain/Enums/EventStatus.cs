namespace EventosVivos.Domain.Enums;

/// <summary>
/// Estado de un evento. "Completado" se calcula automáticamente (RN06) cuando
/// la fecha/hora actual supera la fecha de fin del evento.
/// </summary>
public enum EventStatus
{
    Activo = 0,
    Cancelado = 1,
    Completado = 2
}
