using System.Text;
using OllamaSharp;

namespace Melissa.Core.AiTools.Holidays;

public class HolidayOllamaTools
{
    /// <summary>
    /// Retorna todos os feriados nacionais, estaduais ou municipais que ocorrem num determinado mês, considerando o estado e cidade fornecidos. O mês pode ser o nome ou o número.
    /// </summary>
    /// <param name="city">Nome da cidade</param>
    /// <param name="state">Sigla do estado</param>
    /// <param name="month">Nome ou número do mês</param>
    [OllamaTool]
    public static async Task<string> GetBrazilianHolidays(string city, string state = "", string month = "")
    {
        var service = new HolidayService();
        var holidays = await service.GetHolidaysAsync(city, state, month);
        if (holidays.Count == 0)
            return "Nenhum feriado encontrado.";

        var strBuilder = new StringBuilder();
        strBuilder.AppendLine("Feriados encontrados:");
        foreach (var holiday in holidays)
        {
            if (holiday.IsOptional)
                strBuilder.AppendLine($"(facultativo) {holiday.Date.ToString("dd/MM/yyyy")} - {holiday.Description}");
            else
                strBuilder.AppendLine($"{holiday.Date.ToString("dd/MM/yyyy")} - {holiday.Description}");
        }

        return strBuilder.ToString();
    }

    /// <summary>
    /// Retorna a(s) data(s) em que ocorre um feriado específico pelo nome.
    /// </summary>
    /// <param name="holidayName"></param>
    /// <returns></returns>
    [OllamaTool]
    public static async Task<string> GetHolidayDateByName(string holidayName)
    {
        var service = new HolidayService();
        var holiday = await service.SearchHolidayByNameAsync(holidayName);

        return holiday is null 
            ? "Nenhum feriado encontrado." 
            : holiday.Date.ToString("dd/MM/yyyy");
    }
}