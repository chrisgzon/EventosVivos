using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using EventosVivos.Application.Services;
using EventosVivos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Gestión de eventos culturales.
/// Cubre RF-01 (crear), RF-02 (listar con filtros) y RF-06 (reporte de ocupación).
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController : ControllerBase
{
    private readonly EventService _eventService;

    public EventsController(EventService eventService)
        => _eventService = eventService;

    // GET api/events
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] EventType? type = null,
        [FromQuery] DateTime? startFrom = null,
        [FromQuery] DateTime? startTo = null,
        [FromQuery] int? venueId = null,
        [FromQuery] EventStatus? status = null,
        [FromQuery] string? titleSearch = null,
        CancellationToken ct = default)
    {
        var request = new ListEventsRequest(type, startFrom, startTo, venueId, status, titleSearch);
        var result = await _eventService.GetAllAsync(request, ct);
        return Ok(result);
    }

    // GET api/events/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _eventService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    // POST api/events
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var result = await _eventService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET api/events/{id}/occupancy
    [HttpGet("{id:guid}/occupancy")]
    [ProducesResponseType(typeof(OccupancyReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOccupancy(Guid id, CancellationToken ct)
    {
        var result = await _eventService.GetOccupancyReportAsync(id, ct);
        return Ok(result);
    }
}
