using Melissa.Core.Infraestructure.Holidays;
using OllamaSharp;

namespace Melissa.Core.AiTools.Holidays;

public class HolidayOllamaTool
{
    /// <summary>
    /// Consulta os feriados sejam eles nacionais, estaduais ou municipais dado um específico mês.
    /// </summary>
    /// <param name="city">Cidade</param>
    /// <param name="state">Estado</param>
    /// <param name="month">Mês</param>
    [OllamaTool]
    public static async Task<List<Holiday>> GetHolidays(string city, string state, string month)
    {
        var service = new HolidayService();
        return await service.GetHolidaysAsync(city, state, month);
    }
}