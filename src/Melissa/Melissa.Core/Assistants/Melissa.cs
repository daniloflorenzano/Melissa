using Melissa.Core.AiTools.Weather;
using Melissa.Core.Chats;

namespace Melissa.Core.Assistants;

public class Melissa : Assistant
{
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        chatBuilder
            .WithModelName(ModelName.Llama32_1B)
            .WithAssistantName("Melissa")
            .WithPurposeDescription("""
                                    ser uma assistente pessoal inteligente chamada Melissa que pode responder perguntas gerais
                                     e acessar APIs externas.
                                    """)
            .WithAdicionalDescription("dê suas respostas de maneira curta e direta, com no máximo 140 caracteres.");
            //.WithTool(new GetWeatherTool());
        
        Chat = chatBuilder.Build().Result;
    }
    
    public bool HasInternetAccess { get; private set; }
    public bool IsUsingCryptography { get; private set; }
    
    /// <summary>
    /// Liga/desliga o acesso à internet.
    /// Caso desligada, a assistente não poderá acessar APIs externas.
    /// </summary>
    public void ToggleInternetAccess() => HasInternetAccess = !HasInternetAccess;
    
    /// <summary>
    /// Liga/desliga o uso de criptografia das mensagens.
    /// Se ligada, espera-se que o usuário também envie mensagens criptografadas.
    /// Funciona como uma camada extra de segurança.
    /// </summary>
    public void ToggleCryptography() => IsUsingCryptography = !IsUsingCryptography;
    
    public async Task ChangeModel(ModelName modelName)
    {
        await Chat.ChangeModel(modelName);
    }

    public override IAsyncEnumerable<string> Ask(Question question)
    {
        if (IsUsingCryptography)
        {
            // TODO: Implementar criptografia
            
            // decrypta a mensagem
            var decrypted = question.Text;
            var copy = question with { Text = decrypted };
            
            // encrypta a resposta
            return base.Ask(copy);
        }
        
        return base.Ask(question);
    }
}