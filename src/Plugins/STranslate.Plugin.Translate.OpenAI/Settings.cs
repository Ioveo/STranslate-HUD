namespace STranslate.Plugin.Translate.OpenAI;

public class Settings
{
    public OpenAIApiMode ApiMode { get; set; } = OpenAIApiMode.ChatCompletions;
    public string ApiKey { get; set; } = string.Empty;
    public string Url { get; set; } = "https://api.openai.com/";
    public string Model { get; set; } = "deepseek-chat";
    public List<string> Models { get; set; } =
    [
        "deepseek-chat",
        "deepseek-reasoner",
        "gpt-4o",
        "gpt-4o-mini",
        "o3-mini",
        "claude-3-7-sonnet-20250219",
        "claude-3-5-sonnet-20241022",
        "gemini-2.5-flash",
        "gemini-2.5-pro",
        "qwen-plus",
        "qwen-max",
        "deepseek-ai/DeepSeek-V3",
        "deepseek-ai/DeepSeek-R1",
    ];
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
    public string AdditionalParametersJson { get; set; } = string.Empty;
    public int TopP { get; set; } = 1;
    public int N { get; set; } = 1;
    public bool Stream { get; set; } = true;
    public int? MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;

    public List<Prompt> Prompts { get; set; } = Prompt.GetDefaultPrompts();
}

public enum OpenAIApiMode
{
    ChatCompletions,
    Responses
}

