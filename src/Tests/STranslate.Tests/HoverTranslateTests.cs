using STranslate.Views;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace STranslate.Tests;

public class HoverTranslateTests
{
    [Fact]
    public void HudOverlayItem_InitializesCorrectly()
    {
        var relativeRect = new Rect(50, 100, 200, 30);
        var item = new HudOverlayItem(
            "File",
            "文件",
            relativeRect,
            Color.FromArgb(220, 20, 20, 20),
            Colors.White);

        Assert.Equal("File", item.OriginalText);
        Assert.Equal("文件", item.TranslatedText);
        Assert.Equal(relativeRect, item.RelativeRect);
        Assert.Equal(Colors.White, item.ForegroundColor);
        Assert.Equal(220, item.BackgroundColor.A);
    }
}
