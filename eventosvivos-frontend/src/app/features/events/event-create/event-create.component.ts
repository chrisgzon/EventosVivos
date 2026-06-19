import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { VenueService } from '../../../core/services/venue.service';
import { Venue, ApiError } from '../../../core/models/models';

@Component({
  selector: 'app-event-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './event-create.component.html',
  styleUrls: ['./event-create.component.scss']
})
export class EventCreateComponent implements OnInit {
  form!: FormGroup;
  venues: Venue[] = [];
  loading = false;
  submitting = false;
  error: string | null = null;
  success: string | null = null;

  readonly eventTypes = [
    { label: 'Conferencia', value: 0 },
    { label: 'Taller',      value: 1 },
    { label: 'Concierto',   value: 2 },
  ];

  constructor(
    private fb: FormBuilder,
    private eventService: EventService,
    private venueService: VenueService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      title:            ['', [Validators.required, Validators.minLength(5), Validators.maxLength(100)]],
      description:      ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
      venueId:          ['', Validators.required],
      maxCapacity:      ['', [Validators.required, Validators.min(1)]],
      startDateTimeUtc: ['', Validators.required],
      endDateTimeUtc:   ['', Validators.required],
      ticketPrice:      ['', [Validators.required, Validators.min(0.01)]],
      type:             ['', Validators.required],
    });

    this.venueService.getAll().subscribe({ next: v => this.venues = v });
  }

  get f() { return this.form.controls; }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.error = null;

    const val = this.form.value;
    this.eventService.create({
      title:            val.title,
      description:      val.description,
      venueId:          Number(val.venueId),
      maxCapacity:      Number(val.maxCapacity),
      startDateTimeUtc: new Date(val.startDateTimeUtc).toISOString(),
      endDateTimeUtc:   new Date(val.endDateTimeUtc).toISOString(),
      ticketPrice:      Number(val.ticketPrice),
      type:             Number(val.type),
    }).subscribe({
      next: event => {
        this.submitting = false;
        this.router.navigate(['/events', event.id]);
      },
      error: (err: ApiError) => {
        this.submitting = false;
        this.error = `[${err.ruleCode ?? err.title}] ${err.detail}`;
      }
    });
  }
}
