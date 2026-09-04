using CommunityToolkit.Mvvm.ComponentModel;
using STranslate.Plugin;
using System.ComponentModel;
using Windows.Media.Ocr;

namespace STranslate.Plugin.Ocr.WindowsMedia.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    [ObservableProperty] public partial LangEnum Language { get; set; }

    [ObservableProperty] public partial bool AutoMergeLines { get; set; }

    [ObservableProperty] public partial string InstalledLanguagesInfo { get; set; } = string.Empty;

    public List<LangEnum> LanguageOptions { get; } =
    [
        LangEnum.Auto,
        LangEnum.ChineseSimplified,
        LangEnum.ChineseTraditional,
        LangEnum.English,
        LangEnum.Japanese,
        LangEnum.Korean,
        LangEnum.French,
        LangEnum.German,
        LangEnum.Spanish,
        LangEnum.Russian,
        LangEnum.Italian,
        LangEnum.PortuguesePortugal,
    ];

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        Language = settings.Language;
        AutoMergeLines = settings.AutoMergeLines;

        RefreshInstalledLanguages();
        PropertyChanged += OnSettingsPropertyChanged;
    }

    private void RefreshInstalledLanguages()
    {
        try
        {
            var langs = OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
            InstalledLanguagesInfo = langs.Count > 0
                ? string.Join(", ", langs)
                : "当前系统未检测到 OCR 语言包，可在 Windows 系统“设置 -> 时间和语言 -> 语言”中添加支持。";
        }
        catch
        {
            InstalledLanguagesInfo = "Windows 10/11 系统原生 OCR";
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Language):
                _settings.Language = Language;
                break;
            case nameof(AutoMergeLines):
                _settings.AutoMergeLines = AutoMergeLines;
                break;
            default:
                return;
        }
        _context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= OnSettingsPropertyChanged;
}
