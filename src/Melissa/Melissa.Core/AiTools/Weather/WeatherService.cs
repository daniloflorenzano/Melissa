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

        #region Config

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
        //ToDo: Pegar o Cookie automaticamente
        httpClient.DefaultRequestHeaders.Add("Cookie", 
            "SEARCH_SAMESITE=CgQIjp0B; SID=g.a000wAjxY4wm3hwLyBkPTRJsSTyObvOuiUQwd0z_gJpyvc-3enlt3X0hOU6yBSqnPjvydHEopwACgYKATgSARQSFQHGX2MiYhkH7NJSFi3dp-bmOEwLNxoVAUF8yKoU_U8E_6Jv2_5VFZuu_wYe0076; __Secure-1PSID=g.a000wAjxY4wm3hwLyBkPTRJsSTyObvOuiUQwd0z_gJpyvc-3enltzbIuf-cx_ASwIi-FJnKeoQACgYKATMSARQSFQHGX2MiaI3TF5EwD3MZZw1pgcpEeRoVAUF8yKpvbmE64fn_sUnlsoVI-Til0076; __Secure-3PSID=g.a000wAjxY4wm3hwLyBkPTRJsSTyObvOuiUQwd0z_gJpyvc-3enlt6Pdhq5to2xMuALj6COMH0AACgYKAbsSARQSFQHGX2MisdYEoXpLoW6HhqpRUubMBhoVAUF8yKrnOxUTXv9Dsc-75Qrbma_k0076; HSID=AQqVZCGyD_AG39pfj; SSID=Ajj2OodcKaLvhbM23; APISID=aK4HY3cLEG1xBRQr/Aj3Rr_Q6YopFWamwB; SAPISID=RNxTo7TDnC7xuWru/AYgIyEbAPlAQ4d-ul; __Secure-1PAPISID=RNxTo7TDnC7xuWru/AYgIyEbAPlAQ4d-ul; __Secure-3PAPISID=RNxTo7TDnC7xuWru/AYgIyEbAPlAQ4d-ul; ADS_VISITOR_ID=00000000-0000-0000-0000-000000000000; OTZ=8055220_68_64_73560_68_416340; AEC=AVcja2dsTqML_Sy28zrxvpYF7XJD7JAP88thOdmdL1QPcQrMj9I5km3krvo; NID=523=E2S1IkZbJYpz9TvMOtGvDRXqNza0XQD2KYPrzWHhC1Ndz-kPSoLw5mAt430UNzVFa6M4B6_QuD01t9YDoj_TRwwh3ZpyBicVtYB4-ODnf1sFABAPlWPAnnKe2qbRkHoVCERvsNsIbwAwekwQAHjqkgDQMOJlH50yPpgDHqjiuPopkdaw_D5mJBGMXimuNILaXAkqHD-VbCNx6sVu0umhyYQxrrYRpyQZNfCtH3q17LX1HsLZnVSOmQQsBvt031aTf-ulfbsXYqcgQwR1pKtaOuWDfq95WqUPzKg_g1AMQTrV3TOBur4smf8AVH505Bc3trkcyaab1-vejZpnQIrIyBXLNkMZRqLCcIFVKJSYRpdhH_AX-o7osPSfroIpZ4HrIqYNSnIkO0vSL8OwkiHqwZTZzR0iVfpYDbyGeAc6on_S7CqAm7kga5mjE4zQFOSbBER56U9HU9VlZcXi1y5ySmXDhCuy4LyNci_snQZFHT_i1Um2caBreD4WdoRPrImSfkcpFduQLsvc82g88jZWB1UIpvpiRKTVZts4bVHFC6FiY4LYAz2JwtaQ0ysqF7o9wWfcxlHtmBUowwrL7mTLwIdkALM-kTUh-md_w65XABxKasFXJ5fMVBdUxJAwSRCJlD0Eq1lhj2oCVnhCMuzmaFii9het104BDlCWVdAZNJtdpoHZ4t_rjQnZGVmwh04xk7tIbKmnH3jx8fpFWSoN1P36rEi9fjLd1_lh5nEg2vwqmI-5vleVQmmR-BKpXd3CZkuza1NotfP32LfJz2hib6F4wK2IAOWbb1EkU0Vjw0danGYWE7sgaXCjEGS-xI6P2pWrvJEstDXKHq8l1lGeDsVsZ_Zv8F05yoEAF_c7f_VdU8vOFp6pT8snevtjY0MXYE513y5ZVh67akCDUCxsrZuUfg1KsfiTzp-f0ggbI0C_ega5itamyzaJlDYXq6g6QBGyn55zO1wqnrY2GVRSqNtSE5wsJXAdwYrlieW-dyxUf4klX88krmSwlioaEexGNQTdn8iWkICmOqm0LsTwkqZRV9SCS-rh8MvGKlBzVKC88bHIlacFhvR0ERQpRM9jAyUIbNnM8yTrYmTpz6kq1Renhq-gFQ6JW6_5mvZP3eaC67TxeWmmekCXLpzxO48c-1SwE8UkrMjQwQbPMaxr-gGqBmXOY8z5qzYHlAd1dcqVeqCCVuJdo_3v1URIcWfpNF7qwF5S-MkcLg8jpVapdnoZoeOJl4FmzYLPUUqpPghvEmrD78MEndDV0aFOmoauZvr4-qixUktPfIRZyk-G_ZAJ5dCXnR4RAZcM0ct9rwQj3cWaDzPkWC_6zw9DEhnipAXq-GsVoKb-eYQMEFtqs5HAqv4aeXDFBUUcb1_J3B3OudXiXedcWIn4PPcyRUcysgyO9E4rn9oLlQyojb8WBssIicccT2wQwOOPVkHA20krpn8AaLsy9hRNl8RqJ3kaLZcTd1y1JAOJfeMPAjLg4agpcJpOW95bu8TF3-itMVhcoBcc63lAr2v2eHZ17QQRjUslq5CJX3LQnzMN9sAaIvhhtKbGco4LnsVhZlbNOQgeAP5Bs-wKCMzOzPtn8Q2_967WQvkmXeTESekQcUn6wLQolOMNxNt0d1mwdVjq5m_O342kJmR6KnXSMfkhF0OCYs8cQEG_kTIYgBolns36LZL0FDF2HfA_TbzSyt2MbNQZDrrMwXVdNajDE-rWLzd5dnIMscQJFeghbBz-fcrmhCJh0h8iRIkVA7r3QQVXoHxDW9xu2h0CG1-WV9R_pwQjLSMez3GSwjzS91iabFfxdBulxkY9xzRFC8K-rYw5sz50d2TyMxCRjeGfVooOqKjxkb5-psdgepDKuPt70-VN0g26Sts82wwqmm7A04T3tlpPP7xhbeMgm99lS7FYeqKWLH9oOmLrgT79WQvGzsk-VqIl7D9oGZkOtlsv74aAeVmx5gMzXPVfTHxahTaRb_S3lvwnJFENzyBGS-D2hOzudWGfNvIBQxXr2ObozpkKhQt5c10Dh_n4x7mBHTVzVJhAoIekMU5te02i-gTL-gKkzpUIyORb2YGPa3SduV_Km3sn9DjgDaQgbZBYuRy6C2ul5aZj8_pVeD4rA_9IEggLiybnnYjkJPGeACMa1kJos2p7iefMeWD_XcbeWY4MeYou16Qx0AvrUBDk5sz0CxCt22a-pmE_6fRr8Too4uLJe8H-Yz6qxx_wpPH1WTodmn2iObgG7BfqzXSAGPZBcAslyOB1m7BYwaItIcrBipzghs3r0qwjwFDPsUsWsjKUY0equqBtCmjFYDrw; __Secure-1PSIDTS=sidts-CjEBjplskKLdgfXx9pODbyA16z0rxyHC1JBgYJWeZVnVJbx2N0qazazVrbL31mxbAW4TEAA; __Secure-3PSIDTS=sidts-CjEBjplskKLdgfXx9pODbyA16z0rxyHC1JBgYJWeZVnVJbx2N0qazazVrbL31mxbAW4TEAA; DV=0yWuf6g-u58jIEjdGkXHrXq9WenWZ1libWuSQyp6YwAAAAA; UULE=a+cm9sZTogMQpwcm9kdWNlcjogMTIKdGltZXN0YW1wOiAxNzQ1ODYxMTIxODI0MDAwCmxhdGxuZyB7CiAgbGF0aXR1ZGVfZTc6IC0yMjQ0NjIyNDEKICBsb25naXR1ZGVfZTc6IC00NDMwODA5MDEKfQpyYWRpdXM6IDkxNzYwCnByb3ZlbmFuY2U6IDYK; SIDCC=AKEyXzViAP4REi_M5D6_VZsTuCh55uBEiac3fY2DFMOnuPssQuKwFzsgryGUdB_7k3NpIyCFal4; __Secure-1PSIDCC=AKEyXzW0RYCc-RyGmrqQysVmo6-JGFB-Ackg3Lc_ysWIgrRKwU1SN8BFij5tJwYsPGlB8w8Bdb4; __Secure-3PSIDCC=AKEyXzWswpwlorHuU8kp_GShegOkFWpF5Kb-g4xt2JPG9Y6I75gW_E5M9LVXXGcd_Voe4dd1Elo");

        #endregion
        
        HttpResponseMessage response = await httpClient.GetAsync(urlRequisicao);
        string content = await response.Content.ReadAsStringAsync();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(content);

        #region DocumentNode

        var diaNode = htmlDoc.DocumentNode.SelectSingleNode($"//div[@class='Z1VzSb' and @aria-label='{diaDaSemana}']");
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
                tempNode = parentDiv.SelectSingleNode(".//div[@class='QrNVmd ZXCv8e']//span[contains(@class, 'wob_t')]");

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
        var cityState = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='BBwThe']")!.InnerText;
        var chuva = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_pp']")!.InnerText;
        var umidade = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@id='wob_hm']")!.InnerText;
        var vento = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wtsRwe']//span[@class='wob_t']")!.InnerText;
        var clima = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='wob_dcp']//span[@id='wob_dc']")!.InnerText;

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