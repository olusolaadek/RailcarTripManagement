using System.ComponentModel.DataAnnotations;

namespace RailcarTripManagement.Api.Models;

public class City
{
    [Key]
    public int CityId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CityName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string TimeZone { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<Trip> OriginTrips { get; set; } = new List<Trip>();
    public ICollection<Trip> DestinationTrips { get; set; } = new List<Trip>();
    public ICollection<EquipmentEvent> Events { get; set; } = new List<EquipmentEvent>();
}
