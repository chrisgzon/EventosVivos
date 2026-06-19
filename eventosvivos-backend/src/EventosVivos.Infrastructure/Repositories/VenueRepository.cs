using EventosVivos.Application.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories
{
    public sealed class VenueRepository : IVenueRepository
    {
        private readonly AppDbContext _db;
        public VenueRepository(AppDbContext db) => _db = db;

        public Task<Venue?> GetByIdAsync(int id, CancellationToken ct) =>
            _db.Venues.FirstOrDefaultAsync(v => v.Id == id, ct);

        public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct) =>
            await _db.Venues.OrderBy(v => v.Name).ToListAsync(ct);
    }
}
