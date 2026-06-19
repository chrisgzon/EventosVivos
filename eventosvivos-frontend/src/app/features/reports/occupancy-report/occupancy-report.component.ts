import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { OccupancyReport, ApiError } from '../../../core/models/models';

@Component({
  selector: 'app-occupancy-report',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './occupancy-report.component.html',
  styleUrls: ['./occupancy-report.component.scss']
})
export class OccupancyReportComponent implements OnInit {
  report: OccupancyReport | null = null;
  loading = true;
  error: string | null = null;
  eventId = '';
  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private eventService: EventService
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.eventService.getOccupancyReport(this.eventId).subscribe({
      next: r => { this.report = r; this.loading = false; },
      error: (err: ApiError) => { this.error = err.detail; this.loading = false; }
    });
  }

  get statusColor(): string {
    switch (this.report?.status) {
      case 'Activo':     return '#4caf50';
      case 'Cancelado':  return '#f44336';
      case 'Completado': return '#2196f3';
      default:           return '#888';
    }
  }

  get gaugeAngle(): number {
    return ((this.report?.occupancyPercentage ?? 0) / 100) * 180;
  }

  get gaugeColor(): string {
    const pct = this.report?.occupancyPercentage ?? 0;
    if (pct >= 90) return '#f44336';
    if (pct >= 70) return '#ff9800';
    return '#4caf50';
  }
}
