using System.Text.Json.Serialization;

namespace Melissa.Core.AiTools.Localization;

public record CityInfoDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("classification")]
    public string Classification { get; set; }
    
    [JsonPropertyName("population")]
    public long? Population { get; set; }
    
    [JsonPropertyName("state")]
    public string State { get; set; }
    
    [JsonPropertyName("country")]
    public string Country { get; set; }
    
    [JsonPropertyName("hospitals")]
    public List<string> Hospitals { get; set; } = new();
    
    [JsonPropertyName("schools")]
    public List<string> Schools { get; set; } = new();
    
    [JsonPropertyName("tourist_attractions")]
    public List<string> TouristAttractions { get; set; } = new();
    
    [JsonPropertyName("has_airport_or_train_station")]
    public bool HasAirportOrTrainStation { get; set; }
    
    [JsonPropertyName("has_public_transport")]
    public int StreetCount { get; set; }
    
    [JsonPropertyName("public_transport_point_count")]
    public int PublicTransportPointCount { get; set; }
}