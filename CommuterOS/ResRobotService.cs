using System.Text.Json;
using CommuterOS.Models;

namespace CommuterOS.Services;

public class ResRobotService
{
    private const string ApiKey = "2fb1a3d6-c1dd-4cbe-a320-acc796c0e643"; 
    
    private const string BaseUrl = "https://api.resrobot.se/v2.1";
    private readonly HttpClient _client;

    public ResRobotService()
    {
        _client = new HttpClient();
    }

    public async Task<Trip?> GetNextTripAsync(string originId, string destId)
    {
        try
        {
            string url = $"{BaseUrl}/trip?format=json&originId={originId}&destId={destId}&passlist=0&accessId={ApiKey}";
            
            var response = await _client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ResRobotTripResponse>(json);
                
                if (data?.Trips == null || data.Trips.Count == 0 ) return null;
                //515 bus
                // var busTrip = data.Trips.FirstOrDefault(t => 
                //     t.LegList.Legs.Any(leg => leg.Name.Contains("515")));

                return data.Trips.First();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API ERROR: {ex.Message}");
        }
        return null;
    }
}