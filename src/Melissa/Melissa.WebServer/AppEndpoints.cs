using DefaultNamespace;
using Melissa.Core.AiTools.Holidays;

namespace Melissa.WebServer;

public class AppEndpoints
{
    /// <summary>
    /// Retorna a temperatura atual de uma localização específica.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static async Task<string> GetCurrentWeatherByLocalizationAsync(string location)
    {
        var weatherService = new WeatherService();
        
        var weather = await weatherService.GetWeatherAsync(location);
        var tempAtual = weather.TemperaturaAtual;
        
        return tempAtual;
    }
    
    /// <summary>
    /// Exporta os feriados nacionais para um arquivo txt.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static async Task ExportNationalHolidaysToTxt()
    {
        var holidayService = new HolidayService();
        await holidayService.ExportNationalHolidaysToTxt();
    }
}