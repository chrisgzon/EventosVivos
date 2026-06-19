import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  EventResponse, CreateEventRequest, EventFilters, OccupancyReport
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class EventService {
  constructor(private api: ApiService) {}

  getAll(filters?: EventFilters): Observable<EventResponse[]> {
    return this.api.get<EventResponse[]>('events', filters as Record<string, string | number | boolean | undefined>);
  }

  getById(id: string): Observable<EventResponse> {
    return this.api.get<EventResponse>(`events/${id}`);
  }

  create(request: CreateEventRequest): Observable<EventResponse> {
    return this.api.post<EventResponse>('events', request);
  }

  getOccupancyReport(id: string): Observable<OccupancyReport> {
    return this.api.get<OccupancyReport>(`events/${id}/occupancy`);
  }
}
