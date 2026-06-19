using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EventosVivos.Tests;

public class ReservationDomainTests
{
    private readonly DateTime _now = TestFactory.Now;

    // ------------------------------------------------------------------
    // RF-03: Crear reserva — flujo feliz
    // ------------------------------------------------------------------

    [Fact]
    public void Create_WithValidData_ShouldCreatePendingReservation()
    {
        var @event = TestFactory.CreateEvent(maxCapacity: 100, nowUtc: _now);

        var reservation = Reservation.Create(@event, 3, "Ana López", "ana@example.com", _now);

        reservation.Status.Should().Be(ReservationStatus.PendientePago);
        reservation.Quantity.Should().Be(3);
        reservation.BuyerEmail.Should().Be("ana@example.com");
        reservation.ReservationCode.Should().BeNull();
        @event.AvailableTickets.Should().Be(97); // 100 - 3
    }

    // ------------------------------------------------------------------
    // RF-03: Validaciones de entrada
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    [InlineData("")]
    public void Create_WithInvalidEmail_ShouldThrowRF03(string email)
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);

        var act = () => Reservation.Create(@event, 1, "Comprador", email, _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RF03");
    }

    [Fact]
    public void Create_WithZeroQuantity_ShouldThrowRF03()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);

        var act = () => Reservation.Create(@event, 0, "Comprador", "test@test.com", _now);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Create_ExceedingAvailableCapacity_ShouldThrowRF03()
    {
        var @event = TestFactory.CreateEvent(maxCapacity: 5, nowUtc: _now);

        var act = () => Reservation.Create(@event, 6, "Comprador", "test@test.com", _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.Message.Should().Contain("disponibles");
    }

    // ------------------------------------------------------------------
    // RN04: No reservar si falta menos de 1 hora
    // ------------------------------------------------------------------

    [Fact]
    public void Create_WhenEventStartsInLessThanOneHour_ShouldThrowRN04()
    {
        var startSoon = _now.AddMinutes(30);
        var @event = TestFactory.CreateEvent(startUtc: startSoon,
                                              endUtc: startSoon.AddHours(2),
                                              nowUtc: _now);

        var act = () => Reservation.Create(@event, 1, "Comprador", "test@test.com", _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RN04");
    }

    // ------------------------------------------------------------------
    // RF-03: Restricción < 24 horas → máximo 5 entradas
    // ------------------------------------------------------------------

    [Fact]
    public void Create_LessThan24HoursToStart_WithMoreThan5Tickets_ShouldThrow()
    {
        var startSoon = _now.AddHours(5); // dentro de < 24h pero > 1h
        var @event = TestFactory.CreateEvent(startUtc: startSoon,
                                              endUtc: startSoon.AddHours(2),
                                              nowUtc: _now);

        var act = () => Reservation.Create(@event, 6, "Comprador", "test@test.com", _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.Message.Should().Contain("24 horas");
    }

    [Fact]
    public void Create_LessThan24HoursToStart_With5OrLessTickets_ShouldSucceed()
    {
        var startSoon = _now.AddHours(5);
        var @event = TestFactory.CreateEvent(maxCapacity: 100,
                                              startUtc: startSoon,
                                              endUtc: startSoon.AddHours(2),
                                              nowUtc: _now);

        var act = () => Reservation.Create(@event, 5, "Comprador", "test@test.com", _now);

        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------
    // RN05: Precio > $100 → máximo 10 entradas
    // ------------------------------------------------------------------

    [Fact]
    public void Create_WithHighPriceEvent_AndMoreThan10Tickets_ShouldThrowRN05()
    {
        var @event = TestFactory.CreateEvent(price: 150m, maxCapacity: 200, nowUtc: _now);

        var act = () => Reservation.Create(@event, 11, "Comprador", "test@test.com", _now);

        act.Should().Throw<BusinessRuleViolationException>()
           .Which.RuleCode.Should().Be("RN05");
    }

    [Fact]
    public void Create_WithHighPriceEvent_And10Tickets_ShouldSucceed()
    {
        var @event = TestFactory.CreateEvent(price: 150m, maxCapacity: 200, nowUtc: _now);

        var act = () => Reservation.Create(@event, 10, "Comprador", "test@test.com", _now);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithPriceExactly100_ShouldNotApplyRN05()
    {
        // RN05 aplica solo si precio > $100, no exactamente $100
        var @event = TestFactory.CreateEvent(price: 100m, maxCapacity: 200, nowUtc: _now);

        var act = () => Reservation.Create(@event, 11, "Comprador", "test@test.com", _now);

        act.Should().NotThrow();
    }

    // ------------------------------------------------------------------
    // RF-04: Confirmar pago
    // ------------------------------------------------------------------

    [Fact]
    public void ConfirmPayment_FromPendingState_ShouldSetConfirmedAndGenerateCode()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 2, "Comprador", "test@test.com", _now);

        reservation.ConfirmPayment(_now);

        reservation.Status.Should().Be(ReservationStatus.Confirmada);
        reservation.ReservationCode.Should().MatchRegex(@"^EV-\d{6}$");
        reservation.ConfirmedAtUtc.Should().Be(_now);
    }

    [Fact]
    public void ConfirmPayment_AlreadyConfirmed_ShouldThrowInvalidStateTransition()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 2, "Comprador", "test@test.com", _now);
        reservation.ConfirmPayment(_now);

        var act = () => reservation.ConfirmPayment(_now);

        act.Should().Throw<InvalidStateTransitionException>()
           .Which.Message.Should().Contain("Confirmada");
    }

    [Fact]
    public void ConfirmPayment_OnCancelledReservation_ShouldThrowInvalidStateTransition()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 2, "Comprador", "test@test.com", _now);
        reservation.Cancel(_now);

        var act = () => reservation.ConfirmPayment(_now);

        act.Should().Throw<InvalidStateTransitionException>();
    }

    // ------------------------------------------------------------------
    // RF-05: Cancelar reserva
    // ------------------------------------------------------------------

    [Fact]
    public void Cancel_FromPendingState_ShouldSetCancelledAndReleaseTickets()
    {
        var @event = TestFactory.CreateEvent(maxCapacity: 100, nowUtc: _now);
        var reservation = Reservation.Create(@event, 5, "Comprador", "test@test.com", _now);

        var availableBefore = @event.AvailableTickets; // 95
        reservation.Cancel(_now);

        reservation.Status.Should().Be(ReservationStatus.Cancelada);
        reservation.CancelledAtUtc.Should().Be(_now);
        reservation.IsLostOnCancellation.Should().BeFalse();
        @event.AvailableTickets.Should().Be(100); // liberadas
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldThrowInvalidStateTransition()
    {
        var @event = TestFactory.CreateEvent(nowUtc: _now);
        var reservation = Reservation.Create(@event, 2, "Comprador", "test@test.com", _now);
        reservation.Cancel(_now);

        var act = () => reservation.Cancel(_now);

        act.Should().Throw<InvalidStateTransitionException>()
           .Which.Message.Should().Contain("Cancelada");
    }

    // ------------------------------------------------------------------
    // RN07: Cancelación con penalización (< 48h antes del evento)
    // ------------------------------------------------------------------

    [Fact]
    public void Cancel_ConfirmedReservation_LessThan48HoursBeforeEvent_ShouldMarkAsLost()
    {
        var eventStart = _now.AddHours(24); // 24h from now = < 48h threshold
        var @event = TestFactory.CreateEvent(
            maxCapacity: 100,
            startUtc: eventStart,
            endUtc: eventStart.AddHours(3),
            nowUtc: _now);

        var reservation = Reservation.Create(@event, 5, "Comprador", "test@test.com", _now);
        reservation.ConfirmPayment(_now);

        reservation.Cancel(_now);

        reservation.IsLostOnCancellation.Should().BeTrue();
        // Las entradas NO deben liberarse (CountsAsOccupied == true)
        reservation.CountsAsOccupied.Should().BeTrue();
        @event.AvailableTickets.Should().Be(95); // no se liberaron
    }

    [Fact]
    public void Cancel_ConfirmedReservation_MoreThan48HoursBeforeEvent_ShouldRelease()
    {
        var eventStart = _now.AddHours(72); // 72h from now = > 48h threshold
        var @event = TestFactory.CreateEvent(
            maxCapacity: 100,
            startUtc: eventStart,
            endUtc: eventStart.AddHours(3),
            nowUtc: _now);

        var reservation = Reservation.Create(@event, 5, "Comprador", "test@test.com", _now);
        reservation.ConfirmPayment(_now);

        reservation.Cancel(_now);

        reservation.IsLostOnCancellation.Should().BeFalse();
        @event.AvailableTickets.Should().Be(100); // liberadas
    }
}
