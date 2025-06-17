using CsvHelper.Configuration.Attributes;
using DefaultNamespace;
using OllamaSharp;

namespace Melissa.Core.AiTools.Weather;

public class WeatherOllamaTool
{
    /// <summary>
    /// Retorna a temperatura atual de uma determinada cidade.
    /// </summary>
    /// <param name="location">Nome da cidade para o qual se deseja consultar o clima. Exemplo: "Porto Real".</param>
    [OllamaTool]
    public static async Task<string> GetCurrentTemperatureByLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "Não foi possível identificar a cidade/estado desejado. Por favor, tente novamente.";
        
        var service = new WeatherService();
        var weatherReturn = await service.GetWeatherAsync(location);
            
        return $"A Temperatura atual em {weatherReturn.Cidade} é de: {weatherReturn.TemperaturaAtual}°C.";
    }
}