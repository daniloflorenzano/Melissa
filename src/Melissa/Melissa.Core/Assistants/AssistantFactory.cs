using Melissa.Core.Chats;
using Melissa.Core.Chats.Ollama;
using Serilog;

namespace Melissa.Core.Assistants;

/// <summary>
/// Fábrica para criação de Assistentes
/// </summary>
public class AssistantFactory
{
    private readonly IChatBuilder _builder = new OllamaChatBuilder();
    
    /// <summary>
    /// Cria Melissa. Possui sistema de retry em caso de erro. 
    /// </summary>
    /// <param name="timeBetweenRetries">Tempo entre as tentativas</param>
    /// <returns></returns>
    public async Task<Melissa> TryCreateMelissa(TimeSpan timeBetweenRetries)
    {
        var n = 1;
        
        while (true)
        {
            try
            {
                var melissa = new Melissa(_builder);
                Log.Information("Melissa iniciada");
                return melissa;
            }
            catch (Exception)
            {
                Log.Warning("Tentativa: {n}. Erro ao iniciar Melissa", n);
                n++;
                await Task.Delay(timeBetweenRetries);
            }
        }
    }
}