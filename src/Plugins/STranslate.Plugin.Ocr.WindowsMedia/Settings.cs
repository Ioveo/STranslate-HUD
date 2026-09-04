using STranslate.Plugin;

namespace STranslate.Plugin.Ocr.WindowsMedia;

public class Settings
{
    public LangEnum Language { get; set; } = LangEnum.Auto;
    public bool AutoMergeLines { get; set; } = true;
}
