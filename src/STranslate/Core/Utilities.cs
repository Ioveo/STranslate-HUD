using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Plugin;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace STranslate.Core;

public class Utilities
{
    #region StringUtils

    public static string ToPascalCase(string text)
    {
        return ConvertCase(text, false);
    }

    public static string ToCamelCase(string text)
    {
        return ConvertCase(text, true);
    }

    public static string ToSnakeCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var words = text.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return string.Empty;

        // 预估容量：每个单词长度 + 下划线
        var capacity = words.Sum(w => w.Length) + words.Length - 1;
        var sb = new StringBuilder(capacity);

        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0)
                sb.Append('_');
            sb.Append(words[i].ToLower());
        }

        return sb.ToString();
    }

    private static string ConvertCase(string text, bool isCamelCase)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        try
        {
            var lines = text.Split([Environment.NewLine], StringSplitOptions.None);
            var processedLines = new string[lines.Length];

            for (var i = 0; i < lines.Length; i++)
            {
                var words = lines[i].Split([' '], StringSplitOptions.RemoveEmptyEntries);
                processedLines[i] = ConvertWords(words, isCamelCase);
            }

            return string.Join(Environment.NewLine, processedLines);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string ConvertWords(string[] words, bool isCamelCase)
    {
        if (words.Length == 0)
            return string.Empty;

        var capacity = words.Sum(w => w.Length);
        var sb = new StringBuilder(capacity);

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (string.IsNullOrEmpty(word))
                continue;

            // 第一个单词且是驼峰命名时，首字母小写
            if (i == 0 && isCamelCase)
            {
                sb.Append(char.ToLower(word[0]));
            }
            else
            {
                sb.Append(char.ToUpper(word[0]));
            }

            // 追加剩余字符（小写）
            if (word.Length > 1)
            {
                sb.Append(word[1..].ToLower());
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     自动识别语种
    /// </summary>
    /// <param name="text">输入语言</param>
    /// <param name="scale">英文占比</param>
    /// <returns>
    ///     Item1: SourceLang
    ///     Item2: TargetLang
    /// </returns>
    public static (LangEnum SourceLang, LangEnum TargetLang) AutomaticLanguageRecognition(string text, double scale = 0.8)
    {
        //1. 首先去除所有数字、标点及特殊符号
        //https://www.techiedelight.com/zh/strip-punctuations-from-a-string-in-csharp/
        text = Regex
            .Replace(text, "[1234567890!\"#$%&'()*+,-./:;<=>?@\\[\\]^_`{|}~，。、《》？；‘’：“”【】、{}|·！@#￥%……&*（）——+~\\\\]",
                string.Empty)
            .Replace(Environment.NewLine, "")
            .Replace(" ", "");

        //2. 取出上一步中所有英文字符
        var engStr = ExtractEngString(text);

        var ratio = (double)engStr.Length / text.Length;

        //3. 判断英文字符个数占第一步所有字符个数比例，若超过一定比例则判定原字符串为英文字符串，否则为中文字符串
        return ratio > scale
            ? (LangEnum.English, LangEnum.ChineseSimplified)
            : (LangEnum.ChineseSimplified, LangEnum.English);
    }

    /// <summary>
    ///     提取英文
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ExtractEngString(string str)
    {
        var regex = new Regex("[a-zA-Z]+");

        var matchCollection = regex.Matches(str);
        var ret = string.Empty;
        foreach (Match mMatch in matchCollection) ret += mMatch.Value;
        return ret;
    }

    public static string LinebreakHandler(string text, LineBreakHandleType type)
        => type switch
        {
            LineBreakHandleType.RemoveExtraLineBreak => NormalizeText(text),
            LineBreakHandleType.RemoveAllLineBreak => text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " "),
            LineBreakHandleType.RemoveAllLineBreakWithoutSpace => text.Replace("\r\n", "").Replace("\n", "").Replace("\r", ""),
            _ => text,
        };

    public static string CapturedTextHandler(
        string text,
        LineBreakHandleType lineBreakHandleType,
        TextSeparatorHandleType separatorHandleType,
        TextSeparatorHandleScope scope,
        TextSeparatorHandleScope enabledScopes)
    {
        var result = LinebreakHandler(text, lineBreakHandleType);
        return TextSeparatorHandler(result, separatorHandleType, scope, enabledScopes);
    }

    public static string TextSeparatorHandler(
        string text,
        TextSeparatorHandleType type,
        TextSeparatorHandleScope scope,
        TextSeparatorHandleScope enabledScopes)
    {
        if (type == TextSeparatorHandleType.None ||
            scope == TextSeparatorHandleScope.None ||
            (enabledScopes & scope) != scope)
        {
            return text;
        }

        return type switch
        {
            TextSeparatorHandleType.Underscore => WordInternalUnderscores.Replace(text, " "),
            TextSeparatorHandleType.Hyphen => WordInternalHyphens.Replace(text, " "),
            TextSeparatorHandleType.UnderscoreAndHyphen => WordInternalHyphens.Replace(WordInternalUnderscores.Replace(text, " "), " "),
            _ => text,
        };
    }

    /// <summary>
    /// 智能规范化给定的文本，自动修复 PDF 复制产生的硬断行、断词与连字符。
    /// </summary>
    /// <param name="text">要规范的源文本。</param>
    /// <returns>规范化后的文本。</returns>
    public static string NormalizeText(string text)
    {
        return Helpers.SmartTextPreprocessor.RepairPdfLineBreaks(text);
    }

    /// <summary>
    /// 文本框处理后可以Ctrl+Z撤销
    /// <see href="https://stackoverflow.com/questions/4476282/how-can-i-undo-a-textboxs-text-changes-caused-by-a-binding"/>
    /// </summary>
    /// <param name="textBox">需要处理的文本框。</param>
    /// <param name="transform">转换规则。</param>
    /// <param name="action">执行后动作。</param>
    public static void TransformText(TextBox textBox, Func<string, string> transform, Action? action = default)
    {
        var text = textBox.SelectedText.Length > 0 ? textBox.SelectedText : textBox.Text;

        var result = transform(text);
        if (result == text) return;

        if (textBox.SelectedText.Length == 0)
        {
            textBox.SelectAll();
        }

        textBox.SelectedText = result;

        action?.Invoke();

        textBox.Focus();
    }

    // 定义两个正则表达式模式列表，一个用于英文标点，一个用于中文标点
    private static readonly List<Regex> Patterns =
    [
        new(@"([?!.])[ ]?\n"), // 匹配英文标点符号后跟随换行符
        new(@"([？！。])[ ]?\n")
    ];
    // 定义一个正则表达式，用于匹配特定标点符号并用换行符替换
    private static readonly Regex SentenceEnds = new(@"#([?？！!.。])#");
    private static readonly Regex WordInternalUnderscores = new(@"(?<=[A-Za-z0-9])_+(?=[A-Za-z0-9])", RegexOptions.Compiled);
    private static readonly Regex WordInternalHyphens = new(@"(?<=[A-Za-z])-+(?=[A-Za-z0-9])|(?<=[A-Za-z0-9])-+(?=[A-Za-z])", RegexOptions.Compiled);

    #endregion

    #region Microsoft Authentication

    /// <summary>
    ///     https://github.com/d4n3436/GTranslate/blob/master/src/GTranslate/Translators/MicrosoftTranslator.cs
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string GetSignature(string url)
    {
        string guid = Guid.NewGuid().ToString("N");
        string escapedUrl = Uri.EscapeDataString(url);
        string dateTime = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ssG\\MT", CultureInfo.InvariantCulture);

        byte[] bytes = Encoding.UTF8.GetBytes($"MSTranslatorAndroidApp{escapedUrl}{dateTime}{guid}".ToLowerInvariant());

        using var hmac = new HMACSHA256(PrivateKey);
        byte[] hash = hmac.ComputeHash(bytes);

        return $"MSTranslatorAndroidApp::{Convert.ToBase64String(hash)}::{dateTime}::{guid}";
    }

    private static readonly byte[] PrivateKey =
    [
        0xa2, 0x29, 0x3a, 0x3d, 0xd0, 0xdd, 0x32, 0x73,
        0x97, 0x7a, 0x64, 0xdb, 0xc2, 0xf3, 0x27, 0xf5,
        0xd7, 0xbf, 0x87, 0xd9, 0x45, 0x9d, 0xf0, 0x5a,
        0x09, 0x66, 0xc6, 0x30, 0xc6, 0x6a, 0xaa, 0x84,
        0x9a, 0x41, 0xaa, 0x94, 0x3a, 0xa8, 0xd5, 0x1a,
        0x6e, 0x4d, 0xaa, 0xc9, 0xa3, 0x70, 0x12, 0x35,
        0xc7, 0xeb, 0x12, 0xf6, 0xe8, 0x23, 0x07, 0x9e,
        0x47, 0x10, 0x95, 0x91, 0x88, 0x55, 0xd8, 0x17
    ];

    #endregion

    #region WindowUtils

    public static FrameworkElement? FindSettingElementByContent(DependencyObject? parent, string content)
    {
        if (parent == null) return null;

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
            if (child != null)
            {
                switch (child)
                {
                    case SettingsCard settingsCard when
                    (settingsCard.Header is string header && header.Equals(content, StringComparison.OrdinalIgnoreCase)) ||
                    (settingsCard.Description is string description && description.Equals(content, StringComparison.OrdinalIgnoreCase)):
                        return settingsCard;

                    case SettingsExpander settingsExpander when
                    (settingsExpander.Header is string expanderHeader && expanderHeader.Equals(content, StringComparison.OrdinalIgnoreCase)) ||
                    (settingsExpander.Description is string expanderDescription && expanderDescription.Equals(content, StringComparison.OrdinalIgnoreCase)):
                        return settingsExpander;
                }

                (child as Expander)?.IsExpanded = true;
            }

            var result = FindSettingElementByContent(child, content);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static void BringIntoViewAndHighlight(FrameworkElement element)
    {
        element.BringIntoView();

        if (element is SettingsExpander settingsExpander)
        {
            // iNKORE.UI.WPF.Modern 中 背景色在名为ExpanderHeader 的 ToggleButton上设定，没有取Template Background
            var expanderHeader = FindVisualChild<ToggleButton>(settingsExpander, "ExpanderHeader");
            if (expanderHeader != null)
            {
                element = expanderHeader;
            }
        }

        // 获取element的背景色存储为brush
        var originalBrush = element.GetValue(Panel.BackgroundProperty) as SolidColorBrush;

        var highlightColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#40808080");
        var transparentColor = Colors.Transparent;

        var animation = new ColorAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.FromSeconds(1.3)),
            FillBehavior = FillBehavior.Stop // 动画结束后恢复原样
        };
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2))));

        var brush = new SolidColorBrush(transparentColor);
        // 将背景设置为动画画笔
        element.SetCurrentValue(Panel.BackgroundProperty, brush);

        // 动画结束后，将背景属性设置为 null 以恢复默认值
        animation.Completed += (s, e) =>
        {
            element.SetCurrentValue(Panel.BackgroundProperty, originalBrush);
        };

        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    public static T? FindVisualChild<T>(DependencyObject? parent, string? childName = null) where T : FrameworkElement
    {
        if (parent == null) return null;

        T? foundChild = null;

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is not T childType)
            {
                foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null) break;
            }
            else if (!string.IsNullOrEmpty(childName))
            {
                if (childType.Name == childName)
                {
                    foundChild = childType;
                    break;
                }
            }
            else
            {
                foundChild = childType;
                break;
            }
        }

        return foundChild;
    }

    /// <summary>
    /// Finds a visual child of a specified type.
    /// </summary>
    public static T? GetVisualChild<T>(DependencyObject parent) where T : Visual
    {
        T? child = default;
        int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < numVisuals; i++)
        {
            var v = (Visual)VisualTreeHelper.GetChild(parent, i);
            child = v as T ?? GetVisualChild<T>(v);
            if (child != null)
            {
                break;
            }
        }
        return child;
    }

    #endregion

    #region BitmapUtils

    public static BitmapImage ToBitmapImage(Bitmap bitmap, ImageFormat? imageFormat = default)
    {
        using var memory = new MemoryStream();
        imageFormat ??= ImageFormat.Png;    // 默认使用 PNG 格式
        bitmap.Save(memory, imageFormat);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static BitmapImage ToBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);

        var img = new BitmapImage();
        img.BeginInit();
        img.StreamSource = stream;
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }

    public static Bitmap ToBitmap(BitmapSource bitmapSource, BitmapEncoder? encoder = default)
    {
        // 规范化 BitmapSource 到标准格式
        var formatConvertedBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgr24, null, 0);

        encoder ??= new PngBitmapEncoder(); // 默认使用 PNG 编码器
        encoder.Frames.Add(BitmapFrame.Create(formatConvertedBitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        // 创建一个新的Bitmap，它会复制数据而不依赖于流
        using var originalBitmap = new Bitmap(stream);
        return new Bitmap(originalBitmap);
    }

    public static Bitmap ToBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    public static byte[] ToBytes(BitmapSource bitmapSource, BitmapEncoder? encoder = default)
    {
        encoder ??= new PngBitmapEncoder(); // 默认使用 PNG 编码器
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static byte[] ToBytes(Bitmap bitmap, ImageFormat? imageFormat = default)
    {
        imageFormat ??= ImageFormat.Png; // 默认使用 PNG 格式
        using var stream = new MemoryStream();
        bitmap.Save(stream, imageFormat);
        return stream.ToArray();
    }

    public static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    public static byte[] ToBase64Utf8Bytes(byte[] bytes)
    {
        var base64String = Convert.ToBase64String(bytes);
        return Encoding.UTF8.GetBytes(base64String);
    }

    public static byte[] ToBase64Utf8BytesFast(byte[] bytes)
    {
        var base64Length = ((bytes.Length + 2) / 3) * 4;
        var base64Chars = base64Length <= 1024
            ? stackalloc char[base64Length]
            : new char[base64Length];

        Convert.TryToBase64Chars(bytes, base64Chars, out _);
        return Encoding.UTF8.GetBytes(base64Chars.ToArray());
    }

    /// <summary>
    ///     图像变成背景
    /// </summary>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static ImageBrush ToImageBrush(Bitmap bmp)
    {
        var hBitmap = bmp.GetHbitmap();
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            bitmapSource.Freeze();

            var brush = new ImageBrush { ImageSource = bitmapSource };
            brush.Freeze();

            return brush;
        }
        finally
        {
            // 释放 GDI 对象以防止内存泄漏
            if (hBitmap != IntPtr.Zero)
                PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
        }
    }

    /// <summary>
    ///     通过FileStream 来打开文件，这样就可以实现不锁定Image文件，到时可以让多用户同时访问Image文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Bitmap? ToBitmap(string path)
    {
        if (!File.Exists(path)) return null; // 文件不存在

        using var fs = File.OpenRead(path);
        var result = System.Drawing.Image.FromStream(fs); // 从流中创建图像
        return new Bitmap(result); // 创建并返回位图
    }

    #endregion

    #region ProcessUtils

    public static bool IsMultiInstance()
    {
        var runningProcesses = Process.GetProcessesByName(Constant.AppName);
        return runningProcesses.Length > 1;
    }

    /// <summary>
    /// Starts an external program with the specified arguments and options, optionally waiting for it to exit, and
    /// returns the result of the execution.
    /// </summary>
    /// <remarks>If <paramref name="useAdmin"/> is <see langword="true"/>, the method may prompt the user for
    /// UAC consent. If <paramref name="wait"/> is <see langword="false"/>, the method returns immediately after
    /// starting the process, and ExitCode is set to 0. If the process fails to start, times out, or is cancelled by the
    /// user, Success is <see langword="false"/> and ErrorMessage provides details.</remarks>
    /// <param name="filename">The path to the executable file to start. Cannot be null or empty.</param>
    /// <param name="args">An array of command-line arguments to pass to the program. May be empty if no arguments are required.</param>
    /// <param name="useAdmin">Specifies whether to run the program with administrator privileges. If <see langword="true"/>, the process is
    /// started with elevated rights and may prompt for UAC consent.</param>
    /// <param name="wait">Specifies whether to wait for the program to exit before returning. If <see langword="true"/>, the method waits
    /// for the process to complete or until the timeout elapses.</param>
    /// <param name="timeout">The maximum time, in milliseconds, to wait for the process to exit when <paramref name="wait"/> is <see
    /// langword="true"/>. Must be greater than zero.</param>
    /// <returns>A tuple containing a success flag, the exit code if available, and an error message if the process failed to
    /// start or complete. If successful, Success is <see langword="true"/>, ExitCode contains the process exit code,
    /// and ErrorMessage is empty.</returns>
    public static (bool Success, int? ExitCode, string ErrorMessage) ExecuteProgram(
        string filename,
        string[] args,
        bool useAdmin = false,
        bool wait = false,
        int timeout = 30000)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return (false, null, "Filename is null or empty");

        Process? process = null;
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = BuildArguments(args),
                UseShellExecute = useAdmin,
                CreateNoWindow = !useAdmin, // useAdmin 时无法隐藏窗口
            };

            if (useAdmin)
            {
                processStartInfo.Verb = "runas";
            }

            process = Process.Start(processStartInfo);

            if (process == null)
                return (false, null, "Failed to start process");

            if (!wait)
            {
                // 不等待时，不使用 using，让进程继续运行
                process = null; // 防止 finally 中 Dispose
                return (true, 0, "");
            }

            // 等待进程退出
            var completed = process.WaitForExit(timeout);
            if (!completed)
            {
                // 超时处理
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000); // 等待进程完全退出
                    }
                }
                catch (Exception killEx)
                {
                    return (false, null, $"Timeout and failed to kill: {killEx.Message}");
                }
                return (false, null, "Process timeout");
            }

            // 安全获取退出码
            int exitCode;
            try
            {
                exitCode = process.ExitCode;
            }
            catch (Exception)
            {
                return (false, null, "Failed to retrieve exit code");
            }

            return (true, exitCode, "");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, null, "User cancelled UAC prompt");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// 正确转义命令行参数
    /// 参考: https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
    /// </summary>
    private static string BuildArguments(string[] args)
    {
        if (args == null || args.Length == 0)
            return string.Empty;

        var result = new StringBuilder();

        foreach (var arg in args)
        {
            if (result.Length > 0)
                result.Append(' ');

            // 判断是否需要引号
            if (string.IsNullOrEmpty(arg))
            {
                result.Append("\"\"");
                continue;
            }

            bool needsQuotes = arg.Contains(' ') || arg.Contains('\t') || arg.Contains('"');

            if (!needsQuotes)
            {
                result.Append(arg);
                continue;
            }

            // 添加引号并正确转义
            result.Append('"');

            int backslashCount = 0;
            foreach (char c in arg)
            {
                if (c == '\\')
                {
                    backslashCount++;
                }
                else if (c == '"')
                {
                    // 转义前面的反斜杠和当前引号
                    result.Append('\\', backslashCount * 2 + 1);
                    result.Append('"');
                    backslashCount = 0;
                }
                else
                {
                    result.Append('\\', backslashCount);
                    result.Append(c);
                    backslashCount = 0;
                }
            }

            // 结尾的反斜杠需要双倍转义（因为后面有引号）
            result.Append('\\', backslashCount * 2);
            result.Append('"');
        }

        return result.ToString();
    }

    #endregion

    #region ShortcutUtils

    /// <summary>
    ///     设置开机自启
    /// </summary>
    public static void SetStartup()
    {
        ShortCutCreate();
    }

    /// <summary>
    ///     检查是否已经设置开机自启
    /// </summary>
    /// <returns>true: 开机自启 false: 非开机自启</returns>
    public static bool IsStartup()
    {
        return ShortCutExist(DataLocation.AppExePath, DataLocation.StartupPath);
    }

    /// <summary>
    ///     取消开机自启
    /// </summary>
    public static void UnSetStartup()
    {
        ShortCutDelete(DataLocation.AppExePath, DataLocation.StartupPath);
    }

    /// <summary>
    ///     设置桌面快捷方式
    /// </summary>
    public static void SetDesktopShortcut()
    {
        ShortCutCreate(true);
    }

    #region Private Method

    /// <summary>
    ///     获取指定文件夹下的所有快捷方式（不包括子文件夹）
    /// </summary>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static List<string> GetDirectoryFileList(string target)
    {
        if (!Directory.Exists(target))
            return [];

        return [.. Directory.GetFiles(target, "*.lnk")];
    }

    /// <summary>
    ///     判断快捷方式是否存在
    /// </summary>
    /// <param name="path">快捷方式目标（可执行文件的绝对路径）</param>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static bool ShortCutExist(string path, string target)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be null or empty", nameof(target));

        if (!Directory.Exists(target))
            return false;

        var list = GetDirectoryFileList(target);
        return list.Any(item => path.Equals(GetAppPathViaShortCut(item), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     删除快捷方式（通过快捷方式目标进行删除）
    /// </summary>
    /// <param name="path">快捷方式目标（可执行文件的绝对路径）</param>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static bool ShortCutDelete(string path, string target)
    {
        var result = false;
        var list = GetDirectoryFileList(target);
        foreach (var item in list.Where(item => path == GetAppPathViaShortCut(item)))
        {
            File.Delete(item);
            result = true;
        }

        return result;
    }

    /// <summary>
    ///     为本程序创建一个快捷方式
    /// </summary>
    /// <param name="isDesktop">是否为桌面快捷方式</param>
    /// <returns></returns>
    private static bool ShortCutCreate(bool isDesktop = false)
    {
        var result = true;
        try
        {
            var shortcutPath = isDesktop ? DataLocation.DesktopShortcutPath : DataLocation.StartupShortcutPath;
            CreateShortcut(shortcutPath, DataLocation.AppExePath, DataLocation.AppExePath);
        }
        catch
        {
            result = false;
        }

        return result;
    }

    #region 非 COM 实现快捷键创建

    /// <see href="https://blog.csdn.net/weixin_42288222/article/details/124150046" />
    /// <summary>
    ///     获取快捷方式中的目标（可执行文件的绝对路径）
    /// </summary>
    /// <param name="shortCutPath">快捷方式的绝对路径</param>
    /// <returns></returns>
    private static string? GetAppPathViaShortCut(string shortCutPath)
    {
        try
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var file = (IShellLink)new ShellLink();
            try
            {
                file.Load(shortCutPath, 2);
                var sb = new StringBuilder(256);
                file.GetPath(sb, sb.Capacity, IntPtr.Zero, 2);
                return sb.ToString();
            }
            finally
            {
                // 释放COM对象
                if (file != null && Marshal.IsComObject(file))
                {
                    Marshal.ReleaseComObject(file);
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     向目标路径创建指定文件的快捷方式
    /// </summary>
    /// <param name="shortcutPath">快捷方式路径</param>
    /// <param name="appPath">App路径</param>
    /// <param name="description">提示信息</param>
    private static void CreateShortcut(string shortcutPath, string appPath, string description)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var link = (IShellLink)new ShellLink();
        link.SetDescription(description);
        link.SetPath(appPath);
        var workingDir = Directory.GetParent(appPath)?.FullName;
        if (workingDir != null)
            link.SetWorkingDirectory(workingDir);

        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
        link.Save(shortcutPath, false);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink : IPersistFile
    {
        void GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd,
            int fFlags);

        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
            out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    #endregion

    #endregion

    #endregion

    #region OcrUtils

    public static bool HasBoxPoints(OcrResult ocrResult)
    {
        return ocrResult.OcrContents != null &&
               (ocrResult.OcrContents.Any(content => content.BoxPoints != null && content.BoxPoints.Count > 0) ||
                ocrResult.Regions.Any(region =>
                    region.BoxPoints.Count > 0 ||
                    region.Paragraphs.Any(paragraph =>
                        paragraph.BoxPoints.Count > 0 ||
                        paragraph.Lines.Any(line => line.BoxPoints.Count > 0))));
    }

    public static void PrepareOcrResult(OcrResult ocrResult)
    {
        if (ocrResult.OcrContents.Count == 0)
            ProjectStructuredLayoutToContents(ocrResult);
    }

    private static void ProjectStructuredLayoutToContents(OcrResult ocrResult)
    {
        foreach (var paragraph in ocrResult.Regions.SelectMany(region => region.Paragraphs))
        {
            var lines = paragraph.Lines.Where(line => !string.IsNullOrWhiteSpace(line.Text)).ToList();
            if (lines.Count == 0)
                continue;

            ocrResult.OcrContents.Add(new OcrContent
            {
                Text = JoinOcrLines(lines),
                BoxPoints = paragraph.BoxPoints.Count > 0
                    ? CloneBoxPoints(paragraph.BoxPoints)
                    : UnionBoxPoints(lines.Select(line => line.BoxPoints).ToList())
            });
        }
    }

    private static string JoinOcrLines(IReadOnlyList<OcrContent> lines)
    {
        var text = lines[0].Text;
        for (var i = 1; i < lines.Count; i++)
        {
            if (NeedsOcrSpace(text, lines[i].Text))
                text += " ";

            text += lines[i].Text;
        }

        return text;
    }

    private static bool NeedsOcrSpace(string previous, string current)
    {
        if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(current))
            return false;

        var left = previous[^1];
        var right = current[0];
        return !char.IsWhiteSpace(left) &&
               !char.IsWhiteSpace(right) &&
               !char.IsPunctuation(left) &&
               !char.IsPunctuation(right) &&
               !TextHelper.IsCjk(left) &&
               !TextHelper.IsCjk(right);
    }

    private static List<BoxPoint> CloneBoxPoints(IReadOnlyList<BoxPoint> points) =>
        points.Select(point => new BoxPoint(point.X, point.Y)).ToList();

    private static List<BoxPoint> UnionBoxPoints(IReadOnlyList<List<BoxPoint>> boxPointGroups)
    {
        var validGroups = boxPointGroups.Where(points => points.Count > 0).ToList();
        if (validGroups.Count == 0)
            return [];

        var minX = validGroups.Min(points => points.Min(point => point.X));
        var minY = validGroups.Min(points => points.Min(point => point.Y));
        var maxX = validGroups.Max(points => points.Max(point => point.X));
        var maxY = validGroups.Max(points => points.Max(point => point.Y));

        return
        [
            new(minX, minY),
            new(maxX, minY),
            new(maxX, maxY),
            new(minX, maxY)
        ];
    }

    #endregion
}
