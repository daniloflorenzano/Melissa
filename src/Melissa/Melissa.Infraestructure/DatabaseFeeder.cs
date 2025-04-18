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
            
            Parallel.ForEach(holidays, holiday =>
            {
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