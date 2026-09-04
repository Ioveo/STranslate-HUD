using STranslate.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace STranslate.Views;

public sealed record HudOverlayItem(
    string OriginalText,
    string TranslatedText,
    Rect RelativeRect,
    Color BackgroundColor,
    Color ForegroundColor);

public partial class LiveHudOverlayWindow : Window
{
    private nint _targetHwnd;
    private readonly DispatcherTimer _trackingTimer;
    private System.Drawing.Rectangle _lastPhysicalBounds;

    public LiveHudOverlayWindow()
    {
        InitializeComponent();

        _trackingTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _trackingTimer.Tick += OnTrackingTimerTick;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // 关键：WS_EX_TRANSPARENT 实现鼠标事件完全向下穿透至底层被翻译软件
        Win32Helper.MakeWindowTransparentClickThrough(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    public void AttachToTarget(nint targetHandle, List<HudOverlayItem> items)
    {
        _targetHwnd = targetHandle;

        UpdateBoundsFromTarget();
        RenderItems(items);

        Show();
        _trackingTimer.Start();
    }

    private void RenderItems(List<HudOverlayItem> items)
    {
        OverlayCanvas.Children.Clear();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.TranslatedText))
                continue;

            var bgBrush = new SolidColorBrush(item.BackgroundColor);
            bgBrush.Freeze();
            var fgBrush = new SolidColorBrush(item.ForegroundColor);
            fgBrush.Freeze();

            var border = new Border
            {
                Background = bgBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    Opacity = 0.3,
                    ShadowDepth = 1,
                    Color = Colors.Black
                }
            };

            var textBlock = new TextBlock
            {
                Text = item.TranslatedText,
                Foreground = fgBrush,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            border.Child = textBlock;

            Canvas.SetLeft(border, Math.Max(0, item.RelativeRect.Left));
            Canvas.SetTop(border, Math.Max(0, item.RelativeRect.Top));
            if (item.RelativeRect.Width > 10)
            {
                border.MaxWidth = Math.Max(40, item.RelativeRect.Width * 1.5);
            }

            OverlayCanvas.Children.Add(border);
        }
    }

    private void OnTrackingTimerTick(object? sender, EventArgs e)
    {
        if (_targetHwnd == 0 || !Win32Helper.IsWindow(_targetHwnd))
        {
            Close();
            return;
        }

        if (Win32Helper.IsIconic(_targetHwnd))
        {
            if (Visibility == Visibility.Visible)
                Visibility = Visibility.Collapsed;
            return;
        }

        if (Visibility != Visibility.Visible)
            Visibility = Visibility.Visible;

        UpdateBoundsFromTarget();
    }

    private void UpdateBoundsFromTarget()
    {
        if (_targetHwnd == 0) return;

        if (!Win32Helper.GetTargetWindowBounds(_targetHwnd, out var bounds))
            return;

        if (bounds == _lastPhysicalBounds)
            return;

        _lastPhysicalBounds = bounds;

        var dpiScale = Win32Helper.GetDpiScaleForPhysicalPoint(bounds.Left, bounds.Top);
        Left = bounds.Left / dpiScale.DpiScaleX;
        Top = bounds.Top / dpiScale.DpiScaleY;
        Width = bounds.Width / dpiScale.DpiScaleX;
        Height = bounds.Height / dpiScale.DpiScaleY;
    }

    protected override void OnClosed(EventArgs e)
    {
        _trackingTimer.Stop();
        base.OnClosed(e);
    }
}
