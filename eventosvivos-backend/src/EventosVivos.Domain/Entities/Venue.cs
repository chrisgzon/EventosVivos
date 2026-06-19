using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Entities;

/// <summary>
/// Entidad Venue — lugar físico donde se realizan los eventos.
/// Los tres venues iniciales se cargan como datos de referencia en el seed.
/// </summary>
public sealed class Venue
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public string City { get; private set; } = default!;

    // Navigation
    private readonly List<Event> _events = [];
    public IReadOnlyCollection<Event> Events => _events.AsReadOnly();

    // Required for EF Core
    private Venue() { }

    public static Venue Create(int id, string name, int capacity, string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        if (capacity <= 0)
            throw new BusinessRuleViolationException("RN01", "La capacidad del venue debe ser un entero positivo.");

        return new Venue
        {
            Id = id,
            Name = name.Trim(),
            Capacity = capacity,
            City = city.Trim()
        };
    }
}
