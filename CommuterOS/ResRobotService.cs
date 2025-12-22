using System.Text.Json;
using CommuterOS.Models;

namespace CommuterOS.Services;

public class ResRobotService
{
    private const string ApiKey = "2fb1a3d6-c1dd-4cbe-a320-acc796c0e643"; 
    
    private const string BaseUrl = "https://api.resrobot.se/v2.1";

    private const string SITE_SIGMA = "740066762";    
    private const string SITE_NORTULL = "740046132";
    private readonly HttpClient _client;

    public ResRobotService()
    {
        _client = new HttpClient();
    }

    public async Task<Trip?> GetNextTripAsync(string originId, string destId, bool isComfort)
    {
        try
        {
            string url = $"{BaseUrl}/trip?format=json&originId={originId}&destId={destId}&passlist=0&accessId={ApiKey}";
            
            var response = await _client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ResRobotTripResponse>(json);

                var selectedTrip = data.Trips.First();

                if (isComfort)
                {
                    var customTrip = GetCustomizedTrip(data.Trips, destId);
                    
                    return customTrip ?? selectedTrip;
                }
                else 
                {
                    return selectedTrip;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API ERROR: {ex.Message}");
        }
        return null;
    }

    //customization for me
    private Trip GetCustomizedTrip(List<Trip> tripList, string destId)
    {

            // going to work
            // i want Bus 515 from Odenplan -> Norrtull
            if (destId == SITE_NORTULL) 
            {
                
                var morningComfortTrip = tripList.FirstOrDefault(t => 
                    {
                        var legs = t.LegList.Legs;

                        bool hasHomeBus = legs.Any(leg =>
                            leg.Name.Contains("Buss") &&
                            leg.Origin.Name.Contains("Sigma") &&
                            leg.Destination.Name.Contains("Upplands Väsby"));

                        bool has515 = legs.Any(leg => 
                            leg.Name.Contains("515") && 
                            leg.Destination.Name.Contains("Norrtull"));

                        return hasHomeBus && has515;

                    });
                    if (morningComfortTrip != null) return morningComfortTrip;
            }

            // Going home from work 
            // I want: 
            // Bus 515 from Sveaplan -> Odenplan
            // and a Bus from Upplands Väsby Station -> Home
            else if (destId == SITE_SIGMA)
            {
                var eveningComfortTrip = tripList.FirstOrDefault(t =>
                {
                    var legs = t.LegList.Legs;

                    bool has515 = legs.Any(leg => 
                        leg.Name.Contains("515") && 
                        leg.Origin.Name.Contains("Sveaplan"));

                    bool hasHomeBus = legs.Any(leg => 
                        leg.Name.Contains("Buss") && 
                        leg.Origin.Name.Contains("Upplands Väsby") &&
                        leg.Destination.Name.Contains("Sigma")); 

                    return has515 && hasHomeBus;
                });
                if (eveningComfortTrip != null) return eveningComfortTrip;
            }
            return null;
    }
    
}
