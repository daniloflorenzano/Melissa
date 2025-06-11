using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Melissa.Core.AiTools.Weather;

namespace DefaultNamespace;

public class WeatherService
{
    private const string UrlBase = "https://www.google.com.br/search?q=clima";

    /// <summary>
    /// Retorna as informações completas de previsão do tempo de acordo com a localização e dia.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="period"></param>
    /// <returns></returns>
    public async Task<Weather> GetWeatherAsync(string location, string period)
    {
        var dateStringToDateTime = DateTime.ParseExact(period, "dd/MM/yyyy", null);
        string diaDaSemana = dateStringToDateTime.ToString("dddd");

        location = location.Trim().Replace(" ", "+");
        period = period.Replace("/", "%2F");
        var urlRequisicao = $"{UrlBase}+{location}+{period}";

        #region FindSeiKey

        string docSei = "";
        using (var httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(urlRequisicao);
            string content = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            
            var linkNode = htmlDoc.DocumentNode
                .SelectSingleNode("//a[contains(@href, 'sei=')]");
            
            if (linkNode != null)
            {
                var href = linkNode.GetAttributeValue("href", "");
                
                var match = Regex.Match(href, @"sei=([^&""']+)");
                if (match.Success)
                    docSei = match.Groups[1].Value;
            }
        }

        #endregion
        
        using (var httpClient = new HttpClient())
        {
            urlRequisicao = $"{UrlBase}+{location}+{period}&sei={docSei}";
            string cookieHeader = await CookieHelper.GetCookiesAsync(urlRequisicao);
            
            if (!string.IsNullOrEmpty(cookieHeader))
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            
            httpClient.DefaultRequestHeaders.Add("Downlink", "10");
            httpClient.DefaultRequestHeaders.Add("Rtt", "50");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            HttpResponseMessage response = await httpClient.GetAsync(urlRequisicao);
            string content = await response.Content.ReadAsStringAsync();
                
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            #region DocumentNode

            // Se retornar null deu errado.
            var diaNode =
                htmlDoc.DocumentNode.SelectSingleNode($"//div[@class='Z1VzSb' and @aria-label='{diaDaSemana}']");
            
            if (diaNode is null)
                return new Weather();
            
            var maxTemperature = "";
            var minTemperature = "";

            if (diaNode != null)
            {
                var parentDiv = diaNode.ParentNode;

                // Dentro do mesmo 'pai', procura o span da temperatura
                var tempNode = parentDiv.SelectSingleNode(".//span[@class='wob_t' and @style='display:inline']");

                if (tempNode != null)
                {
                    maxTemperature = tempNode.InnerText;

                    // Ajusta o XPath para buscar a temperatura mínima.
                    tempNode = parentDiv.SelectSingleNode(
                        ".//div[@class='QrNVmd ZXCv8e']//span[contains(@class, 'wob_t')]");

                    if (tempNode != null)
                    {
                        minTemperature = tempNode.InnerText;
                    }
                    else
                    {
                        Console.WriteLine("Temperatura mínima não encontrada!");
                    }
                }
                else
                {
                    Console.WriteLine("Temperatura máxima não encontrada!");
                }
            }

            var currentTemperature = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='vk_bk TylWce SGNhVe']//span[@class='wob_t q8U8x']")!.InnerText;
            var cityState = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='eKPi4 BUSxSd']//span[@class='BBwThe']")!.InnerText;
            var chuva = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_pp']")!.InnerText;
            var umidade = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_hm']")!.InnerText;
            var vento = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_ws']")!.InnerText;
            var clima = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='VQF4g']//div[@class='wob_dcp']//span[@id='wob_dc']")!.InnerText;

            #endregion

            string[] cityStateArr = cityState.Contains("-") ? cityState.Split('-') : cityState.Split(',');

            var weather = new Weather()
            {
                TemperaturaMax = maxTemperature,
                TemperaturaMin = minTemperature,
                TemperaturaAtual = currentTemperature,
                Cidade = cityStateArr[0].Trim(),
                Estado = cityStateArr[1].Trim(),
                Chuva = chuva,
                Umidade = umidade,
                Vento = vento,
                Tempo = clima
            };

            return weather;
        }
    }
}