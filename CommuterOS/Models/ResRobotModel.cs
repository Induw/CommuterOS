using System.Text.Json.Serialization;

namespace CommuterOS.Models;


public class ResRobotTripResponse
{
    [JsonPropertyName("Trip")]
    public List<Trip> Trips { get; set; } = [];
}
public class Trip
{
    [JsonPropertyName("LegList")]
    public required LegList LegList { get; set; }

    public DateTime GetDepartureTime()
    {
        if (LegList?.Legs == null || !LegList.Legs.Any()) return DateTime.MinValue;
        
        var firstLeg = LegList.Legs.First();
        if (DateTime.TryParse($"{firstLeg.Origin.Date} {firstLeg.Origin.Time}", out var timestamp))
        {
            return timestamp;
        }
        return DateTime.MinValue;
    }
}
public class LegList
{
    [JsonPropertyName("Leg")]
    public List<Leg> Legs { get; set; } = [];
}

public class Leg
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("Origin")]
    public required Point Origin { get; set; }

    [JsonPropertyName("Destination")]
    public required Point Destination { get; set; }
}

public class Point
{
    [JsonPropertyName("name")] 
    public  required string Name { get; set;}

    [JsonPropertyName("extId")]
    public required string Id { get;set;}

    [JsonPropertyName("time")]
    public required string Time { get; set;}

    [JsonPropertyName("date")]
    public  required string Date {get; set;}
}