using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.Interfaces;

public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct = default);
}
