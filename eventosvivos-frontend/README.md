# EventosVivos — Frontend 🖥️

Aplicación SPA construida con **Angular 19** que consume la API REST de EventosVivos.

---

## Tecnologías

| Herramienta | Versión | Uso |
|-------------|---------|-----|
| Angular | 19.x | Framework principal |
| TypeScript | 5.x | Lenguaje |
| Angular Reactive Forms | — | Formularios con validación |
| Angular Router | — | Navegación con lazy loading |
| SCSS | — | Estilos con design tokens |
| Nginx | 1.27 | Servidor en producción (Docker) |

---

## Estructura del proyecto

```
eventosvivos-frontend/
├── src/
│   └── app/
│       ├── core/
│       │   ├── models/
│       │   │   └── models.ts              # Interfaces TypeScript (espejo de los DTOs del backend)
│       │   └── services/
│       │       ├── api.service.ts         # Cliente HTTP base con manejo de errores centralizado
│       │       ├── event.service.ts       # Llamadas a /api/events
│       │       ├── reservation.service.ts # Llamadas a /api/reservations
│       │       └── venue.service.ts       # Llamadas a /api/venues
│       ├── features/
│       │   ├── events/
│       │   │   ├── event-list/            # RF-02: Lista con 6 filtros
│       │   │   ├── event-detail/          # Vista detalle + panel admin de reservas
│       │   │   └── event-create/          # RF-01: Formulario reactivo de creación
│       │   ├── reservations/
│       │   │   └── reservation-create/    # RF-03: Formulario de reserva con resumen de precio
│       │   └── reports/
│       │       └── occupancy-report/      # RF-06: Reporte con gauge SVG + KPIs
│       ├── shared/
│       │   └── components/
│       │       └── navbar/                # Barra de navegación
│       ├── app.routes.ts                  # Rutas con lazy loading
│       ├── app.config.ts                  # Providers (HttpClient, Router)
│       └── app.component.ts              # Shell de la aplicación
├── src/
│   ├── environments/
│   │   ├── environment.ts                 # Desarrollo (localhost:5000)
│   │   └── environment.production.ts      # Producción (/api)
│   └── styles.scss                        # Estilos globales y design tokens
├── Dockerfile                             # Build multi-stage (Node → Nginx)
└── nginx.conf                             # Config Nginx con proxy /api/ y HTML5 routing
```

---

## Pantallas y funcionalidades

### 📋 Lista de Eventos (`/events`)
- Grid de tarjetas responsivo con todos los eventos
- **6 filtros simultáneos:** título (búsqueda parcial), tipo, venue, estado, fecha desde/hasta
- Barra de ocupación visual en cada tarjeta
- Badges de estado y tipo con colores distintivos
- Accesos directos a detalle, reservar y reporte desde cada tarjeta

### ✨ Crear Evento (`/events/new`)
- Formulario reactivo con validación en tiempo real
- Selector de venue con capacidad visible para ayudar a respetar RN01
- Campos de fecha/hora con validación de rango
- Hints de reglas de negocio (RN01, RN02, RN03) visibles en el formulario
- Contador de caracteres en la descripción
- Redirige al detalle del evento recién creado

### 🎫 Detalle de Evento (`/events/:id`)
- Información completa del evento en tarjetas KPI
- Barra de ocupación con porcentaje
- **Panel de administración de reservas:**
  - Tabla con todas las reservas del evento
  - Botón "Confirmar pago" para reservas `PendientePago`
  - Botón "Cancelar" para reservas no canceladas
  - Código de reserva `EV-XXXXXX` resaltado al confirmar
  - Indicador visual para reservas perdidas (RN07)
  - Feedback de éxito/error inline sin recargar la página

### 🎟 Reservar Entradas (`/events/:id/reserve`)
- Muestra resumen del evento (venue, fecha, precio)
- Alertas dinámicas cuando aplican restricciones (< 24h, precio > $100)
- Límite máximo de entradas calculado según RN04 y RN05
- Resumen de precio en tiempo real (`cantidad × precio`)
- **Pantalla de confirmación** al crear exitosamente (sin redirección abrupta)

### 📊 Reporte de Ocupación (`/events/:id/report`)
- **Gauge SVG animado** que muestra el porcentaje de ocupación (verde/naranja/rojo)
- 4 KPI cards: capacidad total, entradas vendidas, disponibles, ingresos totales
- Barra de desglose vendidas vs disponibles
- Botón de actualización manual

---

## Servicios

### `ApiService`
Capa base de comunicación HTTP. Centraliza:
- Construcción de query params limpiando valores `undefined`/`null`
- Transformación de errores HTTP al tipo `ApiError` con `status`, `title`, `detail` y `ruleCode`

### `EventService`
```typescript
getAll(filters?: EventFilters): Observable<EventResponse[]>
getById(id: string): Observable<EventResponse>
create(request: CreateEventRequest): Observable<EventResponse>
getOccupancyReport(id: string): Observable<OccupancyReport>
```

### `ReservationService`
```typescript
getByEvent(eventId: string): Observable<ReservationResponse[]>
getById(id: string): Observable<ReservationResponse>
create(request: CreateReservationRequest): Observable<ReservationResponse>
confirmPayment(id: string): Observable<ReservationResponse>
cancel(id: string): Observable<ReservationResponse>
```

### `VenueService`
```typescript
getAll(): Observable<Venue[]>
```

---

## Variables de entorno

| Archivo | `apiUrl` | Cuándo se usa |
|---------|----------|---------------|
| `environment.ts` | `http://localhost:7262/api` | `ng serve` (desarrollo local) |
| `environment.production.ts` | `/api` | Build de producción / Docker |

En Docker, Nginx hace proxy de `/api/` hacia el contenedor de la API, por lo que la URL relativa `/api` funciona correctamente.

---

## Ejecución local (sin Docker)

### Prerrequisitos

- Node.js 22.x
- npm 10.x
- API del backend corriendo en `http://localhost:5000`

### Pasos

```bash
# Instalar dependencias
npm install

# Iniciar servidor de desarrollo
npm start
# App disponible en: http://localhost:4200

# Build de producción
npm run build
```

---

## Decisiones de diseño

### Standalone Components + Lazy Loading
Todos los componentes usan `standalone: true`. Las rutas cargan cada feature de forma lazy para reducir el bundle inicial.

### Reactive Forms
Los formularios de creación de eventos y reservas usan `ReactiveFormsModule` para validación sincrónica en el cliente antes de llamar a la API, mejorando la experiencia de usuario.

### Design tokens en SCSS
Los colores, radios, sombras y transiciones están definidos como variables CSS en `styles.scss`, lo que permite cambiar el tema desde un único lugar.

### Manejo de errores centralizado
`ApiService` transforma todos los errores HTTP al tipo `ApiError`. Los componentes muestran el mensaje `detail` y el `ruleCode` cuando está disponible, permitiendo al usuario entender exactamente qué regla de negocio se violó.
