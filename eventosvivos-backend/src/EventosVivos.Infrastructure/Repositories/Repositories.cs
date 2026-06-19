using EventosVivos.Application.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

// ---------------------------------------------------------------------------
// EventRepository
// ---------------------------------------------------------------------------

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Events.Include(e => e.Venue).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Event?> GetByIdWithReservationsAsync(Guid id, CancellationToken ct) =>
        _db.Events
           .Include(e => e.Venue)
           .Include(e => e.Reservations)
           .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Event>> GetAllAsync(
        EventType? type,
        DateTime? startFrom,
        DateTime? startTo,
        int? venueId,
        EventStatus? status,
        string? titleSearch,
        CancellationToken ct)
    {
        var query = _db.Events.Include(e => e.Venue).AsQueryable();

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (startFrom.HasValue)
            query = query.Where(e => e.StartDateTimeUtc >= startFrom.Value);

        if (startTo.HasValue)
            query = query.Where(e => e.StartDateTimeUtc <= startTo.Value);

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(titleSearch))
            query = query.Where(e => EF.Functions.ILike(e.Title, $"%{titleSearch}%"));

        return await query.OrderBy(e => e.StartDateTimeUtc).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Event>> GetOverlappingEventsAsync(
        int venueId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeEventId,
        CancellationToken ct)
    {
        var query = _db.Events.Where(e =>
            e.VenueId == venueId &&
            e.Status != EventStatus.Cancelado &&
            e.StartDateTimeUtc < endUtc &&
            e.EndDateTimeUtc > startUtc);

        if (excludeEventId.HasValue)
            query = query.Where(e => e.Id != excludeEventId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Event @event, CancellationToken ct)
        => await _db.Events.AddAsync(@event, ct);

    public Task UpdateAsync(Event @event, CancellationToken ct)
    {
        _db.Events.Update(@event);
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// ReservationRepository
// ---------------------------------------------------------------------------

public sealed class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _db;
    public ReservationRepository(AppDbContext db) => _db = db;

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Reservation?> GetByIdWithEventAsync(Guid id, CancellationToken ct) =>
        _db.Reservations
           .Include(r => r.Event).ThenInclude(e => e.Venue)
           .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct) =>
        await _db.Reservations
                 .Include(r => r.Event).ThenInclude(e => e.Venue)
                 .Where(r => r.EventId == eventId)
                 .OrderByDescending(r => r.CreatedAtUtc)
                 .ToListAsync(ct);

    public async Task AddAsync(Reservation reservation, CancellationToken ct)
        => await _db.Reservations.AddAsync(reservation, ct);

    public Task UpdateAsync(Reservation reservation, CancellationToken ct)
    {
        _db.Reservations.Update(reservation);
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// VenueRepository
// ---------------------------------------------------------------------------

public sealed class VenueRepository : IVenueRepository
{
    private readonly AppDbContext _db;
    public VenueRepository(AppDbContext db) => _db = db;

    public Task<Venue?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Venues.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct) =>
        await _db.Venues.OrderBy(v => v.Name).ToListAsync(ct);
}
