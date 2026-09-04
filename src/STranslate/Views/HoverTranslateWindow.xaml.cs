using STranslate.Helpers;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace STranslate.Views;

public partial class HoverTranslateWindow : Window
{
    private const int CursorOffset = 18;

    public HoverTranslateWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Win32Helper.MakeWindowNonActivatingToolWindow(this);
    }

    public void ShowResult(string source, string translated, System.Drawing.Point physicalCursor)
    {
        SourceText.Text = source;
        TranslatedText.Text = translated;

        // 计算屏幕物理与逻辑坐标
        var dpiScale = Win32Helper.GetDpiScaleForPhysicalPoint(physicalCursor.X, physicalCursor.Y);
        var targetLeft = (physicalCursor.X + CursorOffset) / dpiScale.DpiScaleX;
        var targetTop = (physicalCursor.Y + CursorOffset) / dpiScale.DpiScaleY;

        // 限制在屏幕可见工作区内
        var screen = MonitorInfo.GetDisplayMonitors()
            .FirstOrDefault(m => m.Bounds.Contains(new System.Windows.Point(targetLeft, targetTop)))
            ?? MonitorInfo.GetPrimaryDisplayMonitor();

        if (screen != null)
        {
            var workArea = screen.WorkingArea;
            if (targetLeft + ActualWidth > workArea.Right)
                targetLeft = physicalCursor.X / dpiScale.DpiScaleX - ActualWidth - 8;
            if (targetTop + ActualHeight > workArea.Bottom)
                targetTop = physicalCursor.Y / dpiScale.DpiScaleY - ActualHeight - 8;

            targetLeft = Math.Max(workArea.Left + 4, targetLeft);
            targetTop = Math.Max(workArea.Top + 4, targetTop);
        }

        Left = targetLeft;
        Top = targetTop;

        if (Visibility != Visibility.Visible)
        {
            Show();
            if (Resources["FadeInStoryboard"] is Storyboard sb)
            {
                sb.Begin();
            }
        }
    }

    private DispatcherTimer? _autoHideTimer;

    public void ShowToast(string title, string message, System.Drawing.Point physicalCursor, int durationMs = 1500)
    {
        _autoHideTimer?.Stop();
        ShowResult(title, message, physicalCursor);

        _autoHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _autoHideTimer.Tick += (s, e) =>
        {
            _autoHideTimer?.Stop();
            HideWindow();
        };
        _autoHideTimer.Start();
    }

    public void HideWindow()
    {
        _autoHideTimer?.Stop();
        if (Visibility == Visibility.Visible)
        {
            Hide();
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TranslatedText.Text))
        {
            Clipboard.SetText(TranslatedText.Text);
        }
    }
}
