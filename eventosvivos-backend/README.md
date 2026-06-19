# EventosVivos 🎭

Sistema de reservas para EventosVivos — prueba técnica Fullstack .NET 10 + Angular 19.

---

## Arquitectura

### Backend — Clean Architecture

```
EventosVivos/
├── src/
│   ├── EventosVivos.Domain/          # Entidades, Enums, Excepciones de dominio (sin dependencias externas)
│   ├── EventosVivos.Application/     # DTOs, Interfaces, Servicios de aplicación
│   ├── EventosVivos.Infrastructure/  # EF Core + Npgsql, Repositorios, UoW
│   └── EventosVivos.Api/             # ASP.NET Core Web API, Controllers, Middleware
├── tests/
│   └── EventosVivos.Tests/           # xUnit + Moq + FluentAssertions
└── eventosvivos-frontend/            # Angular 19 SPA
```

**Justificación:** Clean Architecture garantiza que las reglas de negocio (Domain) sean completamente independientes de frameworks, bases de datos o la UI. Esto permite:
- Testear la lógica de dominio sin mocks de infraestructura.
- Cambiar la BD (p.ej. de PostgreSQL a SQL Server) tocando solo Infrastructure.
- Los servicios de Application orquestan casos de uso sin conocer EF Core ni HTTP.

### Frontend — Angular 19 (Standalone + Lazy Loading)

```
eventosvivos-frontend/src/app/
├── core/
│   ├── models/         # Interfaces TypeScript (espejo de los DTOs del backend)
│   └── services/       # ApiService, EventService, ReservationService, VenueService
├── features/
│   ├── events/         # EventList, EventDetail, EventCreate
│   ├── reservations/   # ReservationCreate
│   └── reports/        # OccupancyReport
└── shared/
    └── components/     # Navbar
```

---

## Requisitos previos

| Herramienta | Versión mínima |
|-------------|---------------|
| .NET SDK    | 10.0          |
| Node.js     | 22.x          |
| npm         | 10.x          |
| PostgreSQL  | 15+           |
| Docker      | 20+ (opcional)|

---

## Ejecución local

### Opción A — Docker Compose (recomendado)

```bash
# Levanta PostgreSQL + API
docker-compose up -d

# Frontend
cd eventosvivos-frontend
npm install
npm start          # http://localhost:4200
```

### Opción B — Manual

#### 1. Base de datos PostgreSQL

```bash
# Crear base de datos
psql -U postgres -c "CREATE DATABASE eventosvivos;"
```

#### 2. Backend

```bash
# Ajustar cadena de conexión (si es necesario)
# Editar: src/EventosVivos.Api/appsettings.json
# "DefaultConnection": "Host=localhost;Port=5432;Database=eventosvivos;Username=postgres;Password=postgres"

# Restaurar, migrar y ejecutar
cd src/EventosVivos.Api
dotnet run
# API disponible en: http://localhost:5000
# Swagger UI en:     http://localhost:5000/swagger
```

> Las migraciones se aplican automáticamente al iniciar la API.
> Los venues de referencia (Auditorio Central, Sala Norte, Arena Sur) se cargan vía EF Core Data Seeding.

#### 3. Frontend

```bash
cd eventosvivos-frontend
npm install
npm start
# App disponible en: http://localhost:4200
```

---

## Generar migraciones (primera vez)

```bash
cd src/EventosVivos.Infrastructure
dotnet ef migrations add InitialCreate \
  --startup-project ../EventosVivos.Api \
  --output-dir Persistence/Migrations
```

---

## Tests

```bash
cd tests/EventosVivos.Tests
dotnet test --logger "console;verbosity=detailed"
```

**Cobertura de tests:**

| Suite | Qué valida |
|-------|-----------|
| `EventDomainTests` | RF-01 (validaciones), RN01 (capacidad venue), RN03 (horario nocturno), RN06 (completado automático) |
| `ReservationDomainTests` | RF-03 (crear reserva), RF-04 (confirmar pago), RF-05 (cancelar), RN04 (< 1h), RN05 (precio > $100), RN07 (penalización 48h) |
| `ApplicationServiceTests` | EventService (RN02 superposición venue, venue no encontrado), ReservationService (confirmar, cancelar, transiciones inválidas) |

---

## Endpoints REST

### Venues
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/venues` | Lista los 3 venues de referencia |

### Eventos
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/events` | Listar con filtros (RF-02) |
| GET | `/api/events/{id}` | Obtener evento por ID |
| POST | `/api/events` | Crear evento (RF-01) |
| GET | `/api/events/{id}/occupancy` | Reporte de ocupación (RF-06) |

**Filtros de `/api/events`:** `type`, `startFrom`, `startTo`, `venueId`, `status`, `titleSearch`

### Reservas
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/events/{eventId}/reservations` | Reservas por evento |
| GET | `/api/reservations/{id}` | Reserva por ID |
| POST | `/api/reservations` | Crear reserva (RF-03) |
| POST | `/api/reservations/{id}/confirm` | Confirmar pago (RF-04) |
| POST | `/api/reservations/{id}/cancel` | Cancelar reserva (RF-05) |

---

## Reglas de negocio implementadas

| ID | Regla | Dónde se implementa |
|----|-------|-------------------|
| RN01 | Capacidad del venue | `Event.Create()` (Domain) |
| RN02 | Superposición de venues | `EventService.CreateAsync()` (Application) + query en Repository |
| RN03 | Horario nocturno en weekends | `Event.Create()` (Domain) — evalúa hora Colombia (UTC-5) |
| RN04 | Restricción reserva < 1 hora | `Reservation.Create()` (Domain) |
| RN05 | Máx. 10 entradas si precio > $100 | `Reservation.Create()` (Domain) |
| RN06 | Auto-completado al pasar fecha fin | `Event.RefreshStatus()` — llamado en cada lectura |
| RN07 | Penalización cancelación < 48h | `Reservation.Cancel()` (Domain) — marca `IsLostOnCancellation` |

---

## Manejo de errores HTTP

| Excepción de dominio | HTTP Status |
|---------------------|------------|
| `EntityNotFoundException` | 404 Not Found |
| `BusinessRuleViolationException` | 422 Unprocessable Entity |
| `InvalidStateTransitionException` | 409 Conflict |
| Error no controlado | 500 Internal Server Error |

---

## Tecnologías

**Backend:** .NET 10, ASP.NET Core Web API, Entity Framework Core 10, Npgsql, Swashbuckle (Swagger)

**Tests:** xUnit, Moq, FluentAssertions

**Frontend:** Angular 19 (Standalone Components, Lazy Loading), SCSS, Reactive Forms

**Infraestructura:** PostgreSQL 16, Docker, Docker Compose
