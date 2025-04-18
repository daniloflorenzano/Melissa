namespace Melissa.Core.AiTools.Weather.External;

public class GoogleWeatherService : IWeatherService
{
    public Task<WeatherResponse> GetCurrentWeatherAsync(string city)
    {
        throw new NotImplementedException();
    }
}