import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  ReservationResponse, CreateReservationRequest
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  constructor(private api: ApiService) {}

  getByEvent(eventId: string): Observable<ReservationResponse[]> {
    return this.api.get<ReservationResponse[]>(`events/${eventId}/reservations`);
  }

  getById(id: string): Observable<ReservationResponse> {
    return this.api.get<ReservationResponse>(`reservations/${id}`);
  }

  create(request: CreateReservationRequest): Observable<ReservationResponse> {
    return this.api.post<ReservationResponse>('reservations', request);
  }

  confirmPayment(id: string): Observable<ReservationResponse> {
    return this.api.post<ReservationResponse>(`reservations/${id}/confirm`, {});
  }

  cancel(id: string): Observable<ReservationResponse> {
    return this.api.post<ReservationResponse>(`reservations/${id}/cancel`, {});
  }
}
