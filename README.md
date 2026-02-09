# Railcar Trip Management

## A .NET 10 Blazor WebAssembly Full stack App

### Docs

- Documentation for the project

### Resources

- Contains data files used in the project

### Client (Blazor WASM)

- Railcar Trips page
- CSV upload UI
- Trips grid
- Optional trip details (events list)

### Server (ASP.NET Core)

- API endpoints
- CSV parsing and trip processing logic
- EF Core data access
- Database seeding

### Shared

- DTOs shared between client and server

---

## Database Choice

**SQLite** was chosen for this assessment.

### Rationale

- Zero configuration for reviewers
- Portable and lightweight
- Works well with EF Core
- Suitable for the data size and access patterns in this task

The data access layer is provider-agnostic. Switching to SQL Server or PostgreSQL would only require updating the EF provider and connection string.

---

## Database Schema

### City

| Column     | Description                     |
| ---------- | ------------------------------- |
| CityId     | Primary key                     |
| CityName   | City name                       |
| TimeZoneId | Windows time zone ID (from CSV) |

### Trip

| Column            | Description                  |
| ----------------- | ---------------------------- |
| TripId            | Primary key                  |
| EquipmentId       | Railcar/equipment identifier |
| OriginCityId      | City where trip started      |
| DestinationCityId | City where trip ended        |
| StartUtc          | Trip start time (UTC)        |
| EndUtc            | Trip end time (UTC)          |
| TotalTripHours    | Calculated duration          |

### EquipmentEvent (used for bonus view)

| Column         | Description              |
| -------------- | ------------------------ |
| EventId        | Primary key              |
| EquipmentId    | Equipment identifier     |
| CityId         | Event city               |
| EventCode      | Event code (W, Z, etc.)  |
| EventTimeLocal | Original local timestamp |
| EventTimeUtc   | Converted UTC timestamp  |
| TripId         | Nullable FK to Trip      |

### EventCodeDefinition (reference data)

| Column           | Description                                 |
| ---------------- | ------------------------------------------- |
| EventCode        | Primary key (W, A, D, Z)                    |
| EventDescription | Short description (Released, Arrived, etc.) |
| LongDescription  | Full description                            |

---

## CSV Files

### `canadian_cities.csv` (Reference Data - Seeded)

Used to seed the Cities table.

- City Id
- City Name
- Time Zone (Windows time zone ID)

### `event_code_definitions.csv` (Reference Data - Seeded)

Used to seed the EventCodeDefinitions table.

- Event Code (W, A, D, Z)
- Event Description (Released, Arrived, Departed, Placed)
- Long Description (Full text explanation)

### `equipment_events.csv` (Transactional Data - User Upload)

Uploaded via the UI.

- Equipment Id
- Event Code
- Event Time (local)
- City Id

---

## Trip Processing Logic

1. Parse CSV rows into event records
2. Resolve city time zone
3. Convert event time from **local time → UTC**
4. Group events by Equipment Id
5. Sort events by UTC time
6. Process events per equipment:
   - `W` starts a trip
   - `Z` ends the active trip
7. Persist valid trips and related events

---

## Assumptions

- Origin city = city of the `W` (Released) event
- Destination city = city of the `Z` (Placed) event
- Event codes other than `W` and `Z` do not affect trip boundaries
- Events are unordered in the CSV
- Trailing `W` events without a matching `Z` indicate **incomplete trips**
- Incomplete trips are skipped and logged as warnings
- City time zones are provided as **Windows Time Zone IDs**
- The application runs on Windows (or equivalent TZ support)

---

## Edge Case Handling

| Scenario                 | Behavior                                           |
| ------------------------ | -------------------------------------------------- |
| `W` without matching `Z` | Trip not created, warning logged                   |
| `Z` without open trip    | Event skipped, warning logged                      |
| Multiple `W` before `Z`  | Previous trip considered invalid, new trip started |
| Invalid CSV rows         | Row skipped, import continues                      |
| Duplicate uploads        | Not prevented (see TODOs)                          |

---

## API Endpoints

### Import Events

`POST /api/railcar-trips/import`

- Accepts CSV upload
- Processes events into trips
- Returns summary:
  - Events parsed
  - Trips created
  - Incomplete trips
  - Warnings/errors count

### Get Trips

`GET /api/railcar-trips`

- Returns trip list for grid display

### Get Trip Events (Bonus)

`GET /api/railcar-trips/{tripId}/events`

- Returns ordered events for selected trip

---

## Security Considerations

- No authentication required for this assessment
- File upload validation:
  - CSV-only
  - File size limits
  - Defensive parsing
- Database access restricted to server only

---

## TODOs / Future Improvements

- **Add duplicate event detection** - Prevent re-importing the same events
  - Define uniqueness criteria (EquipmentId + EventCode + EventTime + CityId)
  - Skip duplicates or warn user with count
  - Add database indexes for efficient duplicate checking
- **Implement Azure Event Hub / Message-based ingestion** - Eliminate manual CSV uploads
  - Real-time event streaming from source systems
  - Azure Event Hub or Service Bus integration
  - Automatic processing and trip calculation
  - Scale to handle high-volume event streams
- Add idempotency (prevent duplicate imports with batch tracking)
- Add pagination and filtering to trips grid
- Add unit tests for trip processing logic
- Improve handling of Windows ↔ IANA time zone mapping
- Persist import batches for auditability
- Improve UI/UX feedback and progress indicators
- Add configurable behavior for incomplete trips

---

## How to Run

1. Clone the repository
2. Ensure .NET SDK is installed
3. Run database migrations
4. Start the Server project (API)
5. Start the Client project (WebAssembly)
6. Navigate to the Railcar Trips page and upload `equipment_events.csv`

---

## Notes

This solution is intentionally scoped to demonstrate architecture, data handling, and reasoning rather than a fully production-hardened system. Clear TODOs and assumptions are included to reflect design considerations that would be addressed in a full implementation.

---

## Author

### Festus O. Adekunle

- Repository: [RailcarTripManagement](https://github.com/olusolaadek/RailcarTripManagement)
