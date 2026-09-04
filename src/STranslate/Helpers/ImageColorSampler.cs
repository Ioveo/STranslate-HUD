using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace STranslate.Helpers;

/// <summary>
/// 原图局部背景色彩与亮度采样工具：
/// 从原图文字选区的周边边缘采样背景像素，计算最适合的原图融合背景色与高对比度文字前景色。
/// </summary>
internal static class ImageColorSampler
{
    private const double LuminanceThreshold = 0.55;
    private const byte BackgroundAlpha = 240; // 0.94 不透明度，既遮盖原文字又与背景融为一体

    /// <summary>
    /// 对原图指定矩形区域的周边背景进行色彩采样，提取局部平均背景色与高对比度文字前景色
    /// </summary>
    /// <param name="sourceImage">原图位图</param>
    /// <param name="textRect">文字所在的原图像素矩形</param>
    /// <param name="fallbackTheme">当无法采样时的回退主题</param>
    /// <returns>计算得到的背景色和文字前景色</returns>
    internal static (Color BackgroundColor, Color ForegroundColor) SampleColors(
        BitmapSource? sourceImage,
        Rect textRect,
        Core.ImageTranslateOverlayTheme fallbackTheme = Core.ImageTranslateOverlayTheme.Light)
    {
        if (sourceImage == null || textRect.IsEmpty || textRect.Width <= 0 || textRect.Height <= 0)
            return GetFallbackColors(fallbackTheme);

        try
        {
            var pixelWidth = sourceImage.PixelWidth;
            var pixelHeight = sourceImage.PixelHeight;

            // 选区向外微扩 1 像素以更好地采样真实背景（不超过图片边界）
            var expandX = Math.Max(0, (int)textRect.X - 1);
            var expandY = Math.Max(0, (int)textRect.Y - 1);
            var expandW = Math.Clamp((int)textRect.Width + 2, 1, pixelWidth - expandX);
            var expandH = Math.Clamp((int)textRect.Height + 2, 1, pixelHeight - expandY);

            if (expandW <= 0 || expandH <= 0)
                return GetFallbackColors(fallbackTheme);

            // 转为 Bgra32 格式以统一读取像素
            BitmapSource formattedSource;
            if (sourceImage.Format == PixelFormats.Bgra32 || sourceImage.Format == PixelFormats.Pbgra32 || sourceImage.Format == PixelFormats.Bgr32)
            {
                formattedSource = sourceImage;
            }
            else
            {
                formattedSource = new FormatConvertedBitmap(sourceImage, PixelFormats.Bgra32, null, 0);
            }

            var stride = expandW * 4;
            var pixels = new byte[expandH * stride];
            var cropped = new CroppedBitmap(formattedSource, new Int32Rect(expandX, expandY, expandW, expandH));
            cropped.CopyPixels(pixels, stride, 0);

            long rSum = 0, gSum = 0, bSum = 0;
            var sampleCount = 0;

            // 采样矩形四条边缘边框的像素（边缘受文字内部笔画干扰最小）
            var borderThickness = Math.Clamp(Math.Min(expandW, expandH) / 6, 1, 3);

            for (var row = 0; row < expandH; row++)
            {
                var isRowBorder = row < borderThickness || row >= expandH - borderThickness;

                for (var col = 0; col < expandW; col++)
                {
                    var isColBorder = col < borderThickness || col >= expandW - borderThickness;

                    if (isRowBorder || isColBorder)
                    {
                        var index = row * stride + col * 4;
                        if (index + 2 < pixels.Length)
                        {
                            bSum += pixels[index];     // Blue
                            gSum += pixels[index + 1]; // Green
                            rSum += pixels[index + 2]; // Red
                            sampleCount++;
                        }
                    }
                }
            }

            if (sampleCount == 0)
                return GetFallbackColors(fallbackTheme);

            var avgR = (byte)Math.Clamp(rSum / sampleCount, 0, 255);
            var avgG = (byte)Math.Clamp(gSum / sampleCount, 0, 255);
            var avgB = (byte)Math.Clamp(bSum / sampleCount, 0, 255);

            var bgColor = Color.FromArgb(BackgroundAlpha, avgR, avgG, avgB);

            // 相对亮度公式 (ITU-R BT.709 / sRGB Standard Luminance)
            var luminance = (0.299 * avgR + 0.587 * avgG + 0.114 * avgB) / 255.0;
            var fgColor = luminance > LuminanceThreshold ? Colors.Black : Colors.White;

            return (bgColor, fgColor);
        }
        catch
        {
            return GetFallbackColors(fallbackTheme);
        }
    }

    private static (Color BackgroundColor, Color ForegroundColor) GetFallbackColors(Core.ImageTranslateOverlayTheme theme)
    {
        return theme == Core.ImageTranslateOverlayTheme.Dark
            ? (Color.FromArgb(230, 25, 25, 25), Colors.White)
            : (Color.FromArgb(238, 255, 255, 255), Colors.Black);
    }
}
