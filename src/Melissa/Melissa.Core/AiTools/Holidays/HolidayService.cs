using Melissa.Core.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.AiTools.Holidays;

public class HolidayService
{
    private readonly AppDbContext _dbContext = new();

    public async Task<List<Holiday>> GetHolidaysAsync(string? city, string? state, string month)
    {
        city = city?.ToLower();
        if (city == "none") city = string.Empty;
        if (state == "none") state = string.Empty;
        
        var wantsEveryHoliday = string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state);

        if (wantsEveryHoliday)
        {
            var monthNumber = GetMonth(month);
            return await _dbContext.Holidays
                .Where(h => h.Date.Month == monthNumber)
                .ToListAsync();
        }

        if (!string.IsNullOrEmpty(state) && state.Length == 2) 
            state = GetStateByUf(state);

        var wantsStateHoliday =
            string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(month);
        if (wantsStateHoliday)
        {
            var monthNumber = GetMonth(month);
            return await _dbContext.Holidays
                .Where(h => h.State == state && h.Date.Month == monthNumber)
                .ToListAsync();
        }

        var wantsCityHolidays = !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(month);
        if (wantsCityHolidays)
        {
            var monthNumber = GetMonth(month);
            if (string.IsNullOrEmpty(state))
            {
                return await _dbContext.Holidays
                    .Where(h => h.City == city && h.Date.Month == monthNumber)
                    .ToListAsync();
            }

            return await _dbContext.Holidays
                .Where(h => h.City == city && h.State == state &&
                            h.Date.Month == monthNumber)
                .ToListAsync();
        }

        return [];
    }

    private static string GetStateByUf(string uf)
    {
        return uf.ToUpper() switch
        {
            "AC" => "Acre",
            "AL" => "Alagoas",
            "AP" => "Amapá",
            "AM" => "Amazonas",
            "BA" => "Bahia",
            "CE" => "Ceará",
            "DF" => "Distrito Federal",
            "ES" => "Espírito Santo",
            "GO" => "Goiás",
            "MA" => "Maranhão",
            "MT" => "Mato Grosso",
            "MS" => "Mato Grosso do Sul",
            "MG" => "Minas Gerais",
            "PA" => "Pará",
            "PB" => "Paraíba",
            "PR" => "Paraná",
            "PE" => "Pernambuco",
            "PI" => "Piauí",
            "RJ" => "Rio de Janeiro",
            "RN" => "Rio Grande do Norte",
            "RS" => "Rio Grande do Sul",
            "RO" => "Rondônia",
            "RR" => "Roraima",
            "SC" => "Santa Catarina",
            "SP" => "São Paulo",
            "SE" => "Sergipe",
            "TO" => "Tocantins",
            _ => uf
        };
    }

    private static int GetMonth(string month)
    {
        return month.ToLower() switch
        {
            "janeiro" => 1,
            "fevereiro" => 2,
            "março" => 3,
            "marco" => 3,
            "abril" => 4,
            "maio" => 5,
            "junho" => 6,
            "julho" => 7,
            "agosto" => 8,
            "setembro" => 9,
            "outubro" => 10,
            "novembro" => 11,
            "dezembro" => 12,
            
            
            "january" => 1,
            "february" => 2,
            "march" => 3,
            "april" => 4,
            "may" => 5,
            "june" => 6,
            "july" => 7,
            "august" => 8,
            "september" => 9,
            "october" => 10,
            "november" => 11,
            "december" => 12,

            // por padrao retorna o mes atual
            _ => DateTime.Now.Month
        };
    }
    
    public async Task<Holiday?> SearchHolidayByNameAsync(string name)
    {
        var holidays = _dbContext.Holidays.Select(h => h.Description).Distinct().ToList();

        var mostSimilar = holidays
            .Select(h => new
            {
                Name = h,
                Distance = Fastenshtein.Levenshtein.Distance(h.ToLower(), name.ToLower())
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        var holidayDescription = mostSimilar?.Name;
        if (string.IsNullOrEmpty(holidayDescription))
            return null;
        
        return await _dbContext.Holidays.FirstOrDefaultAsync(h => h.Description == holidayDescription);
    }
}