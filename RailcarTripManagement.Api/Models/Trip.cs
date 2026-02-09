using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailcarTripManagement.Api.Models;

public class Trip
{
    [Key]
    public int TripId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
    
    [Required]
    public int OriginCityId { get; set; }
    
    [Required]
    public int DestinationCityId { get; set; }
    
    [Required]
    public DateTime StartUtc { get; set; }
    
    [Required]
    public DateTime EndUtc { get; set; }
    
    [Required]
    public double TotalTripHours { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(OriginCityId))]
    public City OriginCity { get; set; } = null!;
    
    [ForeignKey(nameof(DestinationCityId))]
    public City DestinationCity { get; set; } = null!;
    
    public ICollection<EquipmentEvent> Events { get; set; } = new List<EquipmentEvent>();
}
