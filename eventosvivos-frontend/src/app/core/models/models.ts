// ─────────────────────────────────────────────────────────────────────────────
// Venue
// ─────────────────────────────────────────────────────────────────────────────
export interface Venue {
  id: number;
  name: string;
  capacity: number;
  city: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Events
// ─────────────────────────────────────────────────────────────────────────────
export type EventType = 'Conferencia' | 'Taller' | 'Concierto';
export type EventStatus = 'Activo' | 'Cancelado' | 'Completado';

export interface EventResponse {
  id: string;
  title: string;
  description: string;
  venue: Venue;
  maxCapacity: number;
  availableTickets: number;
  startDateTimeUtc: string;
  endDateTimeUtc: string;
  ticketPrice: number;
  type: EventType;
  status: EventStatus;
  createdAtUtc: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  venueId: number;
  maxCapacity: number;
  startDateTimeUtc: string;
  endDateTimeUtc: string;
  ticketPrice: number;
  type: number; // enum value: Conferencia=0, Taller=1, Concierto=2
}

export interface EventFilters {
  type?: number;
  startFrom?: string;
  startTo?: string;
  venueId?: number;
  status?: number;
  titleSearch?: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Reservations
// ─────────────────────────────────────────────────────────────────────────────
export type ReservationStatus = 'PendientePago' | 'Confirmada' | 'Cancelada';

export interface ReservationResponse {
  id: string;
  eventId: string;
  eventTitle: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
  status: ReservationStatus;
  reservationCode: string | null;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  cancelledAtUtc: string | null;
  isLostOnCancellation: boolean;
  totalAmount: number;
}

export interface CreateReservationRequest {
  eventId: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Occupancy Report
// ─────────────────────────────────────────────────────────────────────────────
export interface OccupancyReport {
  eventId: string;
  eventTitle: string;
  maxCapacity: number;
  confirmedTickets: number;
  availableTickets: number;
  occupancyPercentage: number;
  totalRevenue: number;
  status: EventStatus;
}

// ─────────────────────────────────────────────────────────────────────────────
// API Error shape
// ─────────────────────────────────────────────────────────────────────────────
export interface ApiError {
  status: number;
  title: string;
  detail: string;
  ruleCode?: string;
  traceId?: string;
}
