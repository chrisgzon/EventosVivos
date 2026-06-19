using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Entities;

/// <summary>
/// Entidad raíz de agregado Event.
/// Contiene TODA la lógica de negocio relacionada con la creación y ciclo de vida
/// del evento, incluyendo las reglas RN01-RN06.
/// </summary>
public sealed class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int VenueId { get; private set; }
    public Venue Venue { get; private set; } = default!;
    public int MaxCapacity { get; private set; }
    public DateTime StartDateTimeUtc { get; private set; }
    public DateTime EndDateTimeUtc { get; private set; }
    public decimal TicketPrice { get; private set; }
    public EventType Type { get; private set; }
    public EventStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<Reservation> _reservations = [];
    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

    // -----------------------------------------------------------------------
    // Computed / derived
    // -----------------------------------------------------------------------

    /// <summary>Entradas ya ocupadas (sólo reservas confirmadas).</summary>
    public int ConfirmedTickets =>
        _reservations.Where(r => r.Status == ReservationStatus.Confirmada || (r.Status == ReservationStatus.Cancelada && r.IsLostOnCancellation)).Sum(r => r.Quantity);

    /// <summary>Entradas disponibles (capacidad − confirmadas − pendientes de pago - canceladas perdidas).</summary>
    public int AvailableTickets =>
        MaxCapacity
        - _reservations
            .Where(r => (r.Status is ReservationStatus.Confirmada or ReservationStatus.PendientePago) || (r.Status == ReservationStatus.Cancelada && r.IsLostOnCancellation))
            .Sum(r => r.Quantity);

    // Required by EF Core
    private Event() { }

    // -----------------------------------------------------------------------
    // Factory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Crea un nuevo evento aplicando todas las validaciones del RF-01 y
    /// las reglas RN01, RN03.
    /// La validación de superposición de venue (RN02) se delega al servicio
    /// de dominio <see cref="EventosVivos.Application.Services.EventService"/>
    /// porque requiere consultar la base de datos.
    /// </summary>
    public static Event Create(
        string title,
        string description,
        Venue venue,
        int maxCapacity,
        DateTime startDateTimeUtc,
        DateTime endDateTimeUtc,
        decimal ticketPrice,
        EventType type,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 100)
            throw new BusinessRuleViolationException("RF01",
                "El título es obligatorio y debe tener entre 5 y 100 caracteres.");

        if (string.IsNullOrWhiteSpace(description) || description.Length < 10 || description.Length > 500)
            throw new BusinessRuleViolationException("RF01",
                "La descripción es obligatoria y debe tener entre 10 y 500 caracteres.");

        if (maxCapacity <= 0)
            throw new BusinessRuleViolationException("RF01",
                "La capacidad máxima debe ser un entero positivo.");

        if (maxCapacity > venue.Capacity)
            throw new BusinessRuleViolationException("RN01",
                $"La capacidad del evento ({maxCapacity}) excede la capacidad del venue '{venue.Name}' ({venue.Capacity}).");

        if (startDateTimeUtc <= nowUtc)
            throw new BusinessRuleViolationException("RF01",
                "La fecha y hora de inicio deben ser futuras.");

        if (endDateTimeUtc <= startDateTimeUtc)
            throw new BusinessRuleViolationException("RF01",
                "La fecha y hora de fin deben ser posteriores al inicio.");

        if (ticketPrice <= 0)
            throw new BusinessRuleViolationException("RF01",
                "El precio de entrada debe ser un decimal positivo.");

        var startLocal = startDateTimeUtc.ToLocalFromColombia();
        if ((startLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            && startLocal.Hour >= 22)
        {
            throw new BusinessRuleViolationException("RN03",
                "Los eventos de fin de semana no pueden iniciar después de las 22:00 (hora Colombia).");
        }

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            VenueId = venue.Id,
            Venue = venue,
            MaxCapacity = maxCapacity,
            StartDateTimeUtc = startDateTimeUtc,
            EndDateTimeUtc = endDateTimeUtc,
            TicketPrice = ticketPrice,
            Type = type,
            Status = EventStatus.Activo,
            CreatedAtUtc = nowUtc
        };
    }

    // -----------------------------------------------------------------------
    // Behaviour — state transitions (RN06)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Recalcula el estado del evento según la hora actual (RN06).
    /// Debe llamarse antes de servir el evento al cliente.
    /// </summary>
    public void RefreshStatus(DateTime nowUtc)
    {
        if (Status != EventStatus.Activo) return;
        Status = nowUtc > EndDateTimeUtc
            ? EventStatus.Completado
            : EventStatus.Activo;
    }

    public void Cancel()
    {
        if (Status == EventStatus.Cancelado)
            throw new InvalidStateTransitionException("Evento", "Cancelado", "Cancelar");
        Status = EventStatus.Cancelado;
    }

    // -----------------------------------------------------------------------
    // Internal helpers for reservation aggregate linkage
    // -----------------------------------------------------------------------

    internal void AddReservation(Reservation reservation)
        => _reservations.Add(reservation);
}

/// <summary>
/// Extensión para convertir UTC a hora Colombia (UTC-5), sin depender de
/// TimeZoneInfo por portabilidad entre sistemas operativos.
/// </summary>
internal static class DateTimeExtensions
{
    private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);

    public static DateTime ToLocalFromColombia(this DateTime utcDate)
        => utcDate + ColombiaOffset;
}
