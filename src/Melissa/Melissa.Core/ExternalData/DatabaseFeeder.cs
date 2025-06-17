using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Melissa.Core.AiTools.Holidays;
using Melissa.WebServer;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.ExternalData;

public static class DatabaseFeeder
{
    public static async Task FeedHolidays(string filePath)
    {
        try
        {
            await using var context = new AppDbContext();
            await context.Database.EnsureCreatedAsync();

            // verifica se o banco ta vazio
            if (context.Holidays.Any())
                return;

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
            });

            csv.Context.RegisterClassMap<HolidayMap>();
            var holidays = csv.GetRecords<Holiday>().ToList();
            var nationals = holidays.Where(h => h.Type == HolidayType.National).ToList();
            var duplicatesToRemove = new List<Holiday>();

            Parallel.ForEach(holidays, holiday =>
            {
                // remove nacionais duplicados nas cidades
                if (holiday.Type is HolidayType.City or HolidayType.State
                    && nationals.Any(n => n.Date == holiday.Date && n.Description == holiday.Description))
                {
                    duplicatesToRemove.Add(holiday);
                    return;
                }

                if (string.IsNullOrEmpty(holiday.State))
                    return;

                holiday.State = holiday.State.ToUpper() switch
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
                    _ => holiday.State
                };
            });

            context.Holidays.AddRange(holidays.Except(duplicatesToRemove));
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(
                "Erro ao prepara tabela de Feriados. Verifique se o arquivo .csv esta disponível no diretório correto" +
                e);
            throw;
        }
    }

    public static async Task FeedHistoryData(DbConversationHistory conversationHistory)
    {
        await using var context = new AppDbContext();
        
        await context.Database.EnsureCreatedAsync();
        
        conversationHistory.Data = DateTime.Now;
        await context.AddAsync(conversationHistory);
        
        await context.SaveChangesAsync();
    }
    
    public static async Task<List<DbConversationHistory>> GetHistoryAsync()
    {
        await using var context = new AppDbContext();

        var historyList = await context.DbHistoryData
            .OrderByDescending(h => h.Data)
            .ToListAsync();

        return historyList;
    }
}

internal sealed class HolidayMap : ClassMap<Holiday>
{
    public HolidayMap()
    {
        Map(h => h.Date).Name("Date");
        Map(h => h.Description).Name("Description");
        Map(h => h.Type).Name("Type");
        Map(h => h.State).Name("State");
        Map(h => h.City).Name("City");
        Map(h => h.IsOptional).Name("IsOptional");
    }
}

