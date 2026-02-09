namespace RailcarTripManagement.Shared.Models;

public class EquipmentEventDto
{
    public int EventId { get; set; }
    public string EquipmentId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public DateTime EventTimeLocal { get; set; }
    public DateTime EventTimeUtc { get; set; }
    public int? TripId { get; set; }
}
