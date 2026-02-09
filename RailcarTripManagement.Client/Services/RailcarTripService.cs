using System.Net.Http.Json;
using RailcarTripManagement.Shared.Models;

namespace RailcarTripManagement.Client.Services;

public class RailcarTripService : IRailcarTripService
{
    private readonly HttpClient _httpClient;
    private const string BaseEndpoint = "api/railcar-trips";

    public RailcarTripService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TripDto>> GetTripsAsync()
    {
        try
        {
            var trips = await _httpClient.GetFromJsonAsync<List<TripDto>>(BaseEndpoint);
            return trips ?? new List<TripDto>();
        }
        catch (HttpRequestException ex)
        {
            // TODO: Implement proper logging
            Console.WriteLine($"Error fetching trips: {ex.Message}");
            throw;
        }
    }

    public async Task<List<EquipmentEventDto>> GetTripEventsAsync(int tripId)
    {
        try
        {
            var events = await _httpClient.GetFromJsonAsync<List<EquipmentEventDto>>($"{BaseEndpoint}/{tripId}/events");
            return events ?? new List<EquipmentEventDto>();
        }
        catch (HttpRequestException ex)
        {
            // TODO: Implement proper logging
            Console.WriteLine($"Error fetching trip events: {ex.Message}");
            throw;
        }
    }

    public async Task<ImportResultDto> ImportEventsAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"{BaseEndpoint}/import", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ImportResultDto>();
            return result ?? new ImportResultDto { Success = false, Errors = new List<string> { "Invalid response from server" } };
        }
        catch (HttpRequestException ex)
        {
            // TODO: Implement proper logging
            Console.WriteLine($"Error importing events: {ex.Message}");
            return new ImportResultDto 
            { 
                Success = false, 
                Errors = new List<string> { $"Import failed: {ex.Message}" }
            };
        }
    }
}
