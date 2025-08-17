using OllamaSharp;
using Serilog;

namespace Melissa.Core.AiTools.Localization;

public class LocalizationOllamaTool
{
    /// <summary>
    /// Retorna informações sobre uma cidade específica, incluindo classificação, população, estado, país, hospitais, escolas, atrações turísticas e se possui aeroporto ou estação de trem.
    /// </summary>
    /// <param name="cityName">Nome da cidade</param>
    /// <returns>Texto com informações sobre a cidade</returns>
    [OllamaTool]
    public static async Task<string> GetCityInfo(string cityName)
    {
        Log.Information("Executando a ferramenta GetCityInfo com o nome da cidade: {CityName}", cityName);
        
        if (string.IsNullOrWhiteSpace(cityName))
            return string.Empty;
        
        try
        {
            var service = new LocalizationService();
            var cityInfo = await service.GetCityInfo(cityName, searchBySimilarName: true, radiusKm: 20);
        
            if (cityInfo is null)
                return "Nenhuma informação encontrada para a cidade informada.";

            var res = $"Cidade: {cityInfo.Name}\n" +
                   $"Classificação: {cityInfo.Classification}\n" +
                   $"População: {cityInfo.Population}\n" +
                   $"Estado: {cityInfo.State}\n" +
                   $"País: {cityInfo.Country}\n" +
                   $"Hospitais: {string.Join(", ", cityInfo.Hospitals)}\n" +
                   $"Escolas: {string.Join(", ", cityInfo.Schools)}\n" +
                   $"Atrações Turísticas: {string.Join(", ", cityInfo.TouristAttractions)}\n" +
                   $"Possui Aeroporto ou Estação de Trem: {cityInfo.HasAirportOrTrainStation}";
            
            Log.Information("Informações da cidade obtidas com sucesso: {CityInfo}", res);
            return res;
        }
        catch (Exception e)
        {
            Log.Error(e, "Erro ao obter informações da cidade: {CityName}", cityName);
            return "Ocorreu um erro ao tentar obter informações sobre a cidade. Por favor, tente novamente mais tarde.";
        }
    }
}