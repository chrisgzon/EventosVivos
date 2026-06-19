using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Entities;

/// <summary>
/// Entidad Reservation — nucleo del flujo de compra de entradas.
/// Encapsula las reglas RF-03 (crear), RF-04 (confirmar pago), RF-05 (cancelar)
/// y las reglas de negocio RN04, RN05, RN07.
/// </summary>
public sealed class Reservation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = default!;
    public int Quantity { get; private set; }
    public string BuyerName { get; private set; } = default!;
    public string BuyerEmail { get; private set; } = default!;
    public ReservationStatus Status { get; private set; }
    public string? ReservationCode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>
    /// Si se cancela una reserva confirmada con menos de 48h del evento (RN07),
    /// las entradas se marcan "perdidas" (no se liberan para venta, solo reporte).
    /// </summary>
    public bool IsLostOnCancellation { get; private set; }

    // Required by EF Core
    private Reservation() { }

    // -----------------------------------------------------------------------
    // Factory — RF-03
    // -----------------------------------------------------------------------

    public static Reservation Create(
        Event @event,
        int quantity,
        string buyerName,
        string buyerEmail,
        DateTime nowUtc)
    {
        if (!IsValidEmail(buyerEmail))
            throw new BusinessRuleViolationException("RF03",
                "El formato del email del comprador no es válido.");

        if (quantity < 1)
            throw new BusinessRuleViolationException("RF03",
                "La cantidad de entradas debe ser al menos 1.");

        if (string.IsNullOrWhiteSpace(buyerName))
            throw new BusinessRuleViolationException("RF03",
                "El nombre del comprador es obligatorio.");

        var hoursToStart = (@event.StartDateTimeUtc - nowUtc).TotalHours;
        if (hoursToStart < 1)
            throw new BusinessRuleViolationException("RN04",
                "No se pueden realizar reservas para eventos que inician en menos de 1 hora.");

        if (hoursToStart < 24 && quantity > 5)
            throw new BusinessRuleViolationException("RF03",
                "Para eventos que inician en menos de 24 horas sólo se permiten máximo 5 entradas por transacción.");

        if (@event.TicketPrice > 100 && quantity > 10)
            throw new BusinessRuleViolationException("RN05",
                "Para eventos con precio superior a $100 se permiten máximo 10 entradas por transacción.");

        if (@event.AvailableTickets < quantity)
            throw new BusinessRuleViolationException("RF03",
                $"No hay suficientes entradas disponibles. Disponibles: {@event.AvailableTickets}, solicitadas: {quantity}.");

        // RF-03: sólo eventos activos
        @event.RefreshStatus(nowUtc);
        if (@event.Status != EventStatus.Activo)
            throw new BusinessRuleViolationException("RF03",
                $"No se pueden crear reservas para un evento en estado '{@event.Status}'.");

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            EventId = @event.Id,
            Event = @event,
            Quantity = quantity,
            BuyerName = buyerName.Trim(),
            BuyerEmail = buyerEmail.Trim().ToLowerInvariant(),
            Status = ReservationStatus.PendientePago,
            CreatedAtUtc = nowUtc
        };

        @event.AddReservation(reservation);
        return reservation;
    }

    // -----------------------------------------------------------------------
    // Behaviour — RF-04: Confirmar pago
    // -----------------------------------------------------------------------

    public void ConfirmPayment(DateTime nowUtc)
    {
        if (Status == ReservationStatus.Confirmada)
            throw new InvalidStateTransitionException("Reserva", "Confirmada", "ConfirmarPago");

        if (Status == ReservationStatus.Cancelada)
            throw new InvalidStateTransitionException("Reserva", "Cancelada", "ConfirmarPago");

        Status = ReservationStatus.Confirmada;
        ReservationCode = GenerateReservationCode();
        ConfirmedAtUtc = nowUtc;
    }

    // -----------------------------------------------------------------------
    // Behaviour — RF-05: Cancelar reserva
    // -----------------------------------------------------------------------

    public void Cancel(DateTime nowUtc)
    {
        if (Status == ReservationStatus.Cancelada)
            throw new InvalidStateTransitionException("Reserva", "Cancelada", "Cancelar");

        // RN07 — cancelación con penalización: confirmada + < 48h antes del evento
        if (Status == ReservationStatus.Confirmada)
        {
            var hoursToEvent = (Event.StartDateTimeUtc - nowUtc).TotalHours;
            if (hoursToEvent < 48)
            {
                // Se marca perdida → no libera entradas
                IsLostOnCancellation = true;
            }
        }

        Status = ReservationStatus.Cancelada;
        CancelledAtUtc = nowUtc;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Indica si esta reserva cuenta como "ocupación efectiva" para el venue:
    /// - Confirmadas siempre.
    /// - Canceladas perdidas (RN07): siguen contando para el reporte aunque estén canceladas.
    /// </summary>
    public bool CountsAsOccupied =>
        Status == ReservationStatus.Confirmada
        || (Status == ReservationStatus.Cancelada && IsLostOnCancellation);

    private static string GenerateReservationCode()
    {
        var digits = Random.Shared.Next(0, 999999).ToString("D6");
        return $"EV-{digits}";
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim().ToLowerInvariant()
                   || addr.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
