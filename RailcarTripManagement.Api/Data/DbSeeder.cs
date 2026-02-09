using Microsoft.EntityFrameworkCore;
using RailcarTripManagement.Api.Data;
using RailcarTripManagement.Api.Services;

namespace RailcarTripManagement.Api.Data;

public class DbSeeder
{
    private readonly RailcarDbContext _context;
    private readonly ICsvParsingService _csvParser;
    private readonly ILogger<DbSeeder> _logger;
    
    public DbSeeder(
        RailcarDbContext context,
        ICsvParsingService csvParser,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _csvParser = csvParser;
        _logger = logger;
    }
    
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            
            // Seed event code definitions first (referenced by events)
            await SeedEventCodeDefinitionsAsync();
            
            // Check if cities already exist
            if (await _context.Cities.AnyAsync())
            {
                _logger.LogInformation("Database already seeded");
                return;
            }
            
            _logger.LogInformation("Seeding database with Canadian cities");
            
            // Load cities from embedded resource or file
            var citiesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "canadian_cities.csv");
            
            if (!File.Exists(citiesFilePath))
            {
                _logger.LogWarning("Cities CSV file not found at {Path}. Creating sample cities.", citiesFilePath);
                await SeedSampleCitiesAsync();
                return;
            }
            
            using var stream = File.OpenRead(citiesFilePath);
            var cities = await _csvParser.ParseCitiesAsync(stream);
            
            if (cities.Any())
            {
                await _context.Cities.AddRangeAsync(cities);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} cities", cities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            // Don't throw - allow app to start even if seeding fails
        }
    }
    
    private async Task SeedSampleCitiesAsync()
    {
        // Sample cities for development/testing
        var sampleCities = new[]
        {
            new Models.City { CityId = 1, CityName = "Calgary", TimeZone = "Mountain Standard Time" },
            new Models.City { CityId = 2, CityName = "Edmonton", TimeZone = "Mountain Standard Time" },
            new Models.City { CityId = 3, CityName = "Vancouver", TimeZone = "Pacific Standard Time" },
            new Models.City { CityId = 4, CityName = "Toronto", TimeZone = "Eastern Standard Time" },
            new Models.City { CityId = 5, CityName = "Montreal", TimeZone = "Eastern Standard Time" }
        };
        
        await _context.Cities.AddRangeAsync(sampleCities);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample cities", sampleCities.Length);
    }
    
    private async Task SeedEventCodeDefinitionsAsync()
    {
        // Check if event codes already exist
        if (await _context.EventCodeDefinitions.AnyAsync())
        {
            _logger.LogInformation("Event code definitions already seeded");
            return;
        }
        
        _logger.LogInformation("Seeding event code definitions");
        
        // Try to load from CSV file
        var eventCodesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "event_code_definitions.csv");
        
        if (File.Exists(eventCodesFilePath))
        {
            try
            {
                var eventCodes = await ParseEventCodeDefinitionsAsync(eventCodesFilePath);
                await _context.EventCodeDefinitions.AddRangeAsync(eventCodes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} event code definitions from CSV", eventCodes.Count);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load event codes from CSV, using defaults");
            }
        }
        
        // Fallback to hardcoded event codes
        var defaultEventCodes = new[]
        {
            new Models.EventCodeDefinition
            {
                EventCode = "W",
                EventDescription = "Released",
                LongDescription = "Railcar equipment is released from origin"
            },
            new Models.EventCodeDefinition
            {
                EventCode = "A",
                EventDescription = "Arrived",
                LongDescription = "Railcar equipment arrives at a city on route"
            },
            new Models.EventCodeDefinition
            {
                EventCode = "D",
                EventDescription = "Departed",
                LongDescription = "Railcar equipment departs from a city on route"
            },
            new Models.EventCodeDefinition
            {
                EventCode = "Z",
                EventDescription = "Placed",
                LongDescription = "Railcar equipment is placed at destination"
            }
        };
        
        await _context.EventCodeDefinitions.AddRangeAsync(defaultEventCodes);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default event code definitions", defaultEventCodes.Length);
    }
    
    private async Task<List<Models.EventCodeDefinition>> ParseEventCodeDefinitionsAsync(string filePath)
    {
        var eventCodes = new List<Models.EventCodeDefinition>();
        
        using var reader = new StreamReader(filePath);
        // Skip header
        await reader.ReadLineAsync();
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var parts = line.Split(',');
            if (parts.Length >= 3)
            {
                eventCodes.Add(new Models.EventCodeDefinition
                {
                    EventCode = parts[0].Trim(),
                    EventDescription = parts[1].Trim(),
                    LongDescription = parts[2].Trim()
                });
            }
        }
        
        return eventCodes;
    }
}
