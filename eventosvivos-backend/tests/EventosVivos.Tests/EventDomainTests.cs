using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EventosVivos.Tests;

public class EventDomainTests
{
    private readonly DateTime _now = TestFactory.Now;
    private readonly Venue _venue = TestFactory.CreateVenue();

    // ------------------------------------------------------------------
    // RF-01: Crear Evento — validaciones básicas
    // ------------------------------------------------------------------

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);

        @event.Should().NotBeNull();
        @event.Id.Should().NotBeEmpty();
        @event.Status.Should().Be(EventStatus.Activo);
        @event.AvailableTickets.Should().Be(@event.MaxCapacity);
    }

    [Theory]
    [InlineData("")]           // empty
    [InlineData("Abc")]        // too short (< 5)
    [InlineData("AB")]         // too short
    public void Create_WithShortTitle_ShouldThrowBusinessRuleViolation(string title)
    {
        var act = () => Event.Create(title, "Descripción suficiente para el test.",
            _venue, 50, _now.AddDays(1), _now.AddDays(1).AddHours(2), 50m, EventType.Conferencia, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RF01");
    }

    [Fact]
    public void Create_WithPastStartDate_ShouldThrowBusinessRuleViolation()
    {
        var act = () => Event.Create(
            "Título válido evento",
            "Descripción suficientemente larga.",
            _venue, 50,
            _now.AddHours(-1),    // pasado
            _now.AddHours(2),
            50m, EventType.Conferencia, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.Message.Should().Contain("futuras");
    }

    [Fact]
    public void Create_WithEndBeforeStart_ShouldThrowBusinessRuleViolation()
    {
        var act = () => Event.Create(
            "Título válido evento",
            "Descripción suficientemente larga.",
            _venue, 50,
            _now.AddDays(2),
            _now.AddDays(1),     // fin antes que inicio
            50m, EventType.Conferencia, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.Message.Should().Contain("posterior");
    }

    // ------------------------------------------------------------------
    // RN01: Capacidad del venue
    // ------------------------------------------------------------------

    [Fact]
    public void Create_WithCapacityExceedingVenue_ShouldThrowRN01()
    {
        var smallVenue = TestFactory.CreateVenue(capacity: 30);

        var act = () => Event.Create(
            "Título válido evento",
            "Descripción suficientemente larga para el test.",
            smallVenue,
            50,                // 50 > 30 venue capacity
            _now.AddDays(1),
            _now.AddDays(1).AddHours(3),
            50m, EventType.Conferencia, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RN01");
    }

    // ------------------------------------------------------------------
    // RN03: Horario nocturno en weekends (≥ 22:00 Colombia = UTC-5, so 03:00 UTC Saturday)
    // ------------------------------------------------------------------

    [Fact]
    public void Create_OnWeekendAfter22hColombia_ShouldThrowRN03()
    {
        // Saturday in Colombia at 22:30 = Sunday UTC at 03:30
        // Colombia is UTC-5, so 22:30 local = 03:30 UTC next day
        // We need a datetime that when converted to Colombia is Saturday >= 22:00
        // Saturday 2026-12-05 22:30 Colombia = Saturday 2026-12-06 03:30 UTC
        var saturdayNightUtc = new DateTime(2026, 12, 6, 3, 30, 0, DateTimeKind.Utc);

        var act = () => Event.Create(
            "Concierto nocturno largo",
            "Descripción suficientemente larga para el test.",
            _venue, 50,
            saturdayNightUtc,
            saturdayNightUtc.AddHours(3),
            50m, EventType.Concierto, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RN03");
    }

    [Fact]
    public void Create_OnWeekendBefore22hColombia_ShouldSucceed()
    {
        // Saturday 2026-12-05 at 20:00 Colombia = Saturday 2026-12-06 01:00 UTC
        var saturdayEveningUtc = new DateTime(2026, 12, 6, 1, 0, 0, DateTimeKind.Utc);

        var act = () => Event.Create(
            "Concierto de tarde sábado",
            "Descripción suficientemente larga para el test.",
            _venue, 50,
            saturdayEveningUtc,
            saturdayEveningUtc.AddHours(2),
            50m, EventType.Concierto, _now);

        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------
    // RN06: Estado completado automáticamente
    // ------------------------------------------------------------------

    [Fact]
    public void RefreshStatus_WhenCurrentTimeAfterEndDate_ShouldMarkAsCompleted()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        @event.Status.Should().Be(EventStatus.Activo);

        // Simulamos que ya pasó la fecha de fin
        @event.RefreshStatus(@event.EndDateTimeUtc.AddMinutes(1));

        @event.Status.Should().Be(EventStatus.Completado);
    }

    [Fact]
    public void RefreshStatus_WhenCurrentTimeBeforeEndDate_ShouldRemainActive()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);

        @event.RefreshStatus(@event.StartDateTimeUtc.AddMinutes(30));

        @event.Status.Should().Be(EventStatus.Activo);
    }

    [Fact]
    public void RefreshStatus_WhenCancelled_ShouldNotChange()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        @event.Cancel();

        @event.RefreshStatus(@event.EndDateTimeUtc.AddDays(1)); // long after end

        @event.Status.Should().Be(EventStatus.Cancelado);
    }
}
