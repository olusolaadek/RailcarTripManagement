using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailcarTripManagement.Api.Data;
using RailcarTripManagement.Api.Services;
using RailcarTripManagement.Shared.Models;

namespace RailcarTripManagement.Api.Controllers;

[ApiController]
[Route("api/railcar-trips")]
public class RailcarTripsController : ControllerBase
{
    private readonly RailcarDbContext _context;
    private readonly ICsvParsingService _csvParser;
    private readonly ITripProcessingService _tripProcessor;
    private readonly ILogger<RailcarTripsController> _logger;
    
    public RailcarTripsController(
        RailcarDbContext context,
        ICsvParsingService csvParser,
        ITripProcessingService tripProcessor,
        ILogger<RailcarTripsController> logger)
    {
        _context = context;
        _csvParser = csvParser;
        _tripProcessor = tripProcessor;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all trips
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TripDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TripDto>>> GetTrips()
    {
        try
        {
            var trips = await _context.Trips
                .Include(t => t.OriginCity)
                .Include(t => t.DestinationCity)
                .OrderByDescending(t => t.StartUtc)
                .Select(t => new TripDto
                {
                    TripId = t.TripId,
                    EquipmentId = t.EquipmentId,
                    OriginCity = t.OriginCity.CityName,
                    OriginCityId = t.OriginCityId,
                    DestinationCity = t.DestinationCity.CityName,
                    DestinationCityId = t.DestinationCityId,
                    StartUtc = t.StartUtc,
                    EndUtc = t.EndUtc,
                    TotalTripHours = t.TotalTripHours
                })
                .ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} trips", trips.Count);
            return Ok(trips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trips");
            return StatusCode(500, "An error occurred while retrieving trips");
        }
    }
    
    /// <summary>
    /// Get events for a specific trip
    /// </summary>
    [HttpGet("{tripId}/events")]
    [ProducesResponseType(typeof(List<EquipmentEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EquipmentEventDto>>> GetTripEvents(int tripId)
    {
        try
        {
            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null)
            {
                return NotFound($"Trip {tripId} not found");
            }
            
            var events = await _context.EquipmentEvents
                .Include(e => e.City)
                .Where(e => e.TripId == tripId)
                .OrderBy(e => e.EventTimeUtc)
                .Select(e => new EquipmentEventDto
                {
                    EventId = e.EventId,
                    EquipmentId = e.EquipmentId,
                    EventCode = e.EventCode,
                    CityName = e.City.CityName,
                    CityId = e.CityId,
                    EventTimeLocal = e.EventTimeLocal,
                    EventTimeUtc = e.EventTimeUtc,
                    TripId = e.TripId
                })
                .ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} events for trip {TripId}", events.Count, tripId);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for trip {TripId}", tripId);
            return StatusCode(500, "An error occurred while retrieving trip events");
        }
    }
    
    /// <summary>
    /// Import equipment events from CSV file
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> ImportEvents(IFormFile file)
    {
        var result = new ImportResultDto { Success = false };
        
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file uploaded");
                return BadRequest(result);
            }
            
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("File must be a CSV file");
                return BadRequest(result);
            }
            
            if (file.Length > 10 * 1024 * 1024) // 10MB limit
            {
                result.Errors.Add("File size exceeds 10MB limit");
                return BadRequest(result);
            }
            
            _logger.LogInformation("Starting import of file {FileName} ({Size} bytes)", file.FileName, file.Length);
            
            // Load cities for reference
            var cities = await _context.Cities.ToListAsync();
            if (!cities.Any())
            {
                result.Errors.Add("No cities found in database. Please seed cities first.");
                return BadRequest(result);
            }
            
            var citiesById = cities.ToDictionary(c => c.CityId);
            
            // Parse CSV
            List<Models.EquipmentEvent> events;
            try
            {
                using var stream = file.OpenReadStream();
                events = await _csvParser.ParseEventsAsync(stream, citiesById);
                result.EventsParsed = events.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV file");
                result.Errors.Add($"Error parsing CSV: {ex.Message}");
                result.ErrorsCount = result.Errors.Count;
                return BadRequest(result);
            }
            
            if (events.Count == 0)
            {
                result.Warnings.Add("No valid events found in CSV file");
                result.WarningsCount = 1;
                result.Success = true; // Not an error, just no data
                return Ok(result);
            }
            
            // Process events into trips
            var (trips, warnings) = _tripProcessor.ProcessEventsIntoTrips(events);
            result.Warnings.AddRange(warnings);
            result.WarningsCount = warnings.Count;
            result.IncompleteTrips = warnings.Count(w => w.Contains("Incomplete trip"));
            
            // Save to database
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Save trips (which will also save associated events due to cascade)
                if (trips.Any())
                {
                    await _context.Trips.AddRangeAsync(trips);
                    await _context.SaveChangesAsync();
                }
                
                // Save any events that weren't associated with trips
                var unassociatedEvents = events.Where(e => e.TripId == null).ToList();
                if (unassociatedEvents.Any())
                {
                    await _context.EquipmentEvents.AddRangeAsync(unassociatedEvents);
                    await _context.SaveChangesAsync();
                }
                
                await transaction.CommitAsync();
                
                result.TripsCreated = trips.Count;
                result.Success = true;
                
                _logger.LogInformation(
                    "Import completed: {EventCount} events parsed, {TripCount} trips created, {WarningCount} warnings",
                    result.EventsParsed, result.TripsCreated, result.WarningsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving trips to database");
                result.Errors.Add($"Error saving to database: {ex.Message}");
                result.ErrorsCount = result.Errors.Count;
                result.Success = false;
                return StatusCode(500, result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during import");
            result.Errors.Add($"Unexpected error: {ex.Message}");
            result.ErrorsCount = result.Errors.Count;
            return StatusCode(500, result);
        }
    }
    
    /// <summary>
    /// Get all event code definitions
    /// </summary>
    [HttpGet("event-codes")]
    [ProducesResponseType(typeof(List<EventCodeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventCodeDefinitionDto>>> GetEventCodes()
    {
        try
        {
            var eventCodes = await _context.EventCodeDefinitions
                .OrderBy(e => e.EventCode)
                .Select(e => new EventCodeDefinitionDto
                {
                    EventCode = e.EventCode,
                    EventDescription = e.EventDescription,
                    LongDescription = e.LongDescription
                })
                .ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} event code definitions", eventCodes.Count);
            return Ok(eventCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event codes");
            return StatusCode(500, "An error occurred while retrieving event codes");
        }
    }
}
