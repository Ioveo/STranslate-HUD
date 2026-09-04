using CommunityToolkit.Mvvm.ComponentModel;
using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.Services;

public partial class OcrService : BaseService
{
    private readonly ServiceSettings _serviceSettings;

    [ObservableProperty] public partial Service? ImageTranslateOcrService { get; set; }

    protected override ServiceType ServiceType => ServiceType.OCR;

    public OcrService(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginService PluginService,
        ServiceSettings serviceSettings,
        Internationalization i18n
    ) : base(pluginManager, serviceManager, PluginService, serviceSettings, i18n)
    {
        _serviceSettings = serviceSettings;

        LoadPlugins<IOcrPlugin>();
        LoadServices<IOcrPlugin>();
        InitialOtherService();
    }

    private void InitialOtherService()
    {
        ImageTranslateOcrService = Services.FirstOrDefault(s =>
            s.ServiceID == _serviceSettings.ImageTranslateOcrSvcID &&
            IsImageTranslateOcrService(s));

        if (ImageTranslateOcrService == null)
        {
            var defaultOcr = Services.FirstOrDefault(s => s.IsEnabled && IsImageTranslateOcrService(s));
            if (defaultOcr != null)
            {
                ActiveImTranOcr(defaultOcr);
            }
        }
    }

    public override async Task<bool> DeleteAsync(Service service)
    {
        var result = await base.DeleteAsync(service);
        if (result && service == ImageTranslateOcrService)
        {
            ImageTranslateOcrService = null;
            _serviceSettings.ImageTranslateOcrSvcID = string.Empty;
            _serviceSettings.Save();
        }

        return result;
    }

    public IOcrPlugin? GetActiveOrFallbackOcr()
    {
        var active = GetActiveSvc<IOcrPlugin>();
        if (active != null) return active;

        var fallbackMeta = Plugins.FirstOrDefault(p => p.PluginType != null && typeof(IOcrPlugin).IsAssignableFrom(p.PluginType));
        if (fallbackMeta != null)
        {
            var svc = AddFromPlugin(fallbackMeta);
            svc.IsEnabled = true;
            return svc.Plugin as IOcrPlugin;
        }

        return null;
    }

    internal void ActiveImTranOcr(Service svc)
    {
        if (!IsImageTranslateOcrService(svc))
            return;

        ImageTranslateOcrService = svc;
        _serviceSettings.ImageTranslateOcrSvcID = svc.ServiceID;
        _serviceSettings.Save();
    }

    internal void DeactiveImTranOcr()
    {
        ImageTranslateOcrService = null;
        _serviceSettings.ImageTranslateOcrSvcID = string.Empty;
        _serviceSettings.Save();
    }

    internal Service? GetImageTranslateOcrServiceOrDefault()
        => IsImageTranslateOcrService(ImageTranslateOcrService)
            ? ImageTranslateOcrService
            : Services.FirstOrDefault(s => s.IsEnabled && IsImageTranslateOcrService(s));

    internal IOcrPlugin? GetImageTranslateOcrSvcOrDefault()
        => GetImageTranslateOcrServiceOrDefault()?.Plugin as IOcrPlugin;

    internal bool IsImageTranslateOcrService(Service? service) =>
        service?.Plugin is IOcrPlugin plugin && plugin.SupportBoxPoints();

    internal IEnumerable<Service> GetImageTranslateOcrServices() =>
        Services.Where(IsImageTranslateOcrService);
}
