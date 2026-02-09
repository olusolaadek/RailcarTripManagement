using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailcarTripManagement.Api.Models;

public class EquipmentEvent
{
    [Key]
    public int EventId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EquipmentId { get; set; } = string.Empty;
    
    [Required]
    public int CityId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string EventCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime EventTimeLocal { get; set; }
    
    [Required]
    public DateTime EventTimeUtc { get; set; }
    
    // Nullable - only set when event is associated with a trip
    public int? TripId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CityId))]
    public City City { get; set; } = null!;
    
    [ForeignKey(nameof(TripId))]
    public Trip? Trip { get; set; }
}
