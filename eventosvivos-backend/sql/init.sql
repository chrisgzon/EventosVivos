-- =============================================================================
-- EventosVivos — SQL Server Schema
-- Compatible with: SQL Server 2019+ / Azure SQL
-- Run order: this single file creates everything from scratch.
-- =============================================================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EventosVivos')
BEGIN
    CREATE DATABASE EventosVivos
    COLLATE Latin1_General_CI_AS;  -- Case-insensitive, accent-sensitive (Spanish-friendly)
END
GO

USE EventosVivos;
GO

-- =============================================================================
-- SECTION 1 — Drop existing objects (safe re-run)
-- =============================================================================
IF OBJECT_ID('reservations', 'U') IS NOT NULL DROP TABLE reservations;
IF OBJECT_ID('events',       'U') IS NOT NULL DROP TABLE events;
IF OBJECT_ID('venues',       'U') IS NOT NULL DROP TABLE venues;
GO

-- =============================================================================
-- SECTION 2 — VENUES
-- Reference data. Three fixed venues as stated in the requirements.
-- =============================================================================
CREATE TABLE venues (
    Id       INT           NOT NULL,
    [Name]     NVARCHAR(150) NOT NULL,
    Capacity INT           NOT NULL,
    City     NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_venues      PRIMARY KEY (Id),
    CONSTRAINT CHK_venues_cap CHECK (Capacity > 0)
);
GO

-- =============================================================================
-- SECTION 3 — EVENTS
-- =============================================================================
CREATE TABLE events (
    Id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    Title            NVARCHAR(100)    NOT NULL,
    [Description]    NVARCHAR(500)    NOT NULL,
    VenueId          INT              NOT NULL,
    MaxCapacity      INT              NOT NULL,
    StartDateTimeUtc DATETIME2(0)     NOT NULL,  -- UTC, seconds precision
    EndDateTimeUtc   DATETIME2(0)     NOT NULL,
    TicketPrice      DECIMAL(18, 2)   NOT NULL,
    -- Enum stored as string for readability: Conferencia | Taller | Concierto
    [Type]           NVARCHAR(20)     NOT NULL,
    -- Enum stored as string: Activo | Cancelado | Completado
    -- NOTE: Completado is computed at read time (RN06); stored value may be
    --       stale for events that just ended. The API always calls RefreshStatus().
    [Status]           NVARCHAR(20)     NOT NULL DEFAULT 'Activo',
    CreatedAtUtc     DATETIME2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_events             PRIMARY KEY (Id),
    CONSTRAINT FK_events_venue       FOREIGN KEY (VenueId) REFERENCES venues(Id),
    CONSTRAINT CHK_events_capacity   CHECK (MaxCapacity > 0),
    CONSTRAINT CHK_events_price      CHECK (TicketPrice > 0),
    CONSTRAINT CHK_events_dates      CHECK (EndDateTimeUtc > StartDateTimeUtc),
    CONSTRAINT CHK_events_type       CHECK (Type   IN ('Conferencia', 'Taller', 'Concierto')),
    CONSTRAINT CHK_events_status     CHECK (Status IN ('Activo', 'Cancelado', 'Completado')),
    -- RN01: MaxCapacity <= venue capacity is enforced at the application layer
    --       because SQL Server CHECK constraints cannot reference other tables.
);
GO

-- Indexes to support RF-02 list filters and RN02 overlap query
CREATE INDEX IX_events_venue_dates
    ON events (VenueId, StartDateTimeUtc, EndDateTimeUtc)
    INCLUDE (Status);

CREATE INDEX IX_events_status
    ON events (Status)
    INCLUDE (VenueId, StartDateTimeUtc, EndDateTimeUtc);

CREATE INDEX IX_events_type
    ON events (Type);

-- Full-text search index for RF-02 title search (case-insensitive thanks to CI collation)
-- Requires Full-Text Search feature; comment out if not available.
-- CREATE FULLTEXT CATALOG ev_catalog AS DEFAULT;
-- CREATE FULLTEXT INDEX ON events(Title) KEY INDEX PK_events;
GO

