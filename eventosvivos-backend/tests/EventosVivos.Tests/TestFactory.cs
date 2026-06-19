using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Application.Interfaces;
using Moq;

namespace EventosVivos.Tests;

/// <summary>
/// Factory helpers para construir entidades de dominio en los tests,
/// evitando repetición y acoplamiento a los constructores privados.
/// </summary>
public static class TestFactory
{
    public static readonly DateTime FutureDate = new(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public static Venue CreateVenue(int id = 1, string name = "Auditorio Central",
                                     int capacity = 200, string city = "Bogotá")
    {
        return Venue.Create(id, name, capacity, city);
    }

    public static Event CreateEvent(
        Venue? venue = null,
        int maxCapacity = 100,
        DateTime? startUtc = null,
        DateTime? endUtc = null,
        decimal price = 50m,
        EventType type = EventType.Conferencia,
        DateTime? nowUtc = null)
    {
        venue ??= CreateVenue();
        startUtc ??= Now.AddDays(30);
        endUtc ??= startUtc.Value.AddHours(3);
        nowUtc ??= Now;

        return Event.Create(
            "Conferencia de Prueba 2026",
            "Descripción suficientemente larga para cumplir los 10 caracteres mínimos.",
            venue,
            maxCapacity,
            startUtc.Value,
            endUtc.Value,
            price,
            type,
            nowUtc.Value);
    }

    public static Reservation CreateReservation(
        Event? @event = null,
        int quantity = 2,
        string email = "test@example.com",
        DateTime? nowUtc = null)
    {
        @event ??= CreateEvent();
        nowUtc ??= Now;

        return Reservation.Create(@event, quantity, "Juan Pérez", email, nowUtc.Value);
    }

    public static Mock<IDateTimeProvider> MockClock(DateTime utcNow)
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.Setup(c => c.UtcNow).Returns(utcNow);
        return mock;
    }
}
