using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Event?> GetByIdWithReservationsAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Event>> GetAllAsync(
        EventType? type = null,
        DateTime? startFrom = null,
        DateTime? startTo = null,
        int? venueId = null,
        EventStatus? status = null,
        string? titleSearch = null,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve eventos activos en el mismo venue cuyo rango horario se superpone
    /// con el rango dado. Utilizado para verificar RN02.
    /// </summary>
    Task<IReadOnlyList<Event>> GetOverlappingEventsAsync(
        int venueId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeEventId = null,
        CancellationToken ct = default);

    Task AddAsync(Event @event, CancellationToken ct = default);
    Task UpdateAsync(Event @event, CancellationToken ct = default);
}
