using EventosVivos.Application.DTOs;
using EventosVivos.Application.Services.Interfaces;
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
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
        => _eventService = eventService;

    /// <summary>
    /// Obtiene la lista de eventos aplicando los filtros opcionales.
    /// </summary>
    /// <param name="type">Tipo de evento (opcional).</param>
    /// <param name="startFrom">Fecha de inicio mínima (opcional).</param>
    /// <param name="startTo">Fecha de inicio máxima (opcional).</param>
    /// <param name="venueId">Id del recinto (opcional).</param>
    /// <param name="status">Estado del evento (opcional).</param>
    /// <param name="titleSearch">Texto para buscar en el título (opcional).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Listado de eventos que cumplen los filtros.</returns>
    /// <response code="200">Devuelve la lista de eventos.</response>
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

    /// <summary>
    /// Obtiene un evento por su identificador.
    /// </summary>
    /// <param name="id">Identificador del evento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Detalle del evento solicitado.</returns>
    /// <response code="200">Devuelve el evento.</response>
    /// <response code="404">Si no se encuentra el evento.</response>
    // GET api/events/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _eventService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo evento.
    /// </summary>
    /// <param name="request">Datos para crear el evento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El evento creado.</returns>
    /// <response code="201">Evento creado correctamente.</response>
    /// <response code="422">Si los datos de entrada no son válidos.</response>
    // POST api/events
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var result = await _eventService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Obtiene el reporte de ocupación de un evento.
    /// </summary>
    /// <param name="id">Identificador del evento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Reporte de ocupación del evento.</returns>
    /// <response code="200">Devuelve el reporte de ocupación.</response>
    /// <response code="404">Si el evento no existe.</response>
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
