using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Reservation?> GetByIdWithEventAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
}
