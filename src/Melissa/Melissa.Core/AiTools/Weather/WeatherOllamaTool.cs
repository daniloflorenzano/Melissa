

using DefaultNamespace;

namespace Melissa.Core.AiTools.Weather;

public class WeatherOllamaTool
{
   public async Task GetCurrentWeatherByLocation()
   {
      var location = "Porto Real";
      var service = new WeatherService();

      var weather = await service.GetWeatherAsync(location, null);
   }
}