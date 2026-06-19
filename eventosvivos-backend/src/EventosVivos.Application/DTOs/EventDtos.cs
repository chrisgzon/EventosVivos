using EventosVivos.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventosVivos.Application.DTOs;

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

public sealed record CreateEventRequest(
    [Required, StringLength(100, MinimumLength = 5)]  string Title,
    [Required, StringLength(500, MinimumLength = 10)] string Description,
    [Required]                                         int VenueId,
    [Required, Range(1, int.MaxValue)]                 int MaxCapacity,
    [Required]                                         DateTime StartDateTimeUtc,
    [Required]                                         DateTime EndDateTimeUtc,
    [Required, Range(0.01, double.MaxValue)]            decimal TicketPrice,
    [Required]                                         EventType Type
);

public sealed record ListEventsRequest(
    EventType? Type = null,
    DateTime? StartFrom = null,
    DateTime? StartTo = null,
    int? VenueId = null,
    EventStatus? Status = null,
    string? TitleSearch = null
);

// ---------------------------------------------------------------------------
// Responses
// ---------------------------------------------------------------------------

public sealed record VenueSummaryResponse(
    int Id,
    string Name,
    int Capacity,
    string City
);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string Description,
    VenueSummaryResponse Venue,
    int MaxCapacity,
    int AvailableTickets,
    DateTimeOffset StartDateTimeUtc,
    DateTimeOffset EndDateTimeUtc,
    decimal TicketPrice,
    string Type,
    string Status,
    DateTimeOffset CreatedAtUtc
);

public sealed record OccupancyReportResponse(
    Guid EventId,
    string EventTitle,
    int MaxCapacity,
    int ConfirmedTickets,
    int AvailableTickets,
    decimal OccupancyPercentage,
    decimal TotalRevenue,
    string Status
);
