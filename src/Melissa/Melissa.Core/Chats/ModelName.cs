using System.ComponentModel;

// ReSharper disable InconsistentNaming

namespace Melissa.Core.Chats;

public enum ModelName
{
    [Description("llama3.2:1b")]
    Llama32_1B,
    
    [Description("llama3.2:3b")]
    Llama32_3B,
    Llama31,
    Mistral
}
