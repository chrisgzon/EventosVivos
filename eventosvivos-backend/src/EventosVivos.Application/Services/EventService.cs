using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Services;

/// <summary>
/// Servicio de aplicación para la gestión de Eventos.
/// Orquesta las operaciones RF-01, RF-02 y RF-06, y delega las reglas
/// de dominio a las entidades del modelo.
/// </summary>
public sealed class EventService
{
    private readonly IEventRepository _eventRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public EventService(
        IEventRepository eventRepo,
        IVenueRepository venueRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _eventRepo = eventRepo;
        _venueRepo = venueRepo;
        _uow = uow;
        _clock = clock;
    }

    // -----------------------------------------------------------------------
    // RF-01: Crear Evento
    // -----------------------------------------------------------------------

    public async Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepo.GetByIdAsync(request.VenueId, ct)
            ?? throw new EntityNotFoundException(nameof(Venue), request.VenueId);

        // RN02 — Superposición de venue
        var overlapping = await _eventRepo.GetOverlappingEventsAsync(
            request.VenueId, request.StartDateTimeUtc, request.EndDateTimeUtc, null, ct);

        if (overlapping.Any())
        {
            var conflict = overlapping.First();
            throw new BusinessRuleViolationException("RN02",
                $"El venue '{venue.Name}' ya tiene un evento activo ('{conflict.Title}') " +
                $"con horario superpuesto: {conflict.StartDateTimeUtc:g} - {conflict.EndDateTimeUtc:g} UTC.");
        }

        var @event = Event.Create(
            request.Title,
            request.Description,
            venue,
            request.MaxCapacity,
            request.StartDateTimeUtc,
            request.EndDateTimeUtc,
            request.TicketPrice,
            request.Type,
            _clock.UtcNow);

        await _eventRepo.AddAsync(@event, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(@event);
    }

    // -----------------------------------------------------------------------
    // RF-02: Listar Eventos con filtros
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<EventResponse>> GetAllAsync(ListEventsRequest request, CancellationToken ct = default)
    {
        var events = await _eventRepo.GetAllAsync(
            request.Type, request.StartFrom, request.StartTo,
            request.VenueId, request.Status, request.TitleSearch, ct);

        var now = _clock.UtcNow;
        foreach (var e in events) e.RefreshStatus(now); // RN06

        return events.Select(MapToResponse).ToList().AsReadOnly();
    }

    // -----------------------------------------------------------------------
    // RF-06: Reporte de Ocupación
    // -----------------------------------------------------------------------

    public async Task<OccupancyReportResponse> GetOccupancyReportAsync(Guid eventId, CancellationToken ct = default)
    {
        var @event = await _eventRepo.GetByIdWithReservationsAsync(eventId, ct)
            ?? throw new EntityNotFoundException(nameof(Event), eventId);

        @event.RefreshStatus(_clock.UtcNow);

        var confirmedTickets = @event.ConfirmedTickets;
        var available = @event.AvailableTickets;
        var occupancyPct = @event.MaxCapacity > 0
            ? Math.Round((decimal)confirmedTickets / @event.MaxCapacity * 100, 2)
            : 0m;
        var revenue = confirmedTickets * @event.TicketPrice;

        return new OccupancyReportResponse(
            @event.Id,
            @event.Title,
            @event.MaxCapacity,
            confirmedTickets,
            available,
            occupancyPct,
            revenue,
            @event.Status.ToString());
    }

    // -----------------------------------------------------------------------
    // GET by ID (helper for frontend)
    // -----------------------------------------------------------------------

    public async Task<EventResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await _eventRepo.GetByIdWithReservationsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Event), id);

        @event.RefreshStatus(_clock.UtcNow);
        return MapToResponse(@event);
    }

    // -----------------------------------------------------------------------
    // Mapper
    // -----------------------------------------------------------------------

    private static EventResponse MapToResponse(Event e) =>
        new(e.Id,
            e.Title,
            e.Description,
            new VenueSummaryResponse(e.Venue.Id, e.Venue.Name, e.Venue.Capacity, e.Venue.City),
            e.MaxCapacity,
            e.AvailableTickets,
            e.StartDateTimeUtc,
            e.EndDateTimeUtc,
            e.TicketPrice,
            e.Type.ToString(),
            e.Status.ToString(),
            e.CreatedAtUtc);
}
