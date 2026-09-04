using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using Xunit;

namespace STranslate.Tests;

public class SmartTextPreprocessorTests
{
    [Fact]
    public void RepairPdfLineBreaks_MergesHyphenatedWords()
    {
        var input = "This is an inter-\r\nnational conference about ma-\r\nchine learning.";
        var expected = "This is an international conference about machine learning.";
        var actual = SmartTextPreprocessor.RepairPdfLineBreaks(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RepairPdfLineBreaks_PreservesParagraphsAndListItems()
    {
        var input = "Paragraph one line one\r\nline two.\r\n\r\n- Item 1\r\n- Item 2\r\n\r\nParagraph two.";
        var actual = SmartTextPreprocessor.RepairPdfLineBreaks(input);

        Assert.Contains("- Item 1", actual);
        Assert.Contains("- Item 2", actual);
        Assert.Contains("Paragraph one line one line two.", actual);
        Assert.Contains("Paragraph two.", actual);
    }

    [Fact]
    public void ProtectFormatMarkers_ProtectsMarkdownAndLatex()
    {
        var input = "Here is a formula $E=mc^2$ and code `var x = 1;` and block:\n```csharp\nConsole.WriteLine(\"Hello\");\n```\nDone.";
        var (protectedText, placeholders) = SmartTextPreprocessor.ProtectFormatMarkers(input);

        Assert.DoesNotContain("$E=mc^2$", protectedText);
        Assert.DoesNotContain("`var x = 1;`", protectedText);
        Assert.DoesNotContain("Console.WriteLine(\"Hello\");", protectedText);
        Assert.True(placeholders.Count >= 3);

        // Simulate translation preserving placeholders
        var restored = SmartTextPreprocessor.RestoreFormatMarkers(protectedText, placeholders);
        Assert.Equal(input, restored);
    }

    [Fact]
    public void SplitIdentifier_SplitsCamelAndSnakeCase()
    {
        Assert.Equal("get user info", TextHelper.SplitIdentifier("getUserInfo").ToLowerInvariant());
        Assert.Equal("xml parser", TextHelper.SplitIdentifier("XMLParser").ToLowerInvariant());
        Assert.Equal("get user profile by id", TextHelper.SplitIdentifier("get_user_profile_by_id").ToLowerInvariant());
        Assert.Equal("utf 8 encoding", TextHelper.SplitIdentifier("utf8Encoding").ToLowerInvariant());
    }

    [Fact]
    public void BuildGlossaryInstructions_GeneratesPromptInstructions()
    {
        var glossary = new Dictionary<string, string>
        {
            ["Large Language Model"] = "大语言模型",
            ["Chain of Thought"] = "思维链"
        };

        var prompt = SmartTextPreprocessor.BuildGlossaryInstructions(glossary);
        Assert.Contains("大语言模型", prompt);
        Assert.Contains("思维链", prompt);
    }

    [Fact]
    public void VocabularyExportHelper_ExportsAnkiTsv()
    {
        var history = new HistoryModel
        {
            Id = 1,
            Time = DateTime.Now,
            SourceLang = "en",
            TargetLang = "zh",
            SourceText = "resilience",
            Data =
            [
                new HistoryData
                {
                    ServiceDisplayName = "TestDict",
                    DictResult = new DictionaryResult
                    {
                        ResultType = DictionaryResultType.Success,
                        Text = "resilience",
                        DictMeans = [new DictMean { PartOfSpeech = "n.", Means = ["恢复力", "韧性"] }],
                        Sentences = ["She showed great resilience."]
                    }
                }
            ]
        };

        var ankiTsv = VocabularyExportHelper.ExportToAnkiTsv([history]);
        Assert.Contains("resilience", ankiTsv);
        Assert.Contains("恢复力", ankiTsv);
        Assert.Contains("STranslate", ankiTsv);
    }
}
