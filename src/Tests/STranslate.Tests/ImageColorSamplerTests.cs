using STranslate.Core;
using STranslate.Helpers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace STranslate.Tests;

public class ImageColorSamplerTests
{
    [Fact]
    public void SampleColors_ReturnsFallback_WhenImageIsNull()
    {
        var (bgColor, fgColor) = ImageColorSampler.SampleColors(null, new Rect(0, 0, 100, 100), ImageTranslateOverlayTheme.Light);

        Assert.Equal(Colors.Black, fgColor);
        Assert.True(bgColor.A > 0);
    }

    [Fact]
    public void SampleColors_ReturnsFallback_WhenRectIsEmpty()
    {
        var (bgColor, fgColor) = ImageColorSampler.SampleColors(null, Rect.Empty, ImageTranslateOverlayTheme.Dark);

        Assert.Equal(Colors.White, fgColor);
        Assert.True(bgColor.A > 0);
    }

    [Fact]
    public void SampleColors_SamplesDarkBackground_AndReturnsWhiteForeground()
    {
        // Create 100x100 dark blue bitmap (R=10, G=20, B=50)
        var width = 100;
        var height = 100;
        var stride = width * 4;
        var pixels = new byte[height * stride];

        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 50;     // B
            pixels[i + 1] = 20; // G
            pixels[i + 2] = 10; // R
            pixels[i + 3] = 255;// A
        }

        var bitmap = BitmapSource.Create(
            width, height, 96, 96,
            PixelFormats.Bgra32, null,
            pixels, stride);

        var (bgColor, fgColor) = ImageColorSampler.SampleColors(bitmap, new Rect(10, 10, 50, 30));

        // Dark background should yield White text foreground
        Assert.Equal(Colors.White, fgColor);
        Assert.InRange(bgColor.R, (byte)5, (byte)15);
        Assert.InRange(bgColor.G, (byte)15, (byte)25);
        Assert.InRange(bgColor.B, (byte)45, (byte)55);
    }

    [Fact]
    public void SampleColors_SamplesLightBackground_AndReturnsBlackForeground()
    {
        // Create 100x100 light yellow/white bitmap (R=240, G=240, B=230)
        var width = 100;
        var height = 100;
        var stride = width * 4;
        var pixels = new byte[height * stride];

        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 230;    // B
            pixels[i + 1] = 240;// G
            pixels[i + 2] = 240;// R
            pixels[i + 3] = 255;// A
        }

        var bitmap = BitmapSource.Create(
            width, height, 96, 96,
            PixelFormats.Bgra32, null,
            pixels, stride);

        var (bgColor, fgColor) = ImageColorSampler.SampleColors(bitmap, new Rect(20, 20, 40, 20));

        // Light background should yield Black text foreground
        Assert.Equal(Colors.Black, fgColor);
        Assert.InRange(bgColor.R, (byte)235, (byte)245);
        Assert.InRange(bgColor.G, (byte)235, (byte)245);
        Assert.InRange(bgColor.B, (byte)225, (byte)235);
    }
}
