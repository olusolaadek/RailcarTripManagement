using RailcarTripManagement.Api.Models;

namespace RailcarTripManagement.Api.Services;

public interface ITripProcessingService
{
    (List<Trip> trips, List<string> warnings) ProcessEventsIntoTrips(List<EquipmentEvent> events);
}

public class TripProcessingService : ITripProcessingService
{
    private readonly ILogger<TripProcessingService> _logger;
    
    public TripProcessingService(ILogger<TripProcessingService> logger)
    {
        _logger = logger;
    }
    
    public (List<Trip> trips, List<string> warnings) ProcessEventsIntoTrips(List<EquipmentEvent> events)
    {
        var trips = new List<Trip>();
        var warnings = new List<string>();
        
        // Group events by equipment ID
        var eventsByEquipment = events
            .GroupBy(e => e.EquipmentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.EventTimeUtc).ToList());
        
        _logger.LogInformation("Processing {EquipmentCount} pieces of equipment", eventsByEquipment.Count);
        
        foreach (var kvp in eventsByEquipment)
        {
            string equipmentId = kvp.Key;
            List<EquipmentEvent> equipmentEvents = kvp.Value;
            
            _logger.LogDebug("Processing {EventCount} events for equipment {EquipmentId}", 
                equipmentEvents.Count, equipmentId);
            
            EquipmentEvent? currentTripStart = null;
            var tripEvents = new List<EquipmentEvent>();
            
            foreach (var evt in equipmentEvents)
            {
                if (evt.EventCode == "W")
                {
                    // W (Released) starts a new trip
                    if (currentTripStart != null)
                    {
                        // Previous trip was incomplete (no Z event)
                        warnings.Add($"Equipment {equipmentId}: Incomplete trip starting at {currentTripStart.EventTimeUtc:yyyy-MM-dd HH:mm:ss} UTC - no Z event found before next W event");
                        tripEvents.Clear();
                    }
                    
                    currentTripStart = evt;
                    tripEvents.Clear();
                    tripEvents.Add(evt);
                    
                    _logger.LogDebug("Equipment {EquipmentId}: Trip started at {Time} UTC in city {CityId}", 
                        equipmentId, evt.EventTimeUtc, evt.CityId);
                }
                else if (evt.EventCode == "Z")
                {
                    // Z (Placed) ends a trip
                    if (currentTripStart == null)
                    {
                        // Z event without an active trip
                        warnings.Add($"Equipment {equipmentId}: Z event at {evt.EventTimeUtc:yyyy-MM-dd HH:mm:ss} UTC without matching W event - skipping");
                        _logger.LogWarning("Equipment {EquipmentId}: Z event at {Time} UTC without active trip", 
                            equipmentId, evt.EventTimeUtc);
                        continue;
                    }
                    
                    tripEvents.Add(evt);
                    
                    // Create the trip
                    var trip = new Trip
                    {
                        EquipmentId = equipmentId,
                        OriginCityId = currentTripStart.CityId,
                        DestinationCityId = evt.CityId,
                        StartUtc = currentTripStart.EventTimeUtc,
                        EndUtc = evt.EventTimeUtc,
                        TotalTripHours = (evt.EventTimeUtc - currentTripStart.EventTimeUtc).TotalHours
                    };
                    
                    // Associate events with this trip
                    foreach (var tripEvent in tripEvents)
                    {
                        trip.Events.Add(tripEvent);
                    }
                    
                    trips.Add(trip);
                    
                    _logger.LogDebug("Equipment {EquipmentId}: Trip completed - {Hours:F2} hours from city {OriginCity} to {DestCity}", 
                        equipmentId, trip.TotalTripHours, trip.OriginCityId, trip.DestinationCityId);
                    
                    // Reset for next trip
                    currentTripStart = null;
                    tripEvents.Clear();
                }
                else
                {
                    // Other event codes - include in current trip if one is active
                    if (currentTripStart != null)
                    {
                        tripEvents.Add(evt);
                        _logger.LogDebug("Equipment {EquipmentId}: Event {EventCode} added to current trip", 
                            equipmentId, evt.EventCode);
                    }
                    else
                    {
                        _logger.LogDebug("Equipment {EquipmentId}: Event {EventCode} outside of trip - not associating", 
                            equipmentId, evt.EventCode);
                    }
                }
            }
            
            // Check for incomplete trip at the end
            if (currentTripStart != null)
            {
                warnings.Add($"Equipment {equipmentId}: Incomplete trip starting at {currentTripStart.EventTimeUtc:yyyy-MM-dd HH:mm:ss} UTC - no Z event found");
                _logger.LogWarning("Equipment {EquipmentId}: Incomplete trip at end of event list", equipmentId);
            }
        }
        
        _logger.LogInformation("Created {TripCount} trips with {WarningCount} warnings", trips.Count, warnings.Count);
        
        return (trips, warnings);
    }
}
