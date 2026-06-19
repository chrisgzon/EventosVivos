namespace EventosVivos.Domain.Enums;

/// <summary>
/// Ciclo de vida de una reserva: pendiente_pago -> confirmada -> (opcional) cancelada,
/// o pendiente_pago -> cancelada directamente.
/// </summary>
public enum ReservationStatus
{
    PendientePago = 0,
    Confirmada = 1,
    Cancelada = 2
}
