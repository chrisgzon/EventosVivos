using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/venues")]
[Produces("application/json")]
public sealed class VenuesController : ControllerBase
{
    private readonly IVenueRepository _venueRepo;
    public VenuesController(IVenueRepository venueRepo) => _venueRepo = venueRepo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var venues = await _venueRepo.GetAllAsync(ct);
        var response = venues.Select(v => new VenueSummaryResponse(v.Id, v.Name, v.Capacity, v.City)).ToList();
        return Ok(response);
    }
}
