using EventosVivos.Application.DTOs;
using EventosVivos.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Ciclo de vida de reservas: crear, confirmar pago y cancelar.
/// Cubre RF-03, RF-04 y RF-05.
/// </summary>
[ApiController]
[Route("api/reservations")]
[Produces("application/json")]
public sealed class ReservationsController : ControllerBase
{
    private readonly ReservationService _reservationService;

    public ReservationsController(ReservationService reservationService)
        => _reservationService = reservationService;

    // GET api/reservations/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    // GET api/events/{eventId}/reservations  (nested route)
    [HttpGet("/api/events/{eventId:guid}/reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(Guid eventId, CancellationToken ct)
    {
        var result = await _reservationService.GetByEventAsync(eventId, ct);
        return Ok(result);
    }

    // POST api/reservations
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var result = await _reservationService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // POST api/reservations/{id}/confirm
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.ConfirmPaymentAsync(id, ct);
        return Ok(result);
    }

    // POST api/reservations/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.CancelAsync(id, ct);
        return Ok(result);
    }
}
