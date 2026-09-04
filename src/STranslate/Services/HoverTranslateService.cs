using Microsoft.Extensions.Logging;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.Views;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Windows.Win32;

namespace STranslate.Services;

/// <summary>
/// 鼠标悬停雷达探测翻译服务：
/// 监听鼠标悬停静止手势，优先探测 UI Automation，若为自绘/网页则微区域离线 OCR 兜底，无需截屏或复制，即指即翻。
/// </summary>
public sealed partial class HoverTranslateService : IDisposable
{
    private readonly TranslateService _translateService;
    private readonly OcrService _ocrService;
    private readonly Settings _settings;
    private readonly ILogger<HoverTranslateService> _logger;
    private readonly DispatcherTimer _pollTimer;
    private readonly Stopwatch _idleStopwatch = new();
    private readonly ConcurrentDictionary<string, string> _translationCache = new(StringComparer.OrdinalIgnoreCase);

    private HoverTranslateWindow? _hoverWindow;
    private System.Drawing.Point _lastPoint;
    private System.Drawing.Point _triggeredPoint;
    private string _lastProcessedText = string.Empty;
    private bool _hasProcessedCurrentPosition;
    private bool _isRunning;
    private bool _disposed;

    private const int IdleThresholdMs = 350;
    private const int MoveDistanceThreshold = 8;
    private const int DismissDistanceThreshold = 60;

    public bool IsRunning => _isRunning;

