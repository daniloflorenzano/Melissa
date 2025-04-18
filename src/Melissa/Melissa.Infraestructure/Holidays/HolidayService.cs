using Melissa.Core.AiTools.Holidays;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Infraestructure.Holidays;

public class HolidayService : IHolidayService
{
    private readonly AppDbContext _dbContext = new();

    public async Task<List<Holiday>> GetHolidaysAsync(string? city, string? state, string month)
    {
        var wantsNationalHoliday =
            string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(month);
        if (wantsNationalHoliday)
        {
            var monthNumber = GetMonth(month);
            return await _dbContext.Holidays
                .Where(h => h.Type == HolidayType.National && h.Date.Month == monthNumber)
                .ToListAsync();
        }

        var wantsStateHoliday =
            string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(month);
        if (wantsStateHoliday)
        {
            var monthNumber = GetMonth(month);
            return await _dbContext.Holidays
                .Where(h => h.Type == HolidayType.State && h.State == state && h.Date.Month == monthNumber)
                .ToListAsync();
        }

        var wantsCityHolidays = !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(month);
        if (wantsCityHolidays)
        {
            var monthNumber = GetMonth(month);
            if (string.IsNullOrEmpty(state))
            {
                return await _dbContext.Holidays
                    .Where(h => h.Type == HolidayType.City && h.City == city && h.Date.Month == monthNumber)
                    .ToListAsync();
            }

            return await _dbContext.Holidays
                .Where(h => h.Type == HolidayType.City && h.City == city && h.State == state &&
                            h.Date.Month == monthNumber)
                .ToListAsync();
        }

        return [];
    }

    public async Task<List<Holiday>> GetAllNationalHolidaysAsync() =>
        await _dbContext.Holidays.Where(h => h.Type == HolidayType.National).ToListAsync();
    
    public async Task<List<Holiday>> GetAllStateHolidaysAsync(string state) =>
        await _dbContext.Holidays.Where(h => h.Type == HolidayType.State && h.State == state).ToListAsync();
    
    public async Task<List<Holiday>> GetAllCityHolidaysAsync(string city) =>
        await _dbContext.Holidays.Where(h => h.Type == HolidayType.City && h.City == city).ToListAsync();

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
            _ => throw new ArgumentException("Mês inválido", nameof(month))
        };
    }
}