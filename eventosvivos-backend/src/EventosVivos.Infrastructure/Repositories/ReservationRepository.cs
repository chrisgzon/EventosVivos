using EventosVivos.Application.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories
{
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
}
