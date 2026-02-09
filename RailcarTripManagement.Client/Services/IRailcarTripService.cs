using RailcarTripManagement.Shared.Models;

namespace RailcarTripManagement.Client.Services;

public interface IRailcarTripService
{
    Task<List<TripDto>> GetTripsAsync();
    Task<List<EquipmentEventDto>> GetTripEventsAsync(int tripId);
    Task<ImportResultDto> ImportEventsAsync(Stream fileStream, string fileName);
}
