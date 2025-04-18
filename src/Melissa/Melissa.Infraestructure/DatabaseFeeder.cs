using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Melissa.Infraestructure.Holidays;

namespace Melissa.Infraestructure;

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

            context.Holidays.AddRange(holidays);
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