-- =============================================================================
-- SECTION 4 — RESERVATIONS
-- =============================================================================
CREATE TABLE reservations (
    Id                   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    EventId              UNIQUEIDENTIFIER NOT NULL,
    Quantity             INT              NOT NULL,
    BuyerName            NVARCHAR(150)    NOT NULL,
    BuyerEmail           NVARCHAR(255)    NOT NULL,
    -- Enum: PendientePago | Confirmada | Cancelada
    [Status]               NVARCHAR(30)     NOT NULL DEFAULT 'PendientePago',
    -- Format: EV-XXXXXX  (6 digits). NULL until RF-04 is called.
    ReservationCode      NCHAR(9)         NULL,
    IsLostOnCancellation BIT              NOT NULL DEFAULT 0,
    CreatedAtUtc         DATETIME2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    ConfirmedAtUtc       DATETIME2(0)     NULL,
    CancelledAtUtc       DATETIME2(0)     NULL,

    CONSTRAINT PK_reservations        PRIMARY KEY (Id),
    CONSTRAINT FK_reservations_event  FOREIGN KEY (EventId) REFERENCES events(Id)
                                      ON DELETE CASCADE,
    CONSTRAINT CHK_reservations_qty   CHECK (Quantity >= 1),
    CONSTRAINT CHK_reservations_status CHECK (Status IN ('PendientePago', 'Confirmada', 'Cancelada')),

    -- RF-04: ReservationCode must be unique when set (NCHAR enforces fixed length EV-XXXXXX)
    CONSTRAINT UQ_reservations_code   UNIQUE (ReservationCode)   -- NULLs are distinct in SQL Server
);
GO

-- Indexes
CREATE INDEX IX_reservations_eventId
    ON reservations (EventId)
    INCLUDE (Status, Quantity, IsLostOnCancellation);

CREATE INDEX IX_reservations_status
    ON reservations (Status);

CREATE INDEX IX_reservations_buyer_email
    ON reservations (BuyerEmail);
GO

-- =============================================================================
-- SECTION 5 — SEED DATA (Reference venues)
-- =============================================================================
-- These three venues are fixed per the technical requirements.
-- They are idempotent — safe to re-run.
INSERT INTO venues (Id, Name, Capacity, City)
SELECT 1, N'Auditorio Central', 200, N'Bogotá'
WHERE NOT EXISTS (SELECT 1 FROM venues WHERE Id = 1);

INSERT INTO venues (Id, Name, Capacity, City)
SELECT 2, N'Sala Norte', 50, N'Bogotá'
WHERE NOT EXISTS (SELECT 1 FROM venues WHERE Id = 2);

INSERT INTO venues (Id, Name, Capacity, City)
SELECT 3, N'Arena Sur', 500, N'Medellín'
WHERE NOT EXISTS (SELECT 1 FROM venues WHERE Id = 3);
GO

-- =============================================================================
-- SECTION 6 — Helper view: Occupancy (RF-06 — readable from SSMS)
-- =============================================================================
CREATE OR ALTER VIEW vw_event_occupancy AS
SELECT
    e.Id                                                          AS EventId,
    e.Title                                                       AS EventTitle,
    e.MaxCapacity,
    e.TicketPrice,
    e.Status,
    e.StartDateTimeUtc,
    e.EndDateTimeUtc,
    v.Name                                                        AS VenueName,
    v.City,

    -- Confirmed tickets (RF-06)
    ISNULL(SUM(CASE WHEN r.Status = 'Confirmada' THEN r.Quantity ELSE 0 END), 0)
        AS ConfirmedTickets,

    -- Available = capacity - (confirmed + pending_payment)
    e.MaxCapacity
        - ISNULL(SUM(CASE
            WHEN r.Status IN ('Confirmada', 'PendientePago') THEN r.Quantity
            ELSE 0
          END), 0)
        AS AvailableTickets,

    -- Lost tickets RN07 (cancelled but counted as occupied)
    ISNULL(SUM(CASE
        WHEN r.Status = 'Cancelada' AND r.IsLostOnCancellation = 1 THEN r.Quantity
        ELSE 0
    END), 0) AS LostTickets,

    -- Occupancy % (only confirmed)
    CASE
        WHEN e.MaxCapacity = 0 THEN 0
        ELSE CAST(
            ISNULL(SUM(CASE WHEN r.Status = 'Confirmada' THEN r.Quantity ELSE 0 END), 0)
            * 100.0 / e.MaxCapacity
        AS DECIMAL(5,2))
    END AS OccupancyPercentage,

    -- Total revenue confirmed
    ISNULL(SUM(CASE WHEN r.Status = 'Confirmada' THEN r.Quantity ELSE 0 END), 0)
        * e.TicketPrice AS TotalRevenue

FROM events      e
JOIN venues      v ON v.Id = e.VenueId
LEFT JOIN reservations r ON r.EventId = e.Id
GROUP BY
    e.Id, e.Title, e.MaxCapacity, e.TicketPrice,
    e.Status, e.StartDateTimeUtc, e.EndDateTimeUtc,
    v.Name, v.City;
GO

-- =============================================================================
-- SECTION 7 — Verify
-- =============================================================================
SELECT 'venues'       AS [Table], COUNT(*) AS Rows FROM venues       UNION ALL
SELECT 'events',                  COUNT(*)          FROM events       UNION ALL
SELECT 'reservations',            COUNT(*)          FROM reservations;
GO

PRINT 'EventosVivos schema created successfully.';
GO
