using System.Net;
using System.Text.Json;
using Polly;

namespace Melissa.Core.AiTools.Localization;

public class LocalizationService
{
    private readonly AllUNeedApiOptions _allUNeedApiOptions = AllUNeedApiOptions.GetInstance();

    public async Task<CityInfoDto?> GetCityInfo(string cityName, bool searchBySimilarName, int radiusKm = 0)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            throw new ArgumentException("O nome da cidade não pode ser nulo ou vazio.", nameof(cityName));

        if (!_allUNeedApiOptions.IsConfigured())
            throw new InvalidOperationException(
                "As opções da API AllUNeed não estão configuradas. Por favor, defina o BaseAddress e o ApiKey.");

        var policy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var url = $"city?" +
                  $"city_name={Uri.EscapeDataString(cityName)}" +
                  $"&search_by_similar_name={searchBySimilarName}" +
                  $"&radius_km={radiusKm}";

        var response = await policy.ExecuteAsync(async () =>
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri(_allUNeedApiOptions.BaseAddress);
            client.DefaultRequestHeaders.Add("X-API-KEY", _allUNeedApiOptions.ApiKey);
            return await client.GetAsync(url);
        });

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null; // Cidade não encontrada
        
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Erro ao buscar informações da cidade: {response.ReasonPhrase}");

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CityInfoDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Falha ao desserializar informações da cidade.");
    }
}