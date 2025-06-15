using CsvHelper.Configuration.Attributes;
using DefaultNamespace;
using OllamaSharp;

namespace Melissa.Core.AiTools.Weather;

public class WeatherOllamaTool
{
    /// <summary>
    /// Retorna a previsão do tempo para uma cidade ou estado em uma data específica, informando a temperatura máxima e mínima.
    /// </summary>
    /// <param name="location">Nome da cidade ou estado para o qual se deseja consultar a previsão do tempo.</param>
    /// <param name="date">(opcional): Data alvo da previsão, no formato dd/mm/yyyy. Também pode ser uma expressão como "ontem", "amanhã", "depois de amanhã", ou "daqui a x dias".</param>
    [OllamaTool]
    public static async Task<string> GetWeatherByLocation(string location, [Optional] string date)
    {
        var currentSystemDate = DateTime.Now.Date;
        var targetDate = currentSystemDate; // Inicia como dia atual
        
        if (string.IsNullOrWhiteSpace(location))
            return "Não foi possível identificar a cidade/estado desejado. Por favor, tente novamente.";
        
        // Caso for um input do tipo: "Daqui a X dias"
        if (date.Contains("dias", StringComparison.CurrentCultureIgnoreCase))
        {
            var splitDate = date.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            
            var numberIndex = Array.FindIndex(splitDate, s => int.TryParse(s, out _));
            if (numberIndex >= 0 && int.TryParse(splitDate[numberIndex], out int daysToAdd))
            {
                targetDate = currentSystemDate.AddDays(daysToAdd);
            }
            else
            {
                return "Não foi possível entender quantos dias adicionar. Exemplo válido: 'daqui a 3 dias'.";
            }
        }
        else if (!string.IsNullOrWhiteSpace(date))
        {
            if (date.StartsWith("amanh", StringComparison.CurrentCultureIgnoreCase))
                targetDate = currentSystemDate.AddDays(1);
            
            else if (date.Contains("depois de aman", StringComparison.CurrentCultureIgnoreCase))
                targetDate = currentSystemDate.AddDays(2);
            
            // Se não for nenhuma das opções acima, tenta formatar a data passada por parâmetro.
            else
            {
                try
                {
                    targetDate = DateTime.ParseExact(date, "dd/MM/yyyy", null);
                    
                    // Caso a data for menor do que a do dia atual
                    if (targetDate.Date < currentSystemDate.Date || date.Contains("ontem", StringComparison.CurrentCultureIgnoreCase))
                        return "A data especificada é anterior ao dia atual. Escolha uma data válida para saber a previsão do tempo.";
                }
                catch
                {
                    return "Não foi possível identificar a data desejada. Por favor, tente usando um formato válido de dia, mês e ano.";
                } 
            }
        }
        
        // Atualiza o parâmetro date para garantir que ele tenha o valor correto
        date = targetDate.ToString("dd/MM/yyyy");
        
        var service = new WeatherService();
        var weatherReturn = await service.GetWeatherAsync(location, date);

        if (currentSystemDate < targetDate)
        {
            var dias = (currentSystemDate - targetDate).Days;
            return $"A previsão para {dias} dias em {weatherReturn.Cidade} é: {weatherReturn.Tempo}. Temperaturas entre {weatherReturn.TemperaturaMin}°C e {weatherReturn.TemperaturaMax}°C.";
        }
            
        return $"A previsão do tempo atual em {weatherReturn.Cidade} é: {weatherReturn.Tempo}. Temperatura atual: {weatherReturn.TemperaturaAtual}°C. A temperatura máxima prevista é de {weatherReturn.TemperaturaMax}°C, e a mínima será {weatherReturn.TemperaturaMin}°C.";
    }
}