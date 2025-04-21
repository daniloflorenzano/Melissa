using System.Globalization;
using OllamaSharp;

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
        return DateTime.Now.ToString("F", new CultureInfo("pt-BR"));
    }
}