using Microsoft.Extensions.Logging;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.Views;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace STranslate.Services;

/// <summary>
/// 窗口跟随式实时透明 HUD 贴面翻译服务：
/// 针对全英文目标窗口生成穿透式跟随浮层，自动抓取并原地覆盖中文，实现无需汉化包的原位翻译。
/// </summary>
public sealed class LiveHudTranslateService(
    TranslateService translateService,
    OcrService ocrService,
    Settings settings,
    ISnackbar snackbar,
    ILogger<LiveHudTranslateService> logger) : IDisposable
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private LiveHudOverlayWindow? _activeHudWindow;
    private nint _activeTargetHwnd;
    private bool _isTranslating;

    public bool IsActive => _activeHudWindow != null && _activeHudWindow.IsLoaded;

    /// <summary>
    /// 触发当前激活窗口的实时 HUD 贴面翻译（若已开启则关闭）
    /// </summary>
    public async Task ToggleHudForForegroundWindowAsync()
    {
        if (_isTranslating)
            return;

        var targetHwnd = Win32Helper.GetForegroundWindow();
        if (targetHwnd == 0)
        {
            snackbar.ShowWarning("未检测到有效的目标窗口");
            return;
        }

        // 如果当前已针对该窗口开启 HUD，则关闭退出
        if (_activeHudWindow != null && _activeTargetHwnd == targetHwnd)
        {
            CloseCurrentHud();
            snackbar.ShowSuccess("已关闭窗口贴面翻译");
            return;
        }

        CloseCurrentHud();
        _activeTargetHwnd = targetHwnd;
        _isTranslating = true;

        try
        {
            snackbar.ShowInfo("正在抓取窗口控件并分析翻译...");

            // 1. 获取目标窗口物理矩形
            if (!Win32Helper.GetTargetWindowBounds(targetHwnd, out var windowBounds))
            {
                snackbar.ShowWarning("无法获取目标窗口位置");
                return;
            }

            var winLeft = windowBounds.Left;
            var winTop = windowBounds.Top;
            var winWidth = windowBounds.Width;
            var winHeight = windowBounds.Height;

            if (winWidth <= 100 || winHeight <= 100)
            {
                snackbar.ShowWarning("目标窗口尺寸过小");
                return;
            }

            var dpiScale = Win32Helper.GetDpiScaleForPhysicalPoint(winLeft, winTop);

            // 2. 优先使用 UIA 提取目标窗口的可见文本及坐标
            var elements = await Task.Run(() => UiAutomationHelper.ExtractWindowElements(targetHwnd));
            
            // 2.1 若 UIA 树为空（针对自绘引擎/DirectX/Canvas等），自动采用全窗离线 OCR 提取坐标
            if (elements.Count == 0)
            {
                elements = await Task.Run(() => CaptureAndOcrWindowElementsAsync(targetHwnd, windowBounds));
            }

            if (elements.Count == 0)
            {
                snackbar.ShowWarning("未在目标窗口中提取到可翻译的英文控件文本");
                return;
            }

            // 3. 获取可用翻译引擎（具备内置免配置降级兜底）
            var translator = translateService.GetActiveOrFallbackTranslator();
            if (translator == null)
            {
                snackbar.ShowWarning("未配置可用的翻译引擎，请在设置中添加");
                return;
            }

            var hudItems = new List<HudOverlayItem>();
            var sourceLang = settings.SourceLang;
            var targetLang = LanguageDetector.GetTargetLanguage(LangEnum.English, settings.TargetLang);
            if (targetLang == LangEnum.Auto)
            {
                targetLang = LangEnum.ChineseSimplified;
            }

            await Task.Run(async () =>
            {
                var tasks = elements.Select(async elem =>
                {
                    var text = elem.Text.Trim();
                    if (_cache.TryGetValue(text, out var cached))
                    {
                        return (Element: elem, Translation: cached);
                    }

                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                        var req = new TranslateRequest(text, sourceLang, targetLang);
                        var transResult = new TranslateResult();
                        await translator.TranslateAsync(req, transResult, cts.Token);
                        if (transResult.IsSuccess && !string.IsNullOrWhiteSpace(transResult.Text))
                        {
                            _cache[text] = transResult.Text;
                            return (Element: elem, Translation: transResult.Text);
                        }
                    }
                    catch
                    {
                        // 忽略单个元素翻译失败
                    }

                    return (Element: elem, Translation: (string?)null);
                });

                var results = await Task.WhenAll(tasks);

                foreach (var item in results)
                {
                    if (string.IsNullOrWhiteSpace(item.Translation))
                        continue;

                    var elem = item.Element;
                    var trans = item.Translation;

                    // 计算相对于目标窗口左上角的逻辑 DIP 坐标
                    var relX = (elem.BoundingRectangle.Left - winLeft) / dpiScale.DpiScaleX;
                    var relY = (elem.BoundingRectangle.Top - winTop) / dpiScale.DpiScaleY;
                    var relW = elem.BoundingRectangle.Width / dpiScale.DpiScaleX;
                    var relH = elem.BoundingRectangle.Height / dpiScale.DpiScaleY;

                    var relativeRect = new Rect(relX, relY, relW, relH);

                    // 深色自适应背景 + 高亮白字
                    var bgColor = Color.FromArgb(220, 24, 24, 24);
                    var fgColor = Colors.White;

                    hudItems.Add(new HudOverlayItem(elem.Text, trans, relativeRect, bgColor, fgColor));
                }
            });

            if (hudItems.Count == 0)
            {
                snackbar.ShowWarning("未获取到有效的翻译结果");
                return;
            }

            // 4. 显示并挂载透明跟随窗口
            App.Current.Dispatcher.Invoke(() =>
            {
                _activeHudWindow = new LiveHudOverlayWindow();
                _activeHudWindow.Closed += (_, _) =>
                {
                    _activeHudWindow = null;
                    _activeTargetHwnd = 0;
                };

                _activeHudWindow.AttachToTarget(targetHwnd, hudItems);
            });

            snackbar.ShowSuccess($"已就绪！成功原位覆盖 {hudItems.Count} 处文本，按 Esc 退出");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Live HUD translation failed for window {Hwnd}", targetHwnd);
            snackbar.ShowError($"窗口贴面翻译失败: {ex.Message}");
        }
        finally
        {
            _isTranslating = false;
        }
    }

    private async Task<List<UiElementInfo>> CaptureAndOcrWindowElementsAsync(nint targetHwnd, System.Drawing.Rectangle windowBounds)
    {
        var result = new List<UiElementInfo>();
        try
        {
            using var bmp = new System.Drawing.Bitmap(windowBounds.Width, windowBounds.Height);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(windowBounds.Left, windowBounds.Top, 0, 0, new System.Drawing.Size(windowBounds.Width, windowBounds.Height));
            }

            var ocr = ocrService.GetActiveOrFallbackOcr();
            if (ocr == null) return result;

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var ocrResult = await ocr.RecognizeAsync(new OcrRequest(bytes, LangEnum.Auto, windowBounds.Width, windowBounds.Height), cts.Token);

            foreach (var region in ocrResult.Regions)
            {
                foreach (var para in region.Paragraphs)
                {
                    var text = string.Join(" ", para.Lines.Select(l => l.Text)).Trim();
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var points = para.BoxPoints.Count > 0
                        ? para.BoxPoints
                        : para.Lines.SelectMany(l => l.BoxPoints).ToList();

                    if (points.Count == 0) continue;

                    var minX = points.Min(p => p.X);
                    var minY = points.Min(p => p.Y);
                    var maxX = points.Max(p => p.X);
                    var maxY = points.Max(p => p.Y);

                    var absRect = new Rect(windowBounds.Left + minX, windowBounds.Top + minY, Math.Max(10, maxX - minX), Math.Max(10, maxY - minY));
                    result.Add(new UiElementInfo(text, absRect, "OcrTextBlock"));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Window OCR fallback extraction failed");
        }
        return result;
    }

    public void CloseCurrentHud()
    {
        if (_activeHudWindow != null)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _activeHudWindow.Close();
                _activeHudWindow = null;
                _activeTargetHwnd = 0;
            });
        }
    }

    public void Dispose()
    {
        CloseCurrentHud();
    }
}
