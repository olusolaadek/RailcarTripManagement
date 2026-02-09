# Railcar Trip Management - API

## Overview
This is the backend ASP.NET Core Web API for the Railcar Trip Management system.

## Features

### 1. CSV Import & Processing
- Parses equipment events CSV files
- Validates event data
- Converts local timestamps to UTC based on city time zones
- Groups events by equipment ID
- Processes events into trips using W/Z logic

### 2. Trip Processing Logic
- **W (Released)** event starts a new trip
- **Z (Placed)** event ends the current trip
- Incomplete trips (W without Z) are logged as warnings
- Events are ordered chronologically by UTC time
- Multiple W events without Z invalidate the previous trip

### 3. RESTful API Endpoints
```
GET  /api/railcar-trips              - Get all trips
GET  /api/railcar-trips/{id}/events  - Get events for a specific trip
POST /api/railcar-trips/import       - Import events from CSV file
```

### 4. Database Management
- SQLite database for zero-configuration setup
- Entity Framework Core with Code First approach
- Automatic database seeding from canadian_cities.csv
- Indexes for performance optimization

## Project Structure

```
RailcarTripManagement.Api/
??? Controllers/
?   ??? RailcarTripsController.cs    # API endpoints
??? Data/
?   ??? RailcarDbContext.cs          # EF Core DbContext
?   ??? DbSeeder.cs                  # Database seeding logic
?   ??? canadian_cities.csv          # City master data
??? Models/
?   ??? City.cs                      # City entity
?   ??? Trip.cs                      # Trip entity
?   ??? EquipmentEvent.cs            # Event entity
??? Services/
?   ??? CsvParsingService.cs         # CSV parsing logic
?   ??? TripProcessingService.cs     # Trip processing algorithm
?   ??? CsvModels.cs                 # CSV record models
??? Program.cs                        # Application configuration
??? appsettings.json                  # Configuration settings
```

## Database Schema

### Cities Table
| Column | Type | Description |
|--------|------|-------------|
| CityId | int (PK) | Primary key |
| CityName | string(100) | City name |
| TimeZoneId | string(100) | Windows time zone ID |

### Trips Table
| Column | Type | Description |
|--------|------|-------------|
| TripId | int (PK) | Primary key |
| EquipmentId | string(50) | Equipment identifier |
| OriginCityId | int (FK) | Origin city ID |
| DestinationCityId | int (FK) | Destination city ID |
| StartUtc | DateTime | Trip start time (UTC) |
| EndUtc | DateTime | Trip end time (UTC) |
| TotalTripHours | double | Duration in hours |

### EquipmentEvents Table
| Column | Type | Description |
|--------|------|-------------|
| EventId | int (PK) | Primary key |
| EquipmentId | string(50) | Equipment identifier |
| CityId | int (FK) | Event city ID |
| EventCode | string(10) | Event code (W, Z, etc.) |
| EventTimeLocal | DateTime | Original local timestamp |
| EventTimeUtc | DateTime | Converted UTC timestamp |
| TripId | int? (FK) | Associated trip (nullable) |

### EventCodeDefinitions Table
| Column | Type | Description |
|--------|------|-------------|
| EventCode | string (PK) | Event code (W, A, D, Z) |
| EventDescription | string(100) | Short description |
| LongDescription | string(500) | Full description |

**Event Codes:**
- **W** - Released: Railcar equipment is released from origin
- **A** - Arrived: Railcar equipment arrives at a city on route
- **D** - Departed: Railcar equipment departs from a city on route
- **Z** - Placed: Railcar equipment is placed at destination

## API Endpoints

### 1. Import Events
**POST** `/api/railcar-trips/import`

Upload a CSV file containing equipment events.

**Request:**
- Content-Type: `multipart/form-data`
- Form field: `file` (CSV file)

**CSV Format:**
```csv
Equipment Id,Event Code,Event Time,City Id
BNSF123456,W,2024-01-15 08:30:00,1
BNSF123456,Z,2024-01-15 14:45:00,2
```

**Response:** `200 OK`
```json
{
  "success": true,
  "eventsParsed": 150,
  "tripsCreated": 42,
  "incompleteTrips": 3,
  "warningsCount": 3,
  "errorsCount": 0,
  "warnings": [
    "Equipment BNSF123456: Incomplete trip starting at 2024-01-20 10:00:00 UTC - no Z event found"
  ],
  "errors": []
}
```

