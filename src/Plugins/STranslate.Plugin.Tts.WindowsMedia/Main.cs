using STranslate.Plugin;
using STranslate.Plugin.Tts.WindowsMedia.View;
using STranslate.Plugin.Tts.WindowsMedia.ViewModel;
using System.IO;
using System.Windows.Controls;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace STranslate.Plugin.Tts.WindowsMedia;

public class Main : ITtsPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public void Dispose() => _viewModel?.Dispose();

    public async Task PlayAudioAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        using var synth = new SpeechSynthesizer();

        if (!string.IsNullOrEmpty(Settings.VoiceId))
        {
            var selectedVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == Settings.VoiceId);
            if (selectedVoice != null)
            {
                synth.Voice = selectedVoice;
            }
        }

        synth.Options.SpeakingRate = Math.Clamp(Settings.Speed, 0.5, 6.0);
        synth.Options.AudioPitch = Math.Clamp(Settings.Pitch, 0.0, 2.0);
        synth.Options.AudioVolume = Math.Clamp(Settings.Volume, 0.0, 100.0);

        var speechStream = await synth.SynthesizeTextToStreamAsync(text);
        if (speechStream == null || speechStream.Size == 0)
            return;

        using var dataReader = new DataReader(speechStream);
        var bytes = new byte[speechStream.Size];
        await dataReader.LoadAsync((uint)speechStream.Size);
        dataReader.ReadBytes(bytes);

        await Context.AudioPlayer.PlayAsync(bytes, cancellationToken);
    }
}
