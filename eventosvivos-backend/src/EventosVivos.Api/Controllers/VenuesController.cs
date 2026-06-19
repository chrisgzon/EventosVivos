using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Gestión de recintos (venues).
/// Permite listar los recintos disponibles.
/// </summary>
[ApiController]
[Route("api/venues")]
[Produces("application/json")]
public sealed class VenuesController : ControllerBase
{
    private readonly IVenueRepository _venueRepo;
    public VenuesController(IVenueRepository venueRepo) => _venueRepo = venueRepo;

    /// <summary>
    /// Obtiene el listado de recintos.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Listado resumido de recintos.</returns>
    /// <response code="200">Devuelve la lista de recintos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var venues = await _venueRepo.GetAllAsync(ct);
        var response = venues.Select(v => new VenueSummaryResponse(v.Id, v.Name, v.Capacity, v.City)).ToList();
        return Ok(response);
    }
}
