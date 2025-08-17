using System.Globalization;
using OllamaSharp;
using Serilog;

namespace Melissa.Core.AiTools.Time;

public class TimeOllamaTools
{
    /// <summary>
    /// Retorna a data e hora atual no formato brasileiro.
    /// </summary>
    /// <returns></returns>
    [OllamaTool]
    public static string GetCurrentDateTime()
    {
        Log.Information("Executando a ferramenta GetCurrentDateTime");
        var res = DateTime.Now.ToString("F", new CultureInfo("pt-BR"));
        Log.Information("Data e hora atual retornada: {CurrentDateTime}", res); 
        return res;
    }
}