### 2. Get All Trips
**GET** `/api/railcar-trips`

Retrieve all trips, ordered by start time descending.

**Response:** `200 OK`
```json
[
  {
    "tripId": 1,
    "equipmentId": "BNSF123456",
    "originCity": "Calgary",
    "originCityId": 1,
    "destinationCity": "Edmonton",
    "destinationCityId": 2,
    "startUtc": "2024-01-15T15:30:00Z",
    "endUtc": "2024-01-15T21:45:00Z",
    "totalTripHours": 6.25
  }
]
```

### 3. Get Trip Events
**GET** `/api/railcar-trips/{tripId}/events`

Retrieve all events for a specific trip, ordered chronologically.

**Response:** `200 OK`
```json
[
  {
    "eventId": 1,
    "equipmentId": "BNSF123456",
    "eventCode": "W",
    "cityName": "Calgary",
    "cityId": 1,
    "eventTimeLocal": "2024-01-15T08:30:00",
    "eventTimeUtc": "2024-01-15T15:30:00Z",
    "tripId": 1
  },
  {
    "eventId": 2,
    "equipmentId": "BNSF123456",
    "eventCode": "Z",
    "cityName": "Edmonton",
    "cityId": 2,
    "eventTimeLocal": "2024-01-15T14:45:00",
    "eventTimeUtc": "2024-01-15T21:45:00Z",
    "tripId": 1
  }
]
```

### 4. Get Event Code Definitions
**GET** `/api/railcar-trips/event-codes`

Retrieve all event code definitions.

**Response:** `200 OK`
```json
[
  {
    "eventCode": "W",
    "eventDescription": "Released",
    "longDescription": "Railcar equipment is released from origin"
  },
  {
    "eventCode": "A",
    "eventDescription": "Arrived",
    "longDescription": "Railcar equipment arrives at a city on route"
  },
  {
    "eventCode": "D",
    "eventDescription": "Departed",
    "longDescription": "Railcar equipment departs from a city on route"
  },
  {
    "eventCode": "Z",
    "eventDescription": "Placed",
    "longDescription": "Railcar equipment is placed at destination"
  }
]
```

## Configuration

### Database Connection
Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=railcar_trips.db"
  }
}
```

### CORS Policy
CORS is configured in `Program.cs` to allow Blazor WASM client:
```csharp
options.AddPolicy("AllowBlazorClient", policy =>
{
    policy.WithOrigins("https://localhost:7000", "http://localhost:5000")
          .AllowAnyMethod()
          .AllowAnyHeader();
});
```

## Running the API

### Development
```bash
cd RailcarTripManagement.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7198`
- HTTP: `http://localhost:5198`
- Swagger UI: `https://localhost:7198/swagger`

### Testing with Swagger
1. Navigate to `https://localhost:7198/swagger`
2. Explore API endpoints
3. Test import with sample CSV
4. View results

## Dependencies

- **Microsoft.EntityFrameworkCore.Sqlite** (10.0.2) - SQLite database provider
- **Microsoft.EntityFrameworkCore.Design** (10.0.2) - EF Core tools
- **CsvHelper** (33.1.0) - CSV parsing library
- **Swashbuckle.AspNetCore** (10.1.1) - Swagger/OpenAPI generation

## Time Zone Handling

The API handles time zone conversions using Windows Time Zone IDs:
- **Mountain Standard Time** - Alberta (Calgary, Edmonton)
- **Pacific Standard Time** - British Columbia (Vancouver)
- **Eastern Standard Time** - Ontario/Quebec (Toronto, Montreal)
- **Atlantic Standard Time** - Maritime provinces
- **Newfoundland Standard Time** - Newfoundland

### Conversion Process
1. Event time is provided in local time for the city
2. City's TimeZoneId is retrieved from database
3. Local time is converted to UTC using `TimeZoneInfo`
4. UTC time is stored and used for all calculations

## Error Handling

The API implements comprehensive error handling:

### Validation Errors (400 Bad Request)
- Missing or invalid file
- File too large (>10MB)
- Invalid CSV format
- Missing required fields

