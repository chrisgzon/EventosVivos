using EventosVivos.Application.DTOs;
using EventosVivos.Application.Interfaces;
using EventosVivos.Application.Services;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventosVivos.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<IVenueRepository> _venueRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DateTime _now = TestFactory.Now;
    private EventService BuildService(DateTime? clock = null)
    {
        var clockMock = TestFactory.MockClock(clock ?? _now);
        return new EventService(_eventRepo.Object, _venueRepo.Object, _uow.Object, clockMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnEventResponse()
    {
        var venue = TestFactory.CreateVenue(id: 1, capacity: 200);
        _venueRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(venue);
        _eventRepo.Setup(r => r.GetOverlappingEventsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, default))
                  .ReturnsAsync([]);
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var svc = BuildService();
        var request = new CreateEventRequest(
            "Conferencia de Prueba",
            "Descripción suficientemente larga.",
            1, 100,
            _now.AddDays(10), _now.AddDays(10).AddHours(3),
            50m, EventType.Conferencia);

        var result = await svc.CreateAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Conferencia de Prueba");
        result.Status.Should().Be("Activo");
        _eventRepo.Verify(r => r.AddAsync(It.IsAny<Event>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingVenueBooking_ShouldThrowRN02()
    {
        var venue = TestFactory.CreateVenue(id: 1);
        var conflicting = TestFactory.CreateEvent(venue: venue, nowUtc: _now);
        _venueRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(venue);
        _eventRepo.Setup(r => r.GetOverlappingEventsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, default))
                  .ReturnsAsync([conflicting]);

        var svc = BuildService();
        var request = new CreateEventRequest(
            "Otro Evento Mismo Venue",
            "Descripción suficientemente larga para el test.",
            1, 50,
            _now.AddDays(30), _now.AddDays(30).AddHours(3),
            50m, EventType.Taller);

        var act = async () => await svc.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .Where(e => e.RuleCode == "RN02");
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentVenue_ShouldThrowNotFound()
    {
        _venueRepo.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Venue?)null);
        var svc = BuildService();
        var request = new CreateEventRequest(
            "Evento Sin Venue",
            "Descripción larga para el evento.",
            99, 50,
            _now.AddDays(5), _now.AddDays(5).AddHours(2),
            30m, EventType.Conferencia);

        var act = async () => await svc.CreateAsync(request);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _resRepo = new();
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DateTime _now = TestFactory.Now;

    private ReservationService BuildService(DateTime? clock = null)
    {
        var clockMock = TestFactory.MockClock(clock ?? _now);
        return new ReservationService(_resRepo.Object, _eventRepo.Object, _uow.Object, clockMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnPendingReservation()
    {
        var venue = TestFactory.CreateVenue();
        var @event = TestFactory.CreateEvent(venue: venue, maxCapacity: 100, nowUtc: _now);
        _eventRepo.Setup(r => r.GetByIdWithReservationsAsync(@event.Id, default)).ReturnsAsync(@event);
        _resRepo.Setup(r => r.AddAsync(It.IsAny<Reservation>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var svc = BuildService();
        var result = await svc.CreateAsync(new CreateReservationRequest(
            @event.Id, 3, "Carlos Ruiz", "carlos@mail.com"));

        result.Status.Should().Be("PendientePago");
        result.Quantity.Should().Be(3);
        result.ReservationCode.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmPaymentAsync_OnPendingReservation_ShouldReturnConfirmedWithCode()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 2, "María Gómez", "maria@mail.com", _now);

        _resRepo.Setup(r => r.GetByIdWithEventAsync(reservation.Id, default)).ReturnsAsync(reservation);
        _resRepo.Setup(r => r.UpdateAsync(reservation, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var svc = BuildService();
        var result = await svc.ConfirmPaymentAsync(reservation.Id);

        result.Status.Should().Be("Confirmada");
        result.ReservationCode.Should().MatchRegex(@"^EV-\d{6}$");
    }

    [Fact]
    public async Task CancelAsync_ConfirmedReservation_MoreThan48Hours_ShouldRelease()
    {
        var eventStart = _now.AddHours(72);
        var @event = TestFactory.CreateEvent(
            maxCapacity: 100,
            startUtc: eventStart,
            endUtc: eventStart.AddHours(3),
            nowUtc: _now);

        var reservation = Reservation.Create(@event, 4, "Luis Mora", "luis@mail.com", _now);
        reservation.ConfirmPayment(_now);

        _resRepo.Setup(r => r.GetByIdWithEventAsync(reservation.Id, default)).ReturnsAsync(reservation);
        _resRepo.Setup(r => r.UpdateAsync(reservation, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var svc = BuildService();
        var result = await svc.CancelAsync(reservation.Id);

        result.Status.Should().Be("Cancelada");
        result.IsLostOnCancellation.Should().BeFalse();
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ShouldThrowConflict()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 1, "Pedro Gil", "pedro@mail.com", _now);
        reservation.Cancel(_now);

        _resRepo.Setup(r => r.GetByIdWithEventAsync(reservation.Id, default)).ReturnsAsync(reservation);

        var svc = BuildService();

        var act = async () => await svc.CancelAsync(reservation.Id);

        await act.Should().ThrowAsync<InvalidStateTransitionException>();
    }
}