    public HoverTranslateService(
        TranslateService translateService,
        OcrService ocrService,
        Settings settings,
        ILogger<HoverTranslateService> logger)
    {
        _translateService = translateService;
        _ocrService = ocrService;
        _settings = settings;
        _logger = logger;

        _pollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(80)
        };
        _pollTimer.Tick += OnPollTimerTick;
    }

    public void Start()
    {
        if (_isRunning) return;

        App.Current.Dispatcher.Invoke(() =>
        {
            _hoverWindow ??= new HoverTranslateWindow();
            if (PInvoke.GetCursorPos(out var cur))
            {
                _triggeredPoint = new System.Drawing.Point(cur.X, cur.Y);
                _hoverWindow.ShowToast("🎯 智能鼠标雷达已开启", "悬停在任意英文上即可即指即翻", _triggeredPoint, 1800);
            }
        });

        _isRunning = true;
        _idleStopwatch.Restart();
        _pollTimer.Start();
        _logger.LogInformation("Hover Translate Radar started.");
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _pollTimer.Stop();
        _idleStopwatch.Reset();

        App.Current.Dispatcher.Invoke(() =>
        {
            if (PInvoke.GetCursorPos(out var cur))
            {
                _triggeredPoint = new System.Drawing.Point(cur.X, cur.Y);
                _hoverWindow?.ShowToast("⏸ 智能鼠标雷达已关闭", "已退出悬停探测", _triggeredPoint, 1200);
            }
            else
            {
                _hoverWindow?.HideWindow();
            }
        });

        _logger.LogInformation("Hover Translate Radar stopped.");
    }

    public bool Toggle()
    {
        if (IsRunning)
        {
            Stop();
            return false;
        }
        else
        {
            Start();
            return true;
        }
    }

    private async void OnPollTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning || _disposed) return;

        if (!PInvoke.GetCursorPos(out var cur))
            return;

        var currentPoint = new System.Drawing.Point(cur.X, cur.Y);
        var distance = Math.Sqrt(Math.Pow(currentPoint.X - _lastPoint.X, 2) + Math.Pow(currentPoint.Y - _lastPoint.Y, 2));

        if (distance > MoveDistanceThreshold)
        {
            _lastPoint = currentPoint;
            _idleStopwatch.Restart();
            _hasProcessedCurrentPosition = false;

            // 若鼠标远离了触发点，自动隐藏窗口
            if (_hoverWindow != null && _hoverWindow.Visibility == System.Windows.Visibility.Visible)
            {
                var dismissDistance = Math.Sqrt(Math.Pow(currentPoint.X - _triggeredPoint.X, 2) + Math.Pow(currentPoint.Y - _triggeredPoint.Y, 2));
                if (dismissDistance > DismissDistanceThreshold)
                {
                    _hoverWindow.HideWindow();
                }
            }
            return;
        }

        // 鼠标已静止足够时长
        if (_idleStopwatch.ElapsedMilliseconds >= IdleThresholdMs && !_hasProcessedCurrentPosition)
        {
            _hasProcessedCurrentPosition = true;
            _triggeredPoint = currentPoint;
            await ProcessHoverAtPointAsync(currentPoint);
        }
    }

    private async Task ProcessHoverAtPointAsync(System.Drawing.Point point)
    {
        try
        {
            // 阶段 1：使用 UI Automation 极速探测光标所在控件文本 (0ms 零开销)
            var elementInfo = UiAutomationHelper.GetElementTextUnderPoint(point);
            var rawText = elementInfo?.Text;

            // 阶段 2：若 UIA 未能获取（如网页Canvas/自绘GUI/游戏/PDF），微区域快速局部 OCR 兜底
            if (string.IsNullOrWhiteSpace(rawText))
            {
                rawText = await CaptureAndOcrMicroRegionAsync(point);
            }

            if (string.IsNullOrWhiteSpace(rawText))
                return;

            var cleanedText = rawText.Trim();

            // 过滤：需包含至少两个英文字母，且非之前刚翻译过的相同文本
            if (!ContainsForeignLanguage(cleanedText))
                return;

            if (string.Equals(cleanedText, _lastProcessedText, StringComparison.OrdinalIgnoreCase) &&
                _hoverWindow?.Visibility == System.Windows.Visibility.Visible)
            {
                return;
            }

            _lastProcessedText = cleanedText;

            // 检查本地瞬时缓存
            if (_translationCache.TryGetValue(cleanedText, out var cachedTranslation))
            {
                ShowTranslation(cleanedText, cachedTranslation, point);
                return;
            }

            // 阶段 3：请求翻译服务（带自动内置降级兜底）
            var translator = _translateService.GetActiveOrFallbackTranslator();
            if (translator == null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _hoverWindow?.ShowToast("提示", "未找到可用的翻译服务，请在设置中配置", point, 2000);
                });
                return;
            }

            var sourceLang = _settings.SourceLang;
            var targetLang = LanguageDetector.GetTargetLanguage(LangEnum.English, _settings.TargetLang);
            if (targetLang == LangEnum.Auto)
            {
                targetLang = LangEnum.ChineseSimplified;
            }

            var result = await Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var request = new TranslateRequest(cleanedText, sourceLang, targetLang);
                var transResult = new TranslateResult();
                await translator.TranslateAsync(request, transResult, cts.Token);
                return transResult;
            });

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Text))
            {
                _translationCache[cleanedText] = result.Text;
                ShowTranslation(cleanedText, result.Text, point);
            }
            else
            {
                _logger.LogWarning("Hover translate failed for text '{Text}': {Error}", cleanedText, result.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to process hover translate at ({X}, {Y})", point.X, point.Y);
        }
    }

    private async Task<string?> CaptureAndOcrMicroRegionAsync(System.Drawing.Point point)
    {
        try
        {
            int w = 180, h = 48;
            using var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(point.X - w / 2, point.Y - h / 2, 0, 0, new System.Drawing.Size(w, h));
            }

            var ocr = _ocrService.GetActiveOrFallbackOcr();
            if (ocr == null) return null;

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var ocrResult = await ocr.RecognizeAsync(new OcrRequest(bytes, LangEnum.Auto), cts.Token);
            return ocrResult.Text;
        }
        catch
        {
            return null;
        }
    }

    private void ShowTranslation(string source, string translated, System.Drawing.Point point)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (!_isRunning) return;
            _hoverWindow?.ShowResult(source, translated, point);
        });
    }

    [GeneratedRegex(@"[a-zA-Z]{2,}")]
    private static partial Regex ForeignWordRegex();

    private static bool ContainsForeignLanguage(string text)
    {
        return ForeignWordRegex().IsMatch(text);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        App.Current.Dispatcher.Invoke(() =>
        {
            _hoverWindow?.Close();
            _hoverWindow = null;
        });
    }
}
