namespace Melissa.Core.AiTools.Localization;

public sealed class AllUNeedApiOptions
{
    public string BaseAddress { get; set; }
    public string ApiKey { get; set; }
    
    private static AllUNeedApiOptions? _instance;
    
    public static AllUNeedApiOptions GetInstance()
    {
        if (_instance == null)
        {
            _instance = new AllUNeedApiOptions();
        }
        
        return _instance;
    }
    
    private AllUNeedApiOptions(){}

    public bool IsConfigured() => !string.IsNullOrWhiteSpace(BaseAddress) && !string.IsNullOrWhiteSpace(ApiKey);
}