using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RailcarTripManagement.Api.Models;

namespace RailcarTripManagement.Api.Services;

public interface ICsvParsingService
{
    Task<List<EquipmentEvent>> ParseEventsAsync(Stream stream, Dictionary<int, City> citiesById);
    Task<List<City>> ParseCitiesAsync(Stream stream);
}

public class CsvParsingService : ICsvParsingService
{
    private readonly ILogger<CsvParsingService> _logger;
    
    public CsvParsingService(ILogger<CsvParsingService> logger)
    {
        _logger = logger;
    }
    
    public async Task<List<EquipmentEvent>> ParseEventsAsync(Stream stream, Dictionary<int, City> citiesById)
    {
        var events = new List<EquipmentEvent>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };
        
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        
        await csv.ReadAsync();
        csv.ReadHeader();
        
        int rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            try
            {
                var record = new CsvEventRecord
                {
                    EquipmentId = csv.GetField<string>("Equipment Id")?.Trim() ?? string.Empty,
                    EventCode = csv.GetField<string>("Event Code")?.Trim() ?? string.Empty,
                    EventTime = csv.GetField<string>("Event Time")?.Trim() ?? string.Empty,
                    CityId = csv.GetField<string>("City Id")?.Trim() ?? string.Empty
                };
                
                // Validate required fields
                if (string.IsNullOrEmpty(record.EquipmentId) ||
                    string.IsNullOrEmpty(record.EventCode) ||
                    string.IsNullOrEmpty(record.EventTime) ||
                    string.IsNullOrEmpty(record.CityId))
                {
                    _logger.LogWarning("Row {RowNumber}: Missing required field(s), skipping", rowNumber);
                    continue;
                }
                
                // Parse city ID
                if (!int.TryParse(record.CityId, out int cityId))
                {
                    _logger.LogWarning("Row {RowNumber}: Invalid City Id '{CityId}', skipping", rowNumber, record.CityId);
                    continue;
                }
                
                // Verify city exists
                if (!citiesById.TryGetValue(cityId, out var city))
                {
                    _logger.LogWarning("Row {RowNumber}: City Id {CityId} not found in database, skipping", rowNumber, cityId);
                    continue;
                }
                
                // Parse event time (local to the city)
                if (!DateTime.TryParse(record.EventTime, out DateTime eventTimeLocal))
                {
                    _logger.LogWarning("Row {RowNumber}: Invalid Event Time '{EventTime}', skipping", rowNumber, record.EventTime);
                    continue;
                }
                
                // Convert local time to UTC
                DateTime eventTimeUtc;
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(city.TimeZone);
                    eventTimeUtc = TimeZoneInfo.ConvertTimeToUtc(eventTimeLocal, timeZone);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Row {RowNumber}: Failed to convert time for timezone {TimeZone}, using local time as UTC", 
                        rowNumber, city.TimeZone);
                    eventTimeUtc = eventTimeLocal;
                }
                
                events.Add(new EquipmentEvent
                {
                    EquipmentId = record.EquipmentId,
                    EventCode = record.EventCode,
                    CityId = cityId,
                    EventTimeLocal = eventTimeLocal,
                    EventTimeUtc = eventTimeUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Row {RowNumber}: Error parsing row, skipping", rowNumber);
            }
        }
        
        _logger.LogInformation("Parsed {Count} events from CSV", events.Count);
        return events;
    }
    
    public async Task<List<City>> ParseCitiesAsync(Stream stream)
    {
        var cities = new List<City>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };
        
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        
        await csv.ReadAsync();
        csv.ReadHeader();
        
        while (await csv.ReadAsync())
        {
            try
            {
                var record = new CsvCityRecord
                {
                    CityId = csv.GetField<string>("City Id")?.Trim() ?? string.Empty,
                    CityName = csv.GetField<string>("City Name")?.Trim() ?? string.Empty,
                    TimeZone = csv.GetField<string>("Time Zone")?.Trim() ?? string.Empty
                };
                
                if (!int.TryParse(record.CityId, out int cityId))
                {
                    _logger.LogWarning("Invalid City Id: {CityId}", record.CityId);
                    continue;
                }
                
                cities.Add(new City
                {
                    CityId = cityId,
                    CityName = record.CityName,
                    TimeZone = record.TimeZone
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing city record");
            }
        }
        
        return cities;
    }
}
