import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { EventResponse, ReservationResponse, ApiError } from '../../../core/models/models';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './event-detail.component.html',
  styleUrls: ['./event-detail.component.scss']
})
export class EventDetailComponent implements OnInit {
  event: EventResponse | null = null;
  reservations: ReservationResponse[] = [];
  loading = true;
  error: string | null = null;
  actionError: string | null = null;
  actionSuccess: string | null = null;
  processingId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private eventService: EventService,
    private reservationService: ReservationService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.eventService.getById(id).subscribe({
      next: e => {
        this.event = e;
        this.loadReservations(id);
      },
      error: (err: ApiError) => { this.error = err.detail; this.loading = false; }
    });
  }

  loadReservations(id: string): void {
    this.reservationService.getByEvent(id).subscribe({
      next: r => { this.reservations = r; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  confirmPayment(r: ReservationResponse): void {
    this.processingId = r.id;
    this.actionError = null;
    this.reservationService.confirmPayment(r.id).subscribe({
      next: updated => {
        this.updateReservation(updated);
        this.processingId = null;
        this.actionSuccess = `Reserva ${updated.reservationCode} confirmada exitosamente.`;
        setTimeout(() => this.actionSuccess = null, 4000);
        this.refreshEvent();
      },
      error: (err: ApiError) => {
        this.actionError = err.detail;
        this.processingId = null;
      }
    });
  }

  cancelReservation(r: ReservationResponse): void {
    if (!confirm('¿Confirma que desea cancelar esta reserva?')) return;
    this.processingId = r.id;
    this.actionError = null;
    this.reservationService.cancel(r.id).subscribe({
      next: updated => {
        this.updateReservation(updated);
        this.processingId = null;
        const lostNote = updated.isLostOnCancellation ? ' (entradas marcadas como perdidas - RN07)' : '';
        this.actionSuccess = `Reserva cancelada.${lostNote}`;
        setTimeout(() => this.actionSuccess = null, 5000);
        this.refreshEvent();
      },
      error: (err: ApiError) => {
        this.actionError = err.detail;
        this.processingId = null;
      }
    });
  }

  private updateReservation(updated: ReservationResponse): void {
    const idx = this.reservations.findIndex(r => r.id === updated.id);
    if (idx >= 0) this.reservations[idx] = updated;
  }

  private refreshEvent(): void {
    if (!this.event) return;
    this.eventService.getById(this.event.id).subscribe({
      next: e => this.event = e
    });
  }

  badgeClass(val: string): string {
    return 'badge badge--' + val.toLowerCase().replace('_', '').replace('pago','pago');
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleString('es-CO', {
      dateStyle: 'long', timeStyle: 'short', timeZone: 'America/Bogota'
    });
  }

  occupancyPercent(): number {
    if (!this.event?.maxCapacity) return 0;
    return Math.round(((this.event.maxCapacity - this.event.availableTickets) / this.event.maxCapacity) * 100);
  }
}
