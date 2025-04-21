using HtmlAgilityPack;
using Melissa.Core.AiTools.Weather;

namespace DefaultNamespace;

public class WeatherService
{
    private const string UrlBase = "www.google.com.br/search?q=clima";

    /// <summary>
    /// Retorna as informações completas de previsão do tempo de acordo com a localização e dia.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="period"></param>
    /// <returns></returns>
    public async Task<Weather> GetWeatherAsync(string location, string? period)
    {
        if (!string.IsNullOrEmpty(period))
        {
            //ToDo: ajustar a requisição passando o dia especificado
        }
        
        location = location.Trim().Replace(" ", "+");
        var urlRequisicao = $"{UrlBase}+{location}";

        HttpClient httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(urlRequisicao);

        string content = await response.Content.ReadAsStringAsync();
        
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(content);

        #region documentNode

        var maxTemperature = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='gNCp2e']//span[@class='wob_t']")!.InnerText;
        var minTemperature = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='QrNVmd ZXCv8e']//span[@class='wob_t']")!.InnerText;
        var cityState = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='BBwThe']")!.InnerText;
        var dayOfWeek = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='Z1VzSb']")!.InnerText;
        var chuva = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_pp']")!.InnerText;
        var umidade = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_hm']")!.InnerText;
        var vento = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[last()]")!.InnerText;

        #endregion
        
        string[] cityStateArr = cityState.Split(',');

        var weather = new Weather()
        {
            TemperaturaMax = maxTemperature,
            TemperaturaMin = minTemperature,
            Cidade = cityStateArr[0].Trim(),
            Estado = cityStateArr[1].Trim(),
            DiaSemana = dayOfWeek,
            Chuva = chuva,
            Umidade = umidade,
            Vento = vento
        };

        return weather;
    }
}