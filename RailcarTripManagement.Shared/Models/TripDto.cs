namespace RailcarTripManagement.Shared.Models;

public class TripDto
{
    public int TripId { get; set; }
    public string EquipmentId { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public int OriginCityId { get; set; }
    public string DestinationCity { get; set; } = string.Empty;
    public int DestinationCityId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public double TotalTripHours { get; set; }
}
