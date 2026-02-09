using System.ComponentModel.DataAnnotations;

namespace RailcarTripManagement.Api.Models;

public class EventCodeDefinition
{
    [Key]
    [MaxLength(10)]
    public string EventCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EventDescription { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string LongDescription { get; set; } = string.Empty;
}
