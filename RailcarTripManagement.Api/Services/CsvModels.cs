namespace RailcarTripManagement.Api.Services;

public class CsvEventRecord
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string EventTime { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
}

public class CsvCityRecord
{
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}
