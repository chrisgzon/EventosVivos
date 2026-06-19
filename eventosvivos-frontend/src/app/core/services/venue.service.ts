import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Venue } from '../models/models';

@Injectable({ providedIn: 'root' })
export class VenueService {
  constructor(private api: ApiService) {}

  getAll(): Observable<Venue[]> {
    return this.api.get<Venue[]>('venues');
  }
}
