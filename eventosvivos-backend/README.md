# EventosVivos — Backend 📦

API REST construida con **.NET 10** siguiendo los principios de **Clean Architecture**.

---

## Arquitectura

El proyecto aplica **Clean Architecture** con cuatro capas que respetan la regla de dependencia (las capas internas no conocen las externas):

```
eventosvivos-backend/
├── src/
│   ├── EventosVivos.Domain/          # Núcleo — entidades, enums, excepciones
│   ├── EventosVivos.Application/     # Casos de uso — servicios, DTOs, interfaces
│   ├── EventosVivos.Infrastructure/  # EF Core, repositorios, acceso a datos
│   └── EventosVivos.Api/             # Controllers, middleware, configuración HTTP
└── tests/
    └── EventosVivos.Tests/           # Tests unitarios — xUnit + Moq + FluentAssertions
```

### ¿Por qué Clean Architecture?

- **Domain** no depende de ningún framework ni librería externa — las reglas de negocio son puro C#.
- **Application** depende solo de Domain. Puede testearse sin EF Core ni HTTP.
- **Infrastructure** puede reemplazarse (cambiar SQL Server por PostgreSQL) sin tocar Domain ni Application.
- Los tests unitarios corren en milisegundos porque no necesitan base de datos.

---

## Capa Domain

Contiene el modelo de dominio **rico** — las entidades encapsulan su propia lógica de negocio.

### Entidades

#### `Venue`
Lugar físico donde se realizan los eventos. Los tres venues de referencia se cargan vía seed.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `int` | Identificador fijo (1, 2, 3) |
| `Name` | `string` | Nombre del venue |
| `Capacity` | `int` | Capacidad máxima de personas |
| `City` | `string` | Ciudad |

#### `Event`
Agregado raíz. Encapsula las reglas RF-01, RN01, RN03 y RN06.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único |
| `Title` | `string` | 5–100 caracteres |
| `Description` | `string` | 10–500 caracteres |
| `VenueId` | `int` | FK al venue |
| `MaxCapacity` | `int` | ≤ capacidad del venue (RN01) |
| `StartDateTimeUtc` | `DateTime` | Debe ser futura |
| `EndDateTimeUtc` | `DateTime` | Debe ser posterior al inicio |
| `TicketPrice` | `decimal` | Positivo |
| `Type` | `EventType` | `Conferencia`, `Taller`, `Concierto` |
| `Status` | `EventStatus` | `Activo`, `Cancelado`, `Completado` |

**Comportamiento importante:**
- `Event.Create()` valida todas las reglas del RF-01, RN01 y RN03 en el momento de construcción.
- `Event.RefreshStatus(nowUtc)` implementa RN06: marca el evento como `Completado` si la fecha actual superó `EndDateTimeUtc`. Se llama en cada lectura.
- `AvailableTickets` se calcula dinámicamente: `MaxCapacity − (Confirmadas + PendientePago)`.

#### `Reservation`
Encapsula el ciclo de vida completo de una reserva (RF-03, RF-04, RF-05, RN04, RN05, RN07).

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único |
| `EventId` | `Guid` | FK al evento |
| `Quantity` | `int` | ≥ 1 |
| `BuyerName` | `string` | Nombre del comprador |
| `BuyerEmail` | `string` | Email válido |
| `Status` | `ReservationStatus` | `PendientePago` → `Confirmada` → `Cancelada` |
| `ReservationCode` | `string?` | Formato `EV-XXXXXX`, generado al confirmar |
| `IsLostOnCancellation` | `bool` | `true` si se aplica RN07 |

### Excepciones de dominio

| Excepción | HTTP Status | Uso |
|-----------|------------|-----|
| `EntityNotFoundException` | 404 | Entidad no encontrada por ID |
| `BusinessRuleViolationException` | 422 | Violación de regla de negocio (incluye `RuleCode`) |
| `InvalidStateTransitionException` | 409 | Transición de estado inválida |

---

## Capa Application

Orquesta los casos de uso sin conocer EF Core ni HTTP.

### `EventService`

| Método | RF/RN | Descripción |
|--------|-------|-------------|
| `CreateAsync` | RF-01, RN02 | Crea un evento. Verifica superposición de venue en el repo. |
| `GetAllAsync` | RF-02 | Lista eventos con filtros opcionales. Aplica RN06. |
| `GetByIdAsync` | — | Obtiene un evento por ID. Aplica RN06. |
| `GetOccupancyReportAsync` | RF-06 | Genera el reporte de ocupación. |

### `ReservationService`

| Método | RF/RN | Descripción |
|--------|-------|-------------|
| `CreateAsync` | RF-03 | Crea una reserva con estado `PendientePago`. |
| `ConfirmPaymentAsync` | RF-04 | Cambia a `Confirmada`, genera código `EV-XXXXXX`. |
| `CancelAsync` | RF-05, RN07 | Cancela la reserva. Aplica penalización RN07 si corresponde. |
| `GetByEventAsync` | — | Lista reservas de un evento. |

