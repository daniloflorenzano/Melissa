using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Melissa.Core.AiTools.Weather;

namespace DefaultNamespace;

public class WeatherService
{

    /// <summary>
    /// Retorna as informações completas de previsão do tempo de acordo com a localização e dia.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="period"></param>
    /// <returns></returns>
    public async Task<Weather> GetWeatherAsync(string location)
    {
        var url = "https://www.climatempo.com.br/json/busca-por-nome";
        
        var idCity = await GetIdCity(location, url);
        
        location = location.Trim().Replace(" ", "").ToLower();
        
        var urlBase = $"https://www.climatempo.com.br/previsao-do-tempo/agora/cidade/{idCity}/{location}";
        
        using (var httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(urlBase);
            string content = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            
            #region DocumentNode
            
            var currentTemperature =
                htmlDoc.DocumentNode.SelectSingleNode("//div[@class='_flex _justify-center _align-center']//span[@class='-bold -gray-dark-2 -font-55 _margin-l-20 _center']")!
                    .InnerText
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Trim();

            var cityState =
                htmlDoc.DocumentNode.SelectSingleNode("//div[@class='_flex _align-center _gap-8 _justify-center _margin-b-20']//h1[@class='-bold -font-18 -dark-blue']")!
                    .InnerText
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Trim();

            // Remove o texto fixo "Tempo agora em " do começo da string
            // Dps a gnt melhora isso
            cityState = cityState.StartsWith("Tempo agora em") 
                ? cityState.Substring("Tempo agora em".Length).Trim() 
                : cityState;

            var umidade = htmlDoc.DocumentNode.SelectSingleNode("//li[@class='item']//div[@class='_flex']//p[@class='-gray _flex _align-center']//span[@class='-gray-light']")!
                .InnerText
                .Replace("\n", "")
                .Replace("\t", "")
                .Trim();

            #endregion

            string[] cityStateArr = cityState.Contains("-") ? cityState.Split('-') : cityState.Split(',');

            var weather = new Weather()
            {
                TemperaturaAtual = currentTemperature,
                Cidade = cityStateArr[0].Trim(),
                Estado = cityStateArr[1].Trim(),
                Umidade = umidade
            };

            return weather;
        }
    }

    private static async Task<string> GetIdCity(string location, string url)
    {
        using (var client = new HttpClient())
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", location)
            });

            try
            {
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("response", out var responseProp) &&
                        responseProp.TryGetProperty("data", out var dataArray) &&
                        dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("idcity", out var idcityProp) &&
                                idcityProp.ValueKind == JsonValueKind.Number)
                            {
                                return idcityProp.GetInt32().ToString();
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erro na requisição: {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Erro ao processar JSON: {e.Message}");
            }
        }

        return "";
    }
}