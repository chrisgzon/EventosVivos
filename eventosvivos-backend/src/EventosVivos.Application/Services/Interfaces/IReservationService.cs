using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Services.Interfaces
{
    public interface IReservationService
    {
        Task<ReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
        Task<ReservationResponse> ConfirmPaymentAsync(Guid reservationId, CancellationToken ct = default);
        Task<ReservationResponse> CancelAsync(Guid reservationId, CancellationToken ct = default);
        Task<IReadOnlyList<ReservationResponse>> GetByEventAsync(Guid eventId, CancellationToken ct = default);
        Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
