using Melissa.Core.AiTools.Weather.External;
using OllamaSharp;

namespace Melissa.Core.AiTools.Weather;

public class WeatherOllamaTool
{
    /// <summary>
    /// Consulta o atual clima de uma cidade.
    /// </summary>
    /// <param name="city">Nome da cidade</param>
    [OllamaTool]
    public static async Task<WeatherResponse> GetWeather(string city)
    {
        var weatherApi = new GoogleWeatherService();
        return await weatherApi.GetCurrentWeatherAsync(city);
    }
}