### Not Found Errors (404 Not Found)
- Trip ID doesn't exist
- City ID not found

### Server Errors (500 Internal Server Error)
- Database connection failures
- Unexpected exceptions
- Time zone conversion errors

All errors are logged with appropriate log levels.

## Logging

Logging is configured in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Log Levels by Component
- **Controllers**: Information (requests, results)
- **Services**: Debug (processing details), Warning (validation issues)
- **EF Core**: Information (queries in development)

## Database Seeding

On first run, the API automatically:
1. Creates the SQLite database
2. Applies EF Core migrations
3. Seeds cities from `canadian_cities.csv`
4. Creates necessary indexes

If `canadian_cities.csv` is not found, sample cities are seeded instead.

## Performance Considerations

### Indexes
- `Cities.CityName` - Fast city lookups
- `Trips.EquipmentId` - Filter by equipment
- `Trips.StartUtc` - Chronological sorting
- `EquipmentEvents.EquipmentId` - Event grouping
- `EquipmentEvents.EventTimeUtc` - Time-based queries
- `EquipmentEvents.TripId` - Trip-event joins

### Query Optimization
- Eager loading with `Include()` for navigation properties
- Projection to DTOs to reduce data transfer
- Ordering at database level

## Security Considerations

### Current Implementation (Assessment)
- No authentication required
- CORS configured for development
- File upload validation (type, size)
- SQL injection protected by EF Core parameterization

### Production Recommendations
- Add authentication/authorization (JWT, Azure AD)
- Restrict CORS origins to production client URL
- Add rate limiting
- Implement API versioning
- Add request size limits
- Enable HTTPS only
- Add input sanitization
- Implement audit logging

## TODOs / Future Improvements

### High Priority
- [ ] Add pagination to GET /api/railcar-trips (performance)
- [ ] Implement idempotency for imports (prevent duplicates)
- [ ] Add batch ID tracking for imports
- [ ] Create database migration scripts
- [ ] Add comprehensive unit tests
- [ ] Add integration tests with in-memory database

### Medium Priority
- [ ] Add filtering/sorting query parameters
- [ ] Implement caching for cities lookup
- [ ] Add background job processing for large imports
- [ ] Improve IANA timezone mapping support
- [ ] Add API rate limiting
- [ ] Implement soft deletes

### Low Priority
- [ ] Add GraphQL endpoint
- [ ] Export trips to CSV/Excel
- [ ] Add database backup/restore endpoints
- [ ] Implement audit trail for changes
- [ ] Add health check endpoints
- [ ] Add metrics/telemetry (Application Insights)

## Troubleshooting

### Database File Not Found
**Symptom:** "Unable to open database file"
**Solution:** Ensure write permissions in the application directory

### Time Zone Conversion Fails
**Symptom:** Warning logs about timezone conversion
**Solution:** Verify Windows timezone IDs in canadian_cities.csv match system timezones

### CSV Import Returns 0 Events
**Symptom:** Import succeeds but creates no trips
**Solution:** 
- Verify CSV format matches expected columns
- Check that City IDs in CSV exist in database
- Review warning messages in import result

### CORS Errors from Blazor Client
**Symptom:** Browser console shows CORS policy errors
**Solution:**
- Verify client URL is in CORS policy
- Ensure API is running
- Check that requests use correct HTTP method

## Development Notes

### Adding New Event Codes
To support additional event codes beyond W and Z:

1. Update `TripProcessingService` logic
2. Add handling in the foreach loop
3. Update documentation
4. Add tests for new scenarios

### Extending the API
To add new endpoints:

1. Add method to `RailcarTripsController`
2. Create corresponding DTO in Shared project
3. Update Swagger documentation
4. Add unit tests

### Database Migrations
If you modify entity models:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Testing

### Manual Testing with cURL

**Import CSV:**
```bash
curl -X POST "https://localhost:7198/api/railcar-trips/import" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@equipment_events.csv"
```

**Get Trips:**
```bash
curl -X GET "https://localhost:7198/api/railcar-trips"
```

**Get Trip Events:**
```bash
curl -X GET "https://localhost:7198/api/railcar-trips/1/events"
```

## License
This is an assessment project for demonstration purposes.
