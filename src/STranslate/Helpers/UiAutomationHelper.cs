using System.Windows;
using System.Windows.Automation;

namespace STranslate.Helpers;

/// <summary>
/// 表示从 Windows UI Automation 树中提取的控件文本信息及所在物理屏幕矩形
/// </summary>
public sealed record UiElementInfo(
    string Text,
    Rect BoundingRectangle,
    string? ControlType = null,
    string? AutomationId = null);

/// <summary>
/// Windows UI Automation (UIA) 辅助工具：
/// 用于从前台软件界面、光标所在控件或整个窗口中无障碍提取可见文本及屏幕物理坐标。
/// 零侵入、零注入、100% 安全且零延迟。
/// </summary>
public static class UiAutomationHelper
{
    private const int MaxElementsPerWindow = 120;

    /// <summary>
    /// 获取屏幕指定物理坐标下控件的文本及边界矩形
    /// </summary>
    /// <param name="physicalPoint">屏幕物理坐标</param>
    /// <returns>控件文本及位置信息，若未找到或为空则返回 null</returns>
    public static UiElementInfo? GetElementTextUnderPoint(System.Drawing.Point physicalPoint)
    {
        try
        {
            var wpfPoint = new Point(physicalPoint.X, physicalPoint.Y);
            var element = AutomationElement.FromPoint(wpfPoint);
            if (element == null)
                return null;

            return ExtractInfoFromElement(element);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 扫描指定窗口句柄下的所有可见 UI 控件，提取其文本和屏幕绝对物理坐标
    /// </summary>
    /// <param name="windowHandle">目标窗口 HWND</param>
    /// <returns>提取到的可见文本控件列表</returns>
    public static List<UiElementInfo> ExtractWindowElements(nint windowHandle)
    {
        var result = new List<UiElementInfo>();
        if (windowHandle == 0)
            return result;

        try
        {
            var rootElement = AutomationElement.FromHandle(windowHandle);
            if (rootElement == null)
                return result;

            // 查找所有控件视图子孙节点
            var condition = new PropertyCondition(AutomationElement.IsOffscreenProperty, false);
            var collection = rootElement.FindAll(TreeScope.Descendants, condition);

            var seenTextsAndPositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (AutomationElement item in collection)
            {
                if (result.Count >= MaxElementsPerWindow)
                    break;

                try
                {
                    var info = ExtractInfoFromElement(item);
                    if (info == null || string.IsNullOrWhiteSpace(info.Text))
                        continue;

                    // 过滤掉极其纯数字或只有符号的文本
                    var text = info.Text.Trim();
                    if (text.Length < 2 && !char.IsLetter(text[0]))
                        continue;

                    var key = $"{text}_{Math.Round(info.BoundingRectangle.X)}_{Math.Round(info.BoundingRectangle.Y)}";
                    if (seenTextsAndPositions.Add(key))
                    {
                        result.Add(info);
                    }
                }
                catch
                {
                    // 忽略单个控件提取异常
                }
            }
        }
        catch
        {
            // 忽略窗口被关闭或权限不足导致的异常
        }

        return result;
    }

    private static UiElementInfo? ExtractInfoFromElement(AutomationElement element)
    {
        try
        {
            var name = element.Current.Name;
            var helpText = element.Current.HelpText;
            var controlType = element.Current.ControlType?.ProgrammaticName;
            var rect = element.Current.BoundingRectangle;

            // 优先从 ValuePattern 中取（如文本输入框或下拉列表中的当前值）
            string? value = null;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern) && pattern is ValuePattern vp)
            {
                value = vp.Current.Value;
            }

            // 选择最具有可读性的文本
            var text = !string.IsNullOrWhiteSpace(name)
                ? name
                : (!string.IsNullOrWhiteSpace(value) ? value : helpText);

            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                return null;

            return new UiElementInfo(
                text.Trim(),
                rect,
                controlType,
                element.Current.AutomationId);
        }
        catch
        {
            return null;
        }
    }
}
