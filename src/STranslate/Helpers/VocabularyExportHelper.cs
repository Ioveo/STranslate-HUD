using STranslate.Core;
using STranslate.Plugin;
using System.IO;
using System.Text;

namespace STranslate.Helpers;

/// <summary>
/// 生词本与词汇导出助手：支持导出为 Anki 格式、Markdown 词汇卡片、CSV 等
/// </summary>
public static class VocabularyExportHelper
{
    private static readonly Encoding Utf8BomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// 导出为 Anki TSV/CSV 导入格式 (Front, Back, Note/Example, Tag)
    /// </summary>
    public static string ExportToAnkiTsv(IReadOnlyList<HistoryModel> items)
    {
        var sb = new StringBuilder();
        // Anki header
        sb.AppendLine("#separator:tab");
        sb.AppendLine("#html:true");
        sb.AppendLine("#tags column:4");

        foreach (var item in items)
        {
            var word = item.SourceText?.Trim().Replace("\t", " ").Replace("\n", "<br>") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(word))
                continue;

            var definition = new StringBuilder();
            var example = new StringBuilder();

            foreach (var data in item.Data)
            {
                if (data.DictResult is { ResultType: DictionaryResultType.Success } dict)
                {
                    if (dict.DictMeans.Count > 0)
                    {
                        foreach (var mean in dict.DictMeans)
                        {
                            var pos = mean.PartOfSpeech?.Trim();
                            var means = string.Join("；", mean.Means.Where(m => !string.IsNullOrWhiteSpace(m)));
                            definition.Append(!string.IsNullOrEmpty(pos) ? $"<b>{pos}</b> {means}<br>" : $"{means}<br>");
                        }
                    }
                    if (dict.Sentences.Count > 0)
                    {
                        foreach (var sentence in dict.Sentences.Take(3))
                        {
                            example.Append($"{sentence}<br>");
                        }
                    }
                }
                else if (data.TransResult is { IsSuccess: true } trans && !string.IsNullOrWhiteSpace(trans.Text))
                {
                    definition.Append($"{trans.Text.Replace("\n", "<br>")}<br>");
                }
            }

            var front = word;
            var back = definition.Length > 0 ? definition.ToString().TrimEnd('<', 'b', 'r', '>') : word;
            var notes = example.ToString().TrimEnd('<', 'b', 'r', '>');
            var tag = "STranslate";

            sb.AppendLine($"{front}\t{back}\t{notes}\t{tag}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 导出为 Markdown 词汇笔记卡片
    /// </summary>
    public static string ExportToMarkdown(IReadOnlyList<HistoryModel> items, string title = "STranslate 词汇本")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | 共 {items.Count} 条记录\n");

        foreach (var item in items)
        {
            var word = item.SourceText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(word))
                continue;

            sb.AppendLine($"## {word}");
            sb.AppendLine($"- **时间**: `{item.Time:yyyy-MM-dd HH:mm}`");
            sb.AppendLine($"- **语种**: `{item.SourceLang} -> {item.TargetLang}`");

            foreach (var data in item.Data)
            {
                var engine = !string.IsNullOrWhiteSpace(data.ServiceDisplayName) ? data.ServiceDisplayName : "Translation";
                if (data.DictResult is { ResultType: DictionaryResultType.Success } dict)
                {
                    sb.AppendLine($"\n### 📖 词典释义 ({engine})");
                    foreach (var mean in dict.DictMeans)
                    {
                        var pos = mean.PartOfSpeech?.Trim();
                        var means = string.Join("；", mean.Means.Where(m => !string.IsNullOrWhiteSpace(m)));
                        sb.AppendLine($"- {(!string.IsNullOrEmpty(pos) ? $"**{pos}** " : "")}{means}");
                    }
                    if (dict.Sentences.Count > 0)
                    {
                        sb.AppendLine("\n**例句:**");
                        foreach (var sentence in dict.Sentences.Take(3))
                        {
                            sb.AppendLine($"> {sentence}");
                        }
                    }
                }
                else if (data.TransResult is { IsSuccess: true } trans && !string.IsNullOrWhiteSpace(trans.Text))
                {
                    sb.AppendLine($"\n### 🌐 译文 ({engine})");
                    sb.AppendLine(trans.Text.Trim());
                }
            }

            sb.AppendLine("\n---\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 保存文本到文件（UTF-8 with BOM）
    /// </summary>
    public static async Task SaveToFileAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content, Utf8BomEncoding);
    }
}
