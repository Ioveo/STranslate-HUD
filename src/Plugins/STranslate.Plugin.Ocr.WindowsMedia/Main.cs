using STranslate.Plugin;
using STranslate.Plugin.Ocr.WindowsMedia.View;
using STranslate.Plugin.Ocr.WindowsMedia.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace STranslate.Plugin.Ocr.WindowsMedia;

public class Main : IOcrPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public IEnumerable<LangEnum> SupportedLanguages =>
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
        LangEnum.PortugueseBrazil,
    ];

    public bool SupportBoxPoints() => true;

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    public async Task<OcrResult> RecognizeAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var ocrResult = new OcrResult();

        try
        {
            if (request.ImageData == null || request.ImageData.Length == 0)
                return ocrResult.Fail("图片数据为空");

            var engine = CreateOcrEngine(request.Language);
            if (engine == null)
            {
                return ocrResult.Fail("系统未找到可用的 Windows OCR 引擎，请确认 Windows 已安装相应语言包。");
            }

            using var memoryStream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(memoryStream))
            {
                writer.WriteBytes(request.ImageData);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }

            memoryStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(memoryStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            if (cancellationToken.IsCancellationRequested)
                return ocrResult.Fail("识别已取消");

            var winResult = await engine.RecognizeAsync(softwareBitmap);
            if (winResult == null)
                return ocrResult.Fail("Windows OCR 返回空结果");

            foreach (var line in winResult.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Text))
                    continue;

                var content = new OcrContent { Text = line.Text };

                if (line.Words.Count > 0)
                {
                    var minX = (float)line.Words.Min(w => w.BoundingRect.X);
                    var minY = (float)line.Words.Min(w => w.BoundingRect.Y);
                    var maxX = (float)line.Words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
                    var maxY = (float)line.Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);

                    content.BoxPoints.Add(new BoxPoint(minX, minY));
                    content.BoxPoints.Add(new BoxPoint(maxX, minY));
                    content.BoxPoints.Add(new BoxPoint(maxX, maxY));
                    content.BoxPoints.Add(new BoxPoint(minX, maxY));
                }

                ocrResult.OcrContents.Add(content);
            }

            stopwatch.Stop();
            ocrResult.Duration = stopwatch.Elapsed;
            ocrResult.IsSuccess = true;
            return ocrResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ocrResult.Duration = stopwatch.Elapsed;
            return ocrResult.Fail($"Windows OCR 识别异常: {ex.Message}");
        }
    }

    private static OcrEngine? CreateOcrEngine(LangEnum lang)
    {
        var bcp47 = MapToBcp47(lang);
        if (!string.IsNullOrEmpty(bcp47))
        {
            var winLang = new Language(bcp47);
            if (OcrEngine.IsLanguageSupported(winLang))
            {
                var engine = OcrEngine.TryCreateFromLanguage(winLang);
                if (engine != null) return engine;
            }

            // 尝试前缀匹配（例如 zh-Hans-CN 匹配 zh-CN 或 zh-Hans）
            var matched = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(l =>
                l.LanguageTag.StartsWith(bcp47[..2], StringComparison.OrdinalIgnoreCase) ||
                bcp47.StartsWith(l.LanguageTag[..2], StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                var engine = OcrEngine.TryCreateFromLanguage(matched);
                if (engine != null) return engine;
            }
        }

        // 默认尝试用户配置语言
        var profileEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (profileEngine != null) return profileEngine;

        // 最后回退到系统任意已安装 OCR 语言
        var fallbackLang = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();
        return fallbackLang != null ? OcrEngine.TryCreateFromLanguage(fallbackLang) : null;
    }

    private static string? MapToBcp47(LangEnum lang) => lang switch
    {
        LangEnum.ChineseSimplified => "zh-Hans-CN",
        LangEnum.ChineseTraditional => "zh-Hant-TW",
        LangEnum.English => "en-US",
        LangEnum.Japanese => "ja-JP",
        LangEnum.Korean => "ko-KR",
        LangEnum.French => "fr-FR",
        LangEnum.German => "de-DE",
        LangEnum.Spanish => "es-ES",
        LangEnum.Russian => "ru-RU",
        LangEnum.Italian => "it-IT",
        LangEnum.PortuguesePortugal => "pt-PT",
        LangEnum.PortugueseBrazil => "pt-BR",
        _ => null
    };
}
