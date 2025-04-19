using System.Globalization;
using System.Text;
using CsvHelper;
using HtmlAgilityPack;
using Melissa.Core.AiTools.Holidays;

namespace Melissa.Core.ExternalData.Holidays;

public class HolidayScraper
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://www.feriados.com.br";
    private readonly int _year = 2025;
    
    public HolidayScraper()
    {
        _httpClient = new HttpClient();
    }

    public async Task ScrapeAllHolidaysToCsvAsync(string outputFilePath)
    {
        var allHolidays = new List<Holiday>();
        
        var nationalHolidays = await GetNationalHolidaysAsync();
        allHolidays.AddRange(nationalHolidays);
        
        Console.WriteLine($"Found {nationalHolidays.Count} national holidays");

        var stateLinks = await GetStateLinksAsync();
        
        foreach (var stateLink in stateLinks)
        {
            var stateCode = ExtractStateCodeFromUrl(stateLink);
            if (string.IsNullOrEmpty(stateCode)) continue;
            
            Console.WriteLine($"Processing state: {stateCode}");
            
            var stateHolidays = await GetStateHolidaysAsync(stateLink, stateCode);
            allHolidays.AddRange(stateHolidays);
            
            Console.WriteLine($"Found {stateHolidays.Count} holidays for state {stateCode}");
            
            var cityLinks = await GetCityLinksForStateAsync(stateLink);
            
            foreach (var cityLink in cityLinks)
            {
                var cityName = ExtractCityNameFromUrl(cityLink);
                if (string.IsNullOrEmpty(cityName)) continue;
                
                Console.WriteLine($"Processing city: {cityName} in state {stateCode}");
                
                var cityHolidays = await GetCityHolidaysAsync(cityLink, stateCode, cityName);
                allHolidays.AddRange(cityHolidays);
                
                Console.WriteLine($"Found {cityHolidays.Count} holidays for city {cityName}");
                
                // Evita overloading do servidor
                await Task.Delay(500);
            }
            
            // Evita overloading do servidor
            await Task.Delay(1000);
        }
        
        WriteHolidaysToCsv(allHolidays, outputFilePath);
        
        Console.WriteLine($"Total holidays found: {allHolidays.Count}");
        Console.WriteLine($"CSV file saved to: {outputFilePath}");
    }
    
    private async Task<List<Holiday>> GetNationalHolidaysAsync()
    {
        var url = $"{_baseUrl}/{_year}";
        var holidays = new List<Holiday>();
        
        var html = await _httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var feriadosLista = doc.DocumentNode.SelectNodes("//div[@id='Feriados  2025']/ul/li/div/span");
        
        if (feriadosLista == null) return holidays;
        
        foreach (var feriado in feriadosLista)
        {
            var text = feriado.InnerText.Trim();
            if (string.IsNullOrEmpty(text)) continue;
            
            var parts = text.Split('-', 2);
            if (parts.Length != 2) continue;
            
            var date = parts[0].Trim();
            var description = parts[1].Trim();
            
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(description))
            {
                var isOptional = feriado.GetAttributeValue("class", "").Contains("facultativos");
                
                holidays.Add(new Holiday
                {
                    Date = ParseDate(date),
                    Description = description,
                    Type = HolidayType.National,
                    IsOptional = isOptional
                });
            }
        }
        
        return holidays;
    }
    
    private async Task<List<string>> GetStateLinksAsync()
    {
        var url = $"{_baseUrl}/{_year}";
        var stateLinks = new List<string>();
        
        var html = await _httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var stateNodes = doc.DocumentNode.SelectNodes("//div[contains(@title, 'Feriados ')]/a");
        
        if (stateNodes == null) return stateLinks;
        
        foreach (var node in stateNodes)
        {
            var link = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(link) && link.Contains("feriados-estado"))
            {
                stateLinks.Add(link);
            }
        }
        
        return stateLinks;
    }
    
    private async Task<List<Holiday>> GetStateHolidaysAsync(string stateUrl, string stateCode)
    {
        var fullUrl = $"{_baseUrl}{stateUrl}?ano={_year}";
        var holidays = new List<Holiday>();
        
        var html = await _httpClient.GetStringAsync(fullUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var feriadosLista = doc.DocumentNode.SelectNodes("//div[contains(@id, 'Feriados')]/ul/li/div/span");
        
        if (feriadosLista == null) return holidays;
        
        foreach (var feriado in feriadosLista)
        {
            var text = feriado.InnerText.Trim();
            if (string.IsNullOrEmpty(text)) continue;
            
            var parts = text.Split('-', 2);
            if (parts.Length != 2) continue;
            
            var date = parts[0].Trim();
            var description = parts[1].Trim();
            
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(description))
            {
                // Pula os feriados Nacionais
                if (description.Trim().Equals("Tiradentes", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Dia do Trabalho", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Independência do Brasil", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Nossa Senhora Aparecida", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Dia de Finados", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Proclamação da República", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Natal", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Ano Novo", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Carnaval", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Sexta-Feira Santa", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Corpus Christi", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Consciência Negra", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var isOptional = feriado.GetAttributeValue("class", "").Contains("facultativos");
                
                holidays.Add(new Holiday
                {
                    Date = ParseDate(date),
                    Description = description,
                    Type = HolidayType.State,
                    State = stateCode,
                    IsOptional = isOptional
                });
            }
        }
        
        return holidays;
    }
    
    private async Task<List<string>> GetCityLinksForStateAsync(string stateUrl)
    {
        var fullUrl = $"{_baseUrl}{stateUrl}?ano={_year}";
        var cityLinks = new List<string>();
        
        var html = await _httpClient.GetStringAsync(fullUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var cityNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'feriados-') and not(contains(@href, 'feriados-estado'))]");
        
        if (cityNodes == null) return cityLinks;
        
        foreach (var node in cityNodes)
        {
            var link = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(link) && !link.Contains("feriados-estado") && link.Contains("https://www.feriados.com.br/feriados-"))
            {
                cityLinks.Add(link);
            }
        }
        
        return cityLinks;
    }
    
    private async Task<List<Holiday>> GetCityHolidaysAsync(string cityUrl, string stateCode, string cityName)
    {
        var fullUrl = $"{cityUrl}?ano={_year}";
        var holidays = new List<Holiday>();
        
        var html = await _httpClient.GetStringAsync(fullUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var feriadosLista = doc.DocumentNode.SelectNodes("//div[contains(@id, 'Feriados')]/ul/li/div/span");
        
        if (feriadosLista == null) return holidays;
        
        foreach (var feriado in feriadosLista)
        {
            var text = feriado.InnerText.Trim();
            if (string.IsNullOrEmpty(text)) continue;
            
            var parts = text.Split('-', 2);
            if (parts.Length != 2) continue;
            
            var date = parts[0].Trim();
            var description = parts[1].Trim();
            
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(description))
            {
                if (description.Trim().Equals("Tiradentes", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Dia do Trabalho", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Independência do Brasil", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Nossa Senhora Aparecida", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Dia de Finados", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Proclamação da República", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Natal", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Ano Novo", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Carnaval", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Sexta-Feira Santa", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Corpus Christi", StringComparison.OrdinalIgnoreCase) ||
                    description.Trim().Equals("Consciência Negra", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var isOptional = feriado.GetAttributeValue("class", "").Contains("facultativos");
                
                holidays.Add(new Holiday
                {
                    Date = ParseDate(date),
                    Description = description,
                    Type = HolidayType.City,
                    State = stateCode,
                    City = cityName,
                    IsOptional = isOptional
                });
            }
        }
        
        return holidays;
    }
    
    private string ExtractStateCodeFromUrl(string url)
    {
        var parts = url.Split('-');
        if (parts.Length >= 4)
        {
            var stateCodeWithPhp = parts[parts.Length - 1];
            return stateCodeWithPhp.Replace(".php", "").ToUpper();
        }
        return string.Empty;
    }
    
    private string ExtractCityNameFromUrl(string url)
    {
        var parts = url.Split('-');
        if (parts.Length >= 3)
        {
            var cityName = parts[1];
            return cityName.Replace("_", " ");
        }
        return string.Empty;
    }
    
    private static DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out DateTime result))
        {
            return result;
        }
        
        throw new FormatException($"Unable to parse date: {dateStr}");
    }
    
    private static void WriteHolidaysToCsv(List<Holiday> holidays, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        csv.WriteRecords(holidays);
    }
}

// Exemplo de uso:
// var scraper = new HolidayScraper();
// await scraper.ScrapeAllHolidaysToCSVAsync("holidays_2025.csv");