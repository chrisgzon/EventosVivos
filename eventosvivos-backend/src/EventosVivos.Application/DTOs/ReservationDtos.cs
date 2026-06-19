using System.ComponentModel.DataAnnotations;

namespace EventosVivos.Application.DTOs;

// ---------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------

public sealed record CreateReservationRequest(
    [Required]                       Guid EventId,
    [Required, Range(1, int.MaxValue)] int Quantity,
    [Required]                       string BuyerName,
    [Required, EmailAddress]          string BuyerEmail
);

// ---------------------------------------------------------------------------
// Responses
// ---------------------------------------------------------------------------

public sealed record ReservationResponse(
    Guid Id,
    Guid EventId,
    string EventTitle,
    int Quantity,
    string BuyerName,
    string BuyerEmail,
    string Status,
    string? ReservationCode,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    bool IsLostOnCancellation,
    decimal TotalAmount
);
