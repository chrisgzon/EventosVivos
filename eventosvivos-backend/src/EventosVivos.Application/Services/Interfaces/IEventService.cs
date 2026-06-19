using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Services.Interfaces
{
    public interface IEventService
    {
        Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken ct = default);

        Task<IReadOnlyList<EventResponse>> GetAllAsync(ListEventsRequest request, CancellationToken ct = default);

        Task<OccupancyReportResponse> GetOccupancyReportAsync(Guid eventId, CancellationToken ct = default);

        Task<EventResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