---

## Reglas de negocio

| ID | Regla | Dónde se implementa |
|----|-------|-------------------|
| RN01 | Capacidad del evento ≤ capacidad del venue | `Event.Create()` |
| RN02 | No puede haber dos eventos activos con horario superpuesto en el mismo venue | `EventService.CreateAsync()` + `IEventRepository.GetOverlappingEventsAsync()` |
| RN03 | Eventos en fin de semana no pueden iniciar después de las 22:00 (hora Colombia UTC-5) | `Event.Create()` |
| RN04 | No se permiten reservas para eventos que inician en menos de 1 hora | `Reservation.Create()` |
| RN05 | Eventos con precio > $100 permiten máximo 10 entradas por transacción | `Reservation.Create()` |
| RN06 | Un evento se marca `Completado` automáticamente cuando la fecha actual supera su hora de fin | `Event.RefreshStatus()` |
| RN07 | Cancelar una reserva `Confirmada` con menos de 48h del evento la marca como "perdida" (no libera entradas) | `Reservation.Cancel()` |

---

## Endpoints REST

### Venues

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/venues` | Lista los 3 venues de referencia |

### Eventos

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/events` | Listar con filtros (RF-02) |
| `GET` | `/api/events/{id}` | Obtener evento por ID |
| `POST` | `/api/events` | Crear evento (RF-01) |
| `GET` | `/api/events/{id}/occupancy` | Reporte de ocupación (RF-06) |

**Query params para `GET /api/events`:**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `titleSearch` | `string` | Búsqueda parcial, case-insensitive |
| `type` | `int` | `0` Conferencia, `1` Taller, `2` Concierto |
| `venueId` | `int` | ID del venue |
| `status` | `int` | `0` Activo, `1` Cancelado, `2` Completado |
| `startFrom` | `datetime` | Filtro de fecha inicio (desde) |
| `startTo` | `datetime` | Filtro de fecha inicio (hasta) |

### Reservas

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/events/{eventId}/reservations` | Reservas de un evento |
| `GET` | `/api/reservations/{id}` | Reserva por ID |
| `POST` | `/api/reservations` | Crear reserva (RF-03) |
| `PATCH` | `/api/reservations/{id}/confirm` | Confirmar pago (RF-04) |
| `PATCH` | `/api/reservations/{id}/cancel` | Cancelar reserva (RF-05) |

---

## Manejo de errores

El middleware `ExceptionHandlingMiddleware` centraliza el manejo de errores y devuelve siempre el mismo formato JSON:

```json
{
  "status": 422,
  "title": "Regla de negocio violada",
  "detail": "La capacidad del evento (300) excede la capacidad del venue 'Sala Norte' (50).",
  "extra": { "ruleCode": "RN01" },
  "traceId": "0HN8..."
}
```

---

## Tests

Los tests están organizados en tres clases que cubren todas las reglas de negocio:

| Clase | Cobertura |
|-------|-----------|
| `EventDomainTests` | RF-01 (validaciones de título, descripción, fechas, precio), RN01, RN03, RN06 |
| `ReservationDomainTests` | RF-03 (email, cantidad, disponibilidad), RF-04, RF-05, RN04, RN05, RN07 |
| `ApplicationServiceTests` | `EventService` (RN02, venue no encontrado), `ReservationService` (confirmar, cancelar, transiciones inválidas) |

### Ejecutar tests

```bash
cd tests/EventosVivos.Tests
dotnet test --logger "console;verbosity=normal"
```

---

## Ejecución local (sin Docker)

### Prerrequisitos

- .NET SDK 10.0
- SQL Server 2019+ o instancia en la nube

### Pasos

```bash
# 1. Ajustar la cadena de conexión
# Editar: src/EventosVivos.Api/appsettings.json
# "DefaultConnection": "Server=localhost,1434;Database=EventosVivos;User Id=sa;Password=EventosVivos_2026!;TrustServerCertificate=True;"

# 2. Crear la base de datos ejecutando el script SQL
# Conectar con SSMS o sqlcmd y ejecutar: init.sql (en la raíz del repositorio)

# 3. Ejecutar la API
cd src/EventosVivos.Api
dotnet run

# API disponible en: http://localhost:7262
# Swagger UI en:     http://localhost:7262/swagger
```

> Las migraciones de EF Core se aplican automáticamente al iniciar si la base de datos ya existe.

---

## Generar migraciones EF Core

```bash
cd src/EventosVivos.Infrastructure
dotnet ef migrations add NombreDeLaMigracion \
  --startup-project ../EventosVivos.Api \
  --output-dir Persistence/Migrations
```

---

## Tecnologías

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.0 | ORM + SQL Server provider |
| `Swashbuckle.AspNetCore` | 7.x | Swagger / OpenAPI |
| `xunit` | 2.9.x | Framework de tests |
| `Moq` | 4.20.x | Mocking de dependencias |
| `FluentAssertions` | 6.x | Assertions legibles |
