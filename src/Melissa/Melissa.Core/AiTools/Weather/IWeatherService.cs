namespace Melissa.Core.AiTools.Weather;

public interface IWeatherService
{
    public Task<WeatherResponse> GetCurrentWeatherAsync(string city);
}