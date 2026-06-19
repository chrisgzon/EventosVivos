using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using EventosVivos.Application.Services.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Services;

/// <summary>
/// Servicio de aplicación para el ciclo de vida de Reservas.
/// Orquesta RF-03 (crear), RF-04 (confirmar pago) y RF-05 (cancelar).
/// Las reglas de negocio RN04-RN07 viven en la entidad <see cref="Reservation"/>.
/// </summary>
public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public ReservationService(
        IReservationRepository reservationRepo,
        IEventRepository eventRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _reservationRepo = reservationRepo;
        _eventRepo = eventRepo;
        _uow = uow;
        _clock = clock;
    }

    // -----------------------------------------------------------------------
    // RF-03: Reservar Entrada
    // -----------------------------------------------------------------------

    public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        // Load event with all its reservations so AvailableTickets is accurate
        var @event = await _eventRepo.GetByIdWithReservationsAsync(request.EventId, ct)
            ?? throw new EntityNotFoundException(nameof(Event), request.EventId);

        var reservation = Reservation.Create(
            @event,
            request.Quantity,
            request.BuyerName,
            request.BuyerEmail,
            _clock.UtcNow);

        await _reservationRepo.AddAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(reservation);
    }

    // -----------------------------------------------------------------------
    // RF-04: Confirmar Pago de Reserva
    // -----------------------------------------------------------------------

    public async Task<ReservationResponse> ConfirmPaymentAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _reservationRepo.GetByIdWithEventAsync(reservationId, ct)
            ?? throw new EntityNotFoundException(nameof(Reservation), reservationId);

        reservation.ConfirmPayment(_clock.UtcNow);

        await _reservationRepo.UpdateAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(reservation);
    }

    // -----------------------------------------------------------------------
    // RF-05: Cancelar Reserva
    // -----------------------------------------------------------------------

    public async Task<ReservationResponse> CancelAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _reservationRepo.GetByIdWithEventAsync(reservationId, ct)
            ?? throw new EntityNotFoundException(nameof(Reservation), reservationId);

        reservation.Cancel(_clock.UtcNow);

        await _reservationRepo.UpdateAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(reservation);
    }

    // -----------------------------------------------------------------------
    // GET by event (admin view)
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<ReservationResponse>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var reservations = await _reservationRepo.GetByEventIdAsync(eventId, ct);
        return reservations.Select(MapToResponse).ToList().AsReadOnly();
    }

    public async Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await _reservationRepo.GetByIdWithEventAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Reservation), id);

        return MapToResponse(reservation);
    }

    // -----------------------------------------------------------------------
    // Mapper
    // -----------------------------------------------------------------------

    private static ReservationResponse MapToResponse(Reservation r) =>
        new(r.Id,
            r.EventId,
            r.Event?.Title ?? string.Empty,
            r.Quantity,
            r.BuyerName,
            r.BuyerEmail,
            r.Status.ToString(),
            r.ReservationCode,
            r.CreatedAtUtc,
            r.ConfirmedAtUtc,
            r.CancelledAtUtc,
            r.IsLostOnCancellation,
            r.Event is not null ? r.Quantity * r.Event.TicketPrice : 0m);
}
