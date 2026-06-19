import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { ReservationService } from '../../../core/services/reservation.service';
import { EventResponse, ReservationResponse, ApiError } from '../../../core/models/models';

@Component({
  selector: 'app-reservation-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reservation-create.component.html',
  styleUrls: ['./reservation-create.component.scss']
})
export class ReservationCreateComponent implements OnInit {
  form!: FormGroup;
  event: EventResponse | null = null;
  createdReservation: ReservationResponse | null = null;
  loading = true;
  submitting = false;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private reservationService: ReservationService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.form = this.fb.group({
      quantity:   [1, [Validators.required, Validators.min(1), Validators.max(100)]],
      buyerName:  ['', [Validators.required, Validators.minLength(2)]],
      buyerEmail: ['', [Validators.required, Validators.email]],
    });

    this.eventService.getById(id).subscribe({
      next: e => { this.event = e; this.loading = false; },
      error: (err: ApiError) => { this.error = err.detail; this.loading = false; }
    });
  }

  get f() { return this.form.controls; }

  get totalAmount(): number {
    return (this.event?.ticketPrice ?? 0) * (Number(this.f['quantity'].value) || 0);
  }

  get hoursToStart(): number {
    if (!this.event) return 999;
    return (new Date(this.event.startDateTimeUtc).getTime() - Date.now()) / 3_600_000;
  }

  get maxPerTransaction(): number {
    if (!this.event) return 100;
    const lessThan24h = this.hoursToStart < 24;
    const highPrice = this.event.ticketPrice > 100;
    if (lessThan24h && highPrice) return 5;
    if (lessThan24h) return 5;
    if (highPrice) return 10;
    return 100;
  }

  submit(): void {
    if (this.form.invalid || !this.event) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.error = null;

    this.reservationService.create({
      eventId:    this.event.id,
      quantity:   Number(this.f['quantity'].value),
      buyerName:  this.f['buyerName'].value,
      buyerEmail: this.f['buyerEmail'].value,
    }).subscribe({
      next: reservation => {
        this.submitting = false;
        this.createdReservation = reservation;
      },
      error: (err: ApiError) => {
        this.submitting = false;
        this.error = `[${err.ruleCode ?? err.title}] ${err.detail}`;
      }
    });
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleString('es-CO', {
      dateStyle: 'long', timeStyle: 'short', timeZone: 'America/Bogota'
    });
  }
}
