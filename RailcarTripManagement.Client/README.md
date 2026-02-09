# Railcar Trip Management - Blazor WebAssembly Client

## Overview

This is the front-end Blazor WebAssembly client for the Railcar Trip Management system.

## Features

### 1. CSV File Upload

- Upload `equipment_events.csv` containing railcar event data
- Client-side validation for file type and size (max 10MB)
- Real-time upload progress and feedback
- File size display in human-readable format

### 2. Trips Grid (Home Page)

Displays processed trips with the following columns:

- Equipment ID
- Origin City
- Destination City
- Start Date/Time (UTC)
- End Date/Time (UTC)
- Total Trip Hours

### 3. Trip Events View (Bonus Feature)

- Click "View Events" on any trip to see detailed event history
- Events displayed in chronological order
- Color-coded event types:
  - Green: W (Released - Trip Start)
  - Yellow: Z (Placed - Trip End)
  - Gray: A (Arrived) and D (Departed)
- **Event descriptions** loaded from reference data
- Shows both local and UTC timestamps
- Dynamic legend with all event codes
- Modal dialog interface

### 4. About Page

- Overview of the system
- Key features and technology stack
- How the trip processing works

### 5. Privacy Policy

- Data collection and usage information
- Security measures
- User rights and compliance information

## Project Structure

```
RailcarTripManagement.Client/
??? Pages/
?   ??? RailcarTrips.razor          # Main trips page (direct HttpClient implementation)
?   ??? RailcarTripsWithService.razor # Alternative using service abstraction
?   ??? RailcarTrips.razor.css      # Page-specific styles
??? Services/
?   ??? IRailcarTripService.cs      # Service interface
?   ??? RailcarTripService.cs       # API service implementation
??? Layout/
?   ??? MainLayout.razor            # App layout
?   ??? NavMenu.razor               # Navigation menu
??? Program.cs                       # App configuration
??? _Imports.razor                   # Global using statements
```

## Components

### RailcarTrips.razor

The main page component implementing all required features:

- File upload with validation
- Import results display with warnings/errors
- Trips grid with sorting and selection
- Trip events modal (bonus feature)
- Loading states and error handling

### RailcarTripService

Optional service abstraction for cleaner separation of concerns:

- `GetTripsAsync()` - Fetch all trips
- `GetTripEventsAsync(tripId)` - Fetch events for a specific trip
- `ImportEventsAsync(stream, fileName)` - Upload and process CSV file
- `GetEventCodeDefinitionsAsync()` - Fetch event code reference data

**Note:** The RailcarTrips.razor page uses HttpClient directly. The service implementation is provided as an alternative pattern.

## Configuration

### API Base URL

The client expects the API to be available at the same base URL by default.
To configure a different API endpoint:

1. Add to `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://your-api-url.com/"
}
```

1. Or set environment variable in `wwwroot/appsettings.Development.json`

## Usage

### Running the Client

```bash
dotnet run --project RailcarTripManagement.Client
```

### Uploading Events

1. Navigate to "Railcar Trips" page
2. Click "Select CSV File" button
3. Choose `equipment_events.csv`
4. Click "Process Events"
5. View import results summary
6. Trips grid automatically refreshes with new data

### Viewing Trip Events

1. Click "View Events" button on any trip row
2. Modal displays all events in chronological order
3. Events show local and UTC timestamps
4. Click "Close" or X to dismiss modal

## Error Handling

The application handles several error scenarios:

- Invalid file types (non-CSV)
- File size exceeds limit
- Network errors during upload
- API errors (4xx, 5xx responses)
- Missing or malformed data

All errors are displayed to the user with actionable messages.

## TODOs / Future Improvements

### High Priority

- [ ] Add pagination for trips grid (performance with large datasets)
- [ ] Implement filtering and sorting on grid columns
- [ ] Add search functionality (by equipment ID, city, date range)
- [ ] Improve accessibility (ARIA labels, keyboard navigation)
- [ ] Add unit tests for components

### Medium Priority

- [ ] Implement proper logging (Application Insights, Serilog)
- [ ] Add loading skeleton instead of spinner
- [ ] Export trips to CSV/Excel
- [ ] Add trip statistics dashboard
- [ ] Implement state management (Fluxor)
- [ ] Add offline support / PWA features

### Low Priority

- [ ] Dark mode support
- [ ] Customizable grid columns
- [ ] Print-friendly trip reports
- [ ] Trip comparison feature
- [ ] Add charts/visualizations

## Dependencies

- **Microsoft.AspNetCore.Components.WebAssembly** - Core Blazor WASM framework
- **System.Net.Http.Json** - JSON serialization for API calls
- **Bootstrap 5** - UI styling (via CDN)
- **Bootstrap Icons** - Icon fonts

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Performance Considerations

- File uploads limited to 10MB to prevent memory issues
- Grid virtualization should be added for 1000+ trips
- Events modal lazy-loads data only when requested
- HTTP client configured with compression support
- Static assets cached by service worker (TODO)

## Security

- File type validation on client-side (defense in depth)
- HTTPS enforced in production
- No sensitive data stored in browser storage
- API calls use CORS policy defined by server
- TODO: Add authentication/authorization when required

## Troubleshooting

### "Failed to load trips" error

- Verify API server is running
- Check browser console for CORS errors
- Verify API base URL configuration

### Upload fails silently

- Check file size (<10MB)
- Verify file is valid CSV format
- Check network tab for request/response details

### Events modal doesn't show data

- Verify trip has associated events in database
- Check API endpoint `/api/railcar-trips/{id}/events`
- Look for errors in browser console

## Development Notes

Two implementations provided:

1. **RailcarTrips.razor** - Direct HttpClient usage (simpler, fewer files)
2. **RailcarTripsWithService.razor** - Service abstraction (better for testing, scaling)

Choose based on project complexity and testing requirements.
