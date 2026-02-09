namespace RailcarTripManagement.Shared.Models;

public class ImportResultDto
{
    public int EventsParsed { get; set; }
    public int TripsCreated { get; set; }
    public int IncompleteTrips { get; set; }
    public int WarningsCount { get; set; }
    public int ErrorsCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool Success { get; set; }
}
