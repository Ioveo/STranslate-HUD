using STranslate.Helpers;
using System.Drawing;
using System.Windows;
using Xunit;

namespace STranslate.Tests;

public class UiAutomationHelperTests
{
    [Fact]
    public void UiElementInfo_InitializesCorrectly()
    {
        var rect = new Rect(10, 20, 100, 50);
        var info = new UiElementInfo("Test Button", rect, "Button", "btn_submit");

        Assert.Equal("Test Button", info.Text);
        Assert.Equal(rect, info.BoundingRectangle);
        Assert.Equal("Button", info.ControlType);
        Assert.Equal("btn_submit", info.AutomationId);
    }

    [Fact]
    public void ExtractWindowElements_ReturnsEmptyList_WhenHandleIsZero()
    {
        var result = UiAutomationHelper.ExtractWindowElements(0);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractWindowElements_ReturnsEmptyList_WhenHandleIsInvalid()
    {
        var result = UiAutomationHelper.ExtractWindowElements(-1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetElementTextUnderPoint_HandlesInvalidPoints_WithoutThrowing()
    {
        // Negative coordinates or points way off screen should return null gracefully
        var info = UiAutomationHelper.GetElementTextUnderPoint(new System.Drawing.Point(-99999, -99999));

        Assert.Null(info);
    }
}
