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
        httpClient.DefaultRequestHeaders.Add("Cookie", 
            "SEARCH_SAMESITE=CgQIjp0B; OTZ=8041070_68_64_73560_68_416340; SID=g.a000wAjxYzR7d6yhv_ng_H9Fph9B-hVu4sT5ShNQx500q6jcdMXE3Z7q90CMASStd5YHP2uWoQACgYKAbUSARQSFQHGX2MiIciseEmptjl--6-SIDY5wxoVAUF8yKrLcTeT7ePI5CjQVA05Ptjo0076; __Secure-1PSID=g.a000wAjxYzR7d6yhv_ng_H9Fph9B-hVu4sT5ShNQx500q6jcdMXE2ZuVIDZuz3XfPHvdhCMOzwACgYKAb4SARQSFQHGX2Miy30jQ46tKLn7gzGReNSo0RoVAUF8yKqbfRNBGcc12Jgrv4oP3vCX0076; __Secure-3PSID=g.a000wAjxYzR7d6yhv_ng_H9Fph9B-hVu4sT5ShNQx500q6jcdMXEV_lJmzojNxL0mBO3Gn4MUAACgYKARwSARQSFQHGX2Mi9Kvy1AvizQbPoklnCbiv2hoVAUF8yKpHhCeQiNbZ1sSPaX0IfZ7K0076; HSID=ARTOQiFWHbqKuTkTs; SSID=Aj3Vj0CzrLI52Mnfw; APISID=wU5Q9Fq73OBd3CW6/AgBlJhFUsUyhSu6S-; SAPISID=GZiICOCtBtFs4UXq/AxtW9TI5PsysgcUsZ; __Secure-1PAPISID=GZiICOCtBtFs4UXq/AxtW9TI5PsysgcUsZ; __Secure-3PAPISID=GZiICOCtBtFs4UXq/AxtW9TI5PsysgcUsZ; AEC=AVcja2eaiFoIQFdIza06vLnuZOfQm6n3dZavVNFeL8RpcWmRh0EZDxjnq_c; __Secure-1PSIDTS=sidts-CjEB7pHptVCc6ggo-6RtBzwzkN2mQ9tLcTlDW_YH6we5nR5_uFabuHP1TrS9iTkEaxY1EAA; __Secure-3PSIDTS=sidts-CjEB7pHptVCc6ggo-6RtBzwzkN2mQ9tLcTlDW_YH6we5nR5_uFabuHP1TrS9iTkEaxY1EAA; NID=523=ThKDRq18HqJ4ldYp-N1rybSI5FRIayjYXzUc8GOxMSiBHfROb32fWDlEsxtM8Kqjh2UXBmgX1Mr8P93T6QtEZcQyjW7Vc9VYB3M2XmwdU5fjfvV3qMp3I2xPVglrV-b9Zsdri27rJSEFjWuP2OX6d2nGjckaKPdq_VHsZz6KmmU3WBTJ0z5raMpNgmco5X_MvtA5oBsbKnHg4XxjB5goZkXHeWAj_5yvwbVSVnwYPBuuX46jacEutTNfMphPPlWcwWsp--h3sUdxkxq4e36qBCfKV7LG_LCBsDMpBeE_avBcxG7_9n1xYI6i-MSThyO_suyOd_lMAXcfwuRIo78hLFbl5tIE92DL1zkeNWR6CzMDxUYQMJ0R1C5p-vYVXYKvgleboygT2eRZxJ-4P1JfcEmsE4HgjOzyU8aBBQpINVNSdRPdXalLYrxPqek5HBN84sN13mz5wPEwENhlmU-XC2kU5WgVW586iA5PVVrRvWtvRkOvYPxzktG8jSK-LmDXoD2i_EWWdziP3xoBx9gD0PA-F2n-t1pc8fO0rCx7ka0ErxZx7qcC3Zx64xjMD-WIpM8LHJOkzvyMw4BA2O7YO05tN5C1c0bfkKIpK-OXqZWZrZFha8nM-fsYFdUkCZYlDfkKFc6J0xiDWWShlvECmxeR3aY8aireLIesDGdJ3TyQwA5qC6Sl0oj34yOm2E2c-kiGtBqAxqTxCU-0uZgwp0eeVUEu__nnxjhByc-Eixi-_2ypsfpuWn1pH-_NruI0W-IYwH4CZvznx_VpsRGx68gYpjJcL8wXIHAZc5NfdGHniHrU9M9iP2sVOBvgwik2-w5g1qv1tuifatfQ5V2CsJYahUhYUH92OiH1SMhx9o2tdIK1Izqfci3eDw5T8WLC9GzpxOWllTX1eCBvd19PWQJCKMhRmCRC6hzP5WnkpQ_u-DxYBn7CyBsIGvuKxM-ufRk05RLvIc8Y1J-8ioQG6Ycn3PP-x1l3_A0lOKQTTaR4PPLVa4NmE7ukO2gcCsAicNAk7xv2gwe6jgcEi7xI8mN8LgKYvoE3ZQiFoZNOALYmv6BmvFSnNIeOjktjUvVYyCi96Fxpmv_0TdfGZN7Q3xFDFBWo47Z-xqRHNp6awiSqBLzA4ynFnmmUUYTxKW2TyOPgKgYoGxkBlX715W3rhk-kB25bLEGbYQw3Zx5TIuVtyWRgMZG849DhLW9vUx_aOLonwlQsp7TDnBN41J2j94kRYiONZl4EWSO2LTpPEsw5jaa0N04Eyum5y5-CY4SamreXRkiyah8oirMlv9xOX4yZR_xapZj5xeuTzvRgBUl07qWBy3ZKgo00U8l4nsP9BZKB4uSWeAntgmS60eQLW9EoSlAF3fBYIK-kbOlgSF6zryHiNj86maSkmpuys07ZwsREmmTv1cacZy5VV7ztCcn5qpf1HfNOHXbEnqmAqw7-K48a96nAPZpA31YsmwP2PHVuO0eAvjU87bUtw-XGmSft3luN-OPv-SJxtzos-vmodHPXXWRjIzKzboa0uc23k8v6--9KRPxwDxgaMcYaK2vbUCUili6-WLu2b-SF5pRqW0Bw9Wz-rmeXwsVhG-4RNra4-SMrkNs9GD89Nalr3MsHoFDGC75tVk2EFI1fAQezw5s15ojFVJKwto0GZ4f7w8J_WU4XQ1S-ltCvIEObzXALbRFxx47x_d5Z3k9ItDEywVMm9f3mnKSv5Kq80IZKEzdNkcMLbvAQRXjsGCXWkCyQRBp5RxciMnPb5ZawZOdADtS-rXIXOujjn70VsFMcK-ZD0AqOx8PjkWnrxT9acmqW642t2Wp_0OufF2TvRfGF0vSi45S9_8lwBLLxulEieVURY_NXmec0cPGRWCQxNKGdBqtYenhUEzvnZ7scUhIRJUCiwL5qsRQqucjRzkMo15GxOzlkYsMKieirV2ql6ogYzzppBK1c0kND2cBrkr6p4KDz-_VUYjVD1XOqLfrJC6Q8CjIrgUq0QjwO38BYLUfsc881EZ23HLd-DDUj1QKqTFI8A9gnXGnfhDCOcgu_t8xGfsWwzDgN1kYXimF-oJMEBqH8UkMU1aZmRolZf7PFYXnWF1Jbaw5COPAIcsaQKIg6MztJFiB1NF6ZWL_TZxHKvlNxUY57N7Ya6GCcxNCj3NPkEUGm0t73UWYNHPE4qVxwAiqDhoJdbeevQZo-uRAK383ze5JHm-1N37uS3MkpioTRg7ppc3RYuJoI7lFEmxQg0momBmlyenXnQ9ghKawSXXdSTyVYni6I5X9rNWx0oUz8uEXn9_hyurmW97loadKGPg; SIDCC=AKEyXzWF_S8DzLY6kSXQYMZQ6B8MR7aXKhXj2TCQijB-_PsKCA0maCl7cYwr8STkMWkb0MKcZ-E; __Secure-1PSIDCC=AKEyXzV4mnN6qZt2clLAjhj6U_LiEkb5tJJrFluuRzpUah9NXIXT4LEQq06C8NN4TcXfGNw5tp0; __Secure-3PSIDCC=AKEyXzUAmBXyLkS7ceI_LS58HRs6NF4ZCdCejMCcdmCHu3xntnt_incQgUK8n7d94X2F7ABxJOk");


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
        
        string[] cityStateArr = cityState.Split(',');

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