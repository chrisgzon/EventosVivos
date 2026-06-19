import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'events', pathMatch: 'full' },
  {
    path: 'events',
    loadComponent: () => import('./features/events/event-list/event-list.component')
      .then(m => m.EventListComponent)
  },
  {
    path: 'events/new',
    loadComponent: () => import('./features/events/event-create/event-create.component')
      .then(m => m.EventCreateComponent)
  },
  {
    path: 'events/:id',
    loadComponent: () => import('./features/events/event-detail/event-detail.component')
      .then(m => m.EventDetailComponent)
  },
  {
    path: 'events/:id/reserve',
    loadComponent: () => import('./features/reservations/reservation-create/reservation-create.component')
      .then(m => m.ReservationCreateComponent)
  },
  {
    path: 'events/:id/report',
    loadComponent: () => import('./features/reports/occupancy-report/occupancy-report.component')
      .then(m => m.OccupancyReportComponent)
  },
];
