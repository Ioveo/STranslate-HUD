using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace STranslate.Plugin;

/// <summary>
/// Prompt
/// </summary>
public partial class Prompt : ObservableObject
{
    /// <summary>
    /// 名称
    /// </summary>
    [ObservableProperty] public partial string Name { get; set; }
    
    /// <summary>
    /// Items
    /// </summary>
    public ObservableCollection<PromptItem> Items { get; set; } = [];
    
    /// <summary>
    /// 是否启用
    /// </summary>
    [ObservableProperty] public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Prompt
    /// </summary>
    public Prompt()
    {
        Name = "New Prompt";
        IsEnabled = false;
    }

    /// <summary>
    /// Prompt
    /// </summary>
    /// <param name="name"></param>
    /// <param name="prompts"></param>
    /// <param name="isEnabled"></param>
    public Prompt(string name, IEnumerable<PromptItem> prompts, bool isEnabled = false)
    {
        Name = name;
        IsEnabled = isEnabled;
        foreach (var prompt in prompts)
        {
            Items.Add(prompt.Clone());
        }
    }
    
    /// <summary>
    /// 克隆
    /// </summary>
    /// <returns></returns>
    public Prompt Clone()
    {
        return new Prompt(Name, Items.Select(p => p.Clone()), IsEnabled);
    }

    /// <summary>
    /// 获取全场景高精默认 Prompt 列表（包含通用翻译、学术论文、代码技术、母语润色、语法纠错、长文总结）
    /// </summary>
    public static List<Prompt> GetDefaultPrompts() =>
    [
        new("翻译",
        [
            new PromptItem("system", "You are a professional, authentic translation engine. You only return the translated text directly, without any explanations, notes, greetings, or prefixes."),
            new PromptItem("user", "Please accurately translate the following text into $target (maintain original markdown format, code syntax, and LaTeX formulas):\r\n\r\n$content"),
        ], true),
        new("学术精译",
        [
            new PromptItem("system", "You are an expert academic translator specializing in scientific papers. Translate with rigorous academic terminology, authentic journal writing style, accurate passive phrasing, and professional scholarly tone. Output only the translated text directly."),
            new PromptItem("user", "Please translate the following academic content into $target using professional scholarly phrasing:\r\n\r\n$content"),
        ]),
        new("代码与文档",
        [
            new PromptItem("system", "You are a specialized technical and programming documentation translator. Strictly preserve all code blocks, inline code, variable names, keywords, placeholders, and markdown formatting intact. Only translate human-readable explanations and comments. Return only the translated text."),
            new PromptItem("user", "Please translate the following technical documentation or code comments into $target while preserving all code syntax and identifiers:\r\n\r\n$content"),
        ]),
        new("润色",
        [
            new PromptItem("system", "You are a native-level language editor and proofreader. Polish and rewrite the text to be authentic, natural, idiomatic, clear, and elegant in $source, while strictly preserving the original core meaning. Return only the polished text."),
            new PromptItem("user", "Please polish the following text to make it sound fluent and natural:\r\n\r\n$content"),
        ]),
        new("语法纠错与精析",
        [
            new PromptItem("system", "You are a language teacher and grammar expert. Identify and correct all spelling and grammatical issues in the text, provide the corrected version, explain key grammar rules and reasons for corrections, and highlight key vocabulary nuances."),
            new PromptItem("user", "Please analyze, correct grammar and spelling, and explain the following text in $target:\r\n\r\n$content"),
        ]),
        new("总结",
        [
            new PromptItem("system", "You are an executive summarization assistant. Extract the core arguments, critical facts, and key takeaways into concise, structured bullet points in $target. Avoid fluff and preamble."),
            new PromptItem("user", "Please summarize the main points of the following text into clear bullet points in $target:\r\n\r\n$content"),
        ]),
    ];
}

/// <summary>
/// PromptItem
/// </summary>
public partial class PromptItem : ObservableObject
{
    /// <summary>
    /// 角色
    /// </summary>
    [ObservableProperty]
    [JsonPropertyName("role")]
    public partial string Role { get; set; } = "";

    /// <summary>
    /// 内容
    /// </summary>
    [ObservableProperty]
    [JsonPropertyName("content")]
    public partial string Content { get; set; } = "";

    /// <summary>
    /// PromptItem
    /// </summary>
    public PromptItem() { }

    /// <summary>
    /// PromptItem
    /// </summary>
    /// <param name="role"></param>
    public PromptItem(string role)
    {
        Role = role;
        Content = "";
    }

    /// <summary>
    /// PromptItem
    /// </summary>
    /// <param name="role"></param>
    /// <param name="content"></param>
    public PromptItem(string role, string content)
    {
        Role = role;
        Content = content;
    }

    /// <summary>
    /// 克隆
    /// </summary>
    /// <returns></returns>
    public PromptItem Clone()
    {
        return new PromptItem(Role, Content);
    }
}