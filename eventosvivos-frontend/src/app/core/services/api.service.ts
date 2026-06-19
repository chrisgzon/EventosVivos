import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiError } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  get<T>(path: string, params?: Record<string, string | number | boolean | undefined>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, val]) => {
        if (val !== undefined && val !== null && val !== '') {
          httpParams = httpParams.set(key, String(val));
        }
      });
    }
    return this.http
      .get<T>(`${this.baseUrl}/${path}`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<T>(`${this.baseUrl}/${path}`, body)
      .pipe(catchError(this.handleError));
  }

  patch<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .patch<T>(`${this.baseUrl}/${path}`, body)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    const apiError: ApiError = {
      status: error.status,
      title: error.error?.title ?? 'Error de conexión',
      detail: error.error?.detail ?? error.message,
      ruleCode: error.error?.extra?.ruleCode,
      traceId: error.error?.traceId,
    };
    return throwError(() => apiError);
  }
}
