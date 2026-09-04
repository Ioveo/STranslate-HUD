using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using STranslate.Helpers;
using STranslate.ViewModels;
using System.ComponentModel;

namespace STranslate.Views;

public partial class ImageTranslateWindow
{
    private readonly ImageTranslateWindowViewModel _viewModel;
    private readonly IServiceScope _serviceScope;

    public ImageTranslateWindow()
    {
        _serviceScope = Ioc.Default.CreateScope();
        try
        {
            _viewModel = _serviceScope.ServiceProvider.GetRequiredService<ImageTranslateWindowViewModel>();
            DataContext = _viewModel;

            InitializeComponent();
        }
        catch
        {
            _serviceScope.Dispose();
            throw;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _viewModel.CancelOperations();
        base.OnClosing(e);

        if (!e.Cancel)
            ModernWindowLifecycle.DetachModernWindowStyle(this);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == System.Windows.Input.Key.Space && !e.IsRepeat && !(System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox))
        {
            _viewModel.IsPeekingOriginal = true;
            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyUp(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyUp(e);
        if (e.Key == System.Windows.Input.Key.Space && !(System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox))
        {
            _viewModel.IsPeekingOriginal = false;
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // VM 由独立 DI scope 持有，释放 scope 会触发 ViewModel.Dispose()，
        // 取消对 OcrService/TranslateService/Settings 等单例的事件订阅。
        // 直接从 root provider 解析 Transient 不被跟踪、Dispose 永不触发，
        // 单例会通过事件委托反向持有 VM 导致泄漏。
        ModernWindowLifecycle.Release(this, _serviceScope.Dispose);
        base.OnClosed(e);
    }

}
