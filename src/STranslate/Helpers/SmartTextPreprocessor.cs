using System.Text;
using System.Text.RegularExpressions;
using STranslate.Plugin;

namespace STranslate.Helpers;

/// <summary>
/// 智能文本预处理引擎：
/// 1. PDF 断行与连字符自动合并与段落重构
/// 2. 代码块、行内代码、LaTeX 公式与占位符保护与还原
/// 3. 代码标识符（驼峰、蛇形）智能拆分
/// 4. 专业术语词典注入与替换
/// </summary>
public static partial class SmartTextPreprocessor
{
    private const string PlaceholderPrefix = "⟦PH_";
    private const string PlaceholderSuffix = "⟧";

    #region PDF 断行与连字符智能修复

    /// <summary>
    /// 智能合并 PDF 复制文本的硬换行与连字符断词。
    /// 保持自然段落空行、列表项与代码块，自动合并句子中间被截断的断行与连字符单词。
    /// </summary>
    public static string RepairPdfLineBreaks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // 统一换行符
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // 按双换行（段落）分段处理
        var paragraphs = normalized.Split(new[] { "\n\n" }, StringSplitOptions.None);
        var processedParagraphs = new List<string>(paragraphs.Length);

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                processedParagraphs.Add(paragraph);
                continue;
            }

            var lines = paragraph.Split('\n');
            if (lines.Length <= 1)
            {
                // 单行直接处理连字符
                processedParagraphs.Add(FixHyphenatedWords(paragraph.Trim()));
                continue;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (sb.Length == 0)
                {
                    sb.Append(line);
                    continue;
                }

                // 检查是否为列表项（如 "- item", "* item", "1. item"），列表项强制换行
                if (IsListItem(line))
                {
                    sb.Append('\n').Append(line);
                    continue;
                }

                var prevText = sb.ToString();
                // 检查上一行末尾是否为连字符 (如 "trans-")
                if (prevText.EndsWith('-'))
                {
                    var trimmedPrev = prevText[..^1];
                    var firstWord = line.TrimStart();
                    // 如果连字符前后都是英文单词，则直接无缝拼接
                    if (Regex.IsMatch(trimmedPrev, @"[a-zA-Z]$") && Regex.IsMatch(firstWord, @"^[a-zA-Z]"))
                    {
                        sb.Length--; // 移除 '-'
                        sb.Append(firstWord);
                        continue;
                    }
                }

                // 判断是否需要空格连接（中文字符之间不加空格，英文/拉丁字符之间加空格）
                var lastChar = prevText[^1];
                var firstChar = line.TrimStart().FirstOrDefault();

                if (TextHelper.IsCjk(lastChar) && TextHelper.IsCjk(firstChar))
                {
                    sb.Append(line.TrimStart());
                }
                else
                {
                    sb.Append(' ').Append(line.TrimStart());
                }
            }

            processedParagraphs.Add(FixHyphenatedWords(sb.ToString().Trim()));
        }

        return string.Join("\n\n", processedParagraphs);
    }

    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart();
        return ListItemRegex().IsMatch(trimmed);
    }

    private static string FixHyphenatedWords(string text)
    {
        // 修复如 "inter- national" 或 "hyphen- \n ation" 形式的断词
        return HyphenSpaceWordRegex().Replace(text, "$1$2");
    }

    #endregion

    #region 代码与 LaTeX 公式标记保护引擎

    /// <summary>
    /// 保护文本中的代码块、行内代码、LaTeX 公式与占位符，替换为安全的占位符标识
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>保护后的文本和占位符映射字典</returns>
    public static (string ProtectedText, Dictionary<string, string> PlaceholderMap) ProtectFormatMarkers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (text, []);

        var placeholderMap = new Dictionary<string, string>();
        var counter = 0;

        string ReplaceWithPlaceholder(Match match)
        {
            var key = $"{PlaceholderPrefix}{counter++}{PlaceholderSuffix}";
            placeholderMap[key] = match.Value;
            return key;
        }

        var result = text;

        // 1. 保护 Markdown 代码块 (``` ... ```)
        result = FencedCodeBlockRegex().Replace(result, ReplaceWithPlaceholder);

        // 2. 保护 LaTeX 独立公式 ($$ ... $$)
        result = LatexBlockMathRegex().Replace(result, ReplaceWithPlaceholder);

        // 3. 保护 LaTeX 行内公式 ($ ... $)
        result = LatexInlineMathRegex().Replace(result, ReplaceWithPlaceholder);

        // 4. 保护行内代码 (` ... `)
        result = InlineCodeRegex().Replace(result, ReplaceWithPlaceholder);

        // 5. 保护 URL 链接 (http:// 或 https://)
        result = UrlRegex().Replace(result, ReplaceWithPlaceholder);

        // 6. 保护常见占位符 (如 %s, %d, {0}, {name}, %(key)s)
        result = PlaceholderFormatRegex().Replace(result, ReplaceWithPlaceholder);

        return (result, placeholderMap);
    }

    /// <summary>
    /// 将翻译结果中的占位符还原为原始的代码或公式标记
    /// </summary>
    public static string RestoreFormatMarkers(string translatedText, Dictionary<string, string> placeholderMap)
    {
        if (string.IsNullOrWhiteSpace(translatedText) || placeholderMap.Count == 0)
            return translatedText;

        var sb = new StringBuilder(translatedText);
        foreach (var (placeholder, original) in placeholderMap)
        {
            // 兼容可能被翻译引擎引入额外空格的占位符（如 ⟦ PH_0 ⟧ 或 ⟦PH_0⟧）
            sb.Replace(placeholder, original);
            var spacedPlaceholder = placeholder.Replace("_", " _ ");
            sb.Replace(spacedPlaceholder, original);
        }

        return sb.ToString();
    }

    #endregion

    #region 专业术语词典增强 (Glossary)

    /// <summary>
    /// 构建可注入 LLM System Prompt 的专业术语约束指令
    /// </summary>
    public static string BuildGlossaryInstructions(IReadOnlyDictionary<string, string> glossary)
    {
        if (glossary.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("\n[Glossary / Terminology Requirements]");
        sb.AppendLine("You MUST translate the following specific terms exactly as specified below:");
        foreach (var (term, translation) in glossary)
        {
            sb.AppendLine($"- \"{term}\" -> \"{translation}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 在文本中应用术语替换规则（支持前后匹配）
    /// </summary>
    public static string ApplyTerminologyRules(string text, IReadOnlyDictionary<string, string> glossary)
    {
        if (string.IsNullOrWhiteSpace(text) || glossary.Count == 0)
            return text;

        var result = text;
        foreach (var (sourceTerm, targetTerm) in glossary)
        {
            if (string.IsNullOrWhiteSpace(sourceTerm))
                continue;

            // 全字匹配（对于英文使用单词边界，对于中文直接替换）
            var pattern = Regex.IsMatch(sourceTerm, @"^[a-zA-Z0-9_\-]+$")
                ? $@"\b{Regex.Escape(sourceTerm)}\b"
                : Regex.Escape(sourceTerm);

            result = Regex.Replace(result, pattern, targetTerm, RegexOptions.IgnoreCase);
        }

        return result;
    }

    #endregion

    #region 正则表达式生成

    [GeneratedRegex(@"^(\d+\.|\*|\-|\+|\([a-zA-Z0-9]+\))\s+")]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"([a-zA-Z]+)-\s+([a-zA-Z]+)")]
    private static partial Regex HyphenSpaceWordRegex();

    [GeneratedRegex(@"```[\s\S]*?```")]
    private static partial Regex FencedCodeBlockRegex();

    [GeneratedRegex(@"\$\$[\s\S]*?\$\$")]
    private static partial Regex LatexBlockMathRegex();

    [GeneratedRegex(@"(?<!\$)\$([^\$\n]+?)\$(?!\$)")]
    private static partial Regex LatexInlineMathRegex();

    [GeneratedRegex(@"`[^`\n]+?`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"https?://[^\s<>\(\)""']+")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"(%[0-9]*\.?[0-9]*[sdfoxX]|\{[0-9a-zA-Z_]+\})")]
    private static partial Regex PlaceholderFormatRegex();

    #endregion
}
