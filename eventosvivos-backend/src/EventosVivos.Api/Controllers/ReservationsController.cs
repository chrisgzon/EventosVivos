using EventosVivos.Application.DTOs;
using EventosVivos.Application.Services;
using EventosVivos.Application.Services.Interfaces;
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
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
        => _reservationService = reservationService;

    /// <summary>
    /// Obtiene una reserva por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la reserva.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Detalle de la reserva.</returns>
    /// <response code="200">Devuelve la reserva.</response>
    /// <response code="404">Si no se encuentra la reserva.</response>
    // GET api/reservations/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las reservas de un evento.
    /// </summary>
    /// <param name="eventId">Identificador del evento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Listado de reservas del evento.</returns>
    /// <response code="200">Devuelve las reservas del evento.</response>
    // GET api/events/{eventId}/reservations  (nested route)
    [HttpGet("/api/events/{eventId:guid}/reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(Guid eventId, CancellationToken ct)
    {
        var result = await _reservationService.GetByEventAsync(eventId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva reserva.
    /// </summary>
    /// <param name="request">Datos para crear la reserva.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Reserva creada.</returns>
    /// <response code="201">Reserva creada correctamente.</response>
    /// <response code="422">Si los datos de entrada no son válidos.</response>
    // POST api/reservations
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var result = await _reservationService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Confirma el pago de una reserva.
    /// </summary>
    /// <param name="id">Identificador de la reserva.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Reserva actualizada con estado confirmado.</returns>
    /// <response code="200">Pago confirmado y reserva actualizada.</response>
    /// <response code="404">Si no se encuentra la reserva.</response>
    /// <response code="409">Si la reserva no puede ser confirmada (conflicto).</response>
    // PATCH api/reservations/{id}/confirm
    [HttpPatch("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.ConfirmPaymentAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancela una reserva existente.
    /// </summary>
    /// <param name="id">Identificador de la reserva.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Reserva actualizada con estado cancelado.</returns>
    /// <response code="200">Reserva cancelada correctamente.</response>
    /// <response code="404">Si no se encuentra la reserva.</response>
    /// <response code="409">Si la reserva no puede ser cancelada (conflicto).</response>
    // PATCH api/reservations/{id}/cancel
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.CancelAsync(id, ct);
        return Ok(result);
    }
}
