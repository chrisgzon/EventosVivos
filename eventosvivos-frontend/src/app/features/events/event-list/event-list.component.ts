import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { VenueService } from '../../../core/services/venue.service';
import { EventResponse, Venue, ApiError } from '../../../core/models/models';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './event-list.component.html',
  styleUrls: ['./event-list.component.scss']
})
export class EventListComponent implements OnInit {
  events: EventResponse[] = [];
  venues: Venue[] = [];
  loading = false;
  error: string | null = null;

  // filters
  titleSearch = '';
  selectedType = '';
  selectedVenue = '';
  selectedStatus = '';
  startFrom = '';
  startTo = '';

  readonly eventTypes = ['Conferencia', 'Taller', 'Concierto'];
  readonly statusList = ['Activo', 'Cancelado', 'Completado'];
  readonly typeEnumMap: Record<string, number> = { Conferencia: 0, Taller: 1, Concierto: 2 };
  readonly statusEnumMap: Record<string, number> = { Activo: 0, Cancelado: 1, Completado: 2 };

  constructor(
    private eventService: EventService,
    private venueService: VenueService
  ) {}

  ngOnInit(): void {
    this.venueService.getAll().subscribe({ next: v => this.venues = v });
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading = true;
    this.error = null;
    this.eventService.getAll({
      titleSearch: this.titleSearch || undefined,
      type: this.selectedType ? this.typeEnumMap[this.selectedType] : undefined,
      venueId: this.selectedVenue ? Number(this.selectedVenue) : undefined,
      status: this.selectedStatus ? this.statusEnumMap[this.selectedStatus] : undefined,
      startFrom: this.startFrom || undefined,
      startTo: this.startTo || undefined,
    }).subscribe({
      next: events => { this.events = events; this.loading = false; },
      error: (err: ApiError) => { this.error = err.detail; this.loading = false; }
    });
  }

  clearFilters(): void {
    this.titleSearch = ''; this.selectedType = ''; this.selectedVenue = '';
    this.selectedStatus = ''; this.startFrom = ''; this.startTo = '';
    this.loadEvents();
  }

  badgeClass(val: string): string {
    return 'badge badge--' + val.toLowerCase().replace('_', '');
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleString('es-CO', {
      dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Bogota'
    });
  }

  occupancyPercent(e: EventResponse): number {
    if (!e.maxCapacity) return 0;
    return Math.round(((e.maxCapacity - e.availableTickets) / e.maxCapacity) * 100);
  }
}
