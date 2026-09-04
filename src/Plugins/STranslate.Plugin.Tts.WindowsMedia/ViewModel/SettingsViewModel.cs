using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using System.ComponentModel;
using Windows.Media.SpeechSynthesis;

namespace STranslate.Plugin.Tts.WindowsMedia.ViewModel;

public partial class VoiceOptionItem(string id, string displayName, string language, string gender)
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public string Language { get; } = language;
    public string Gender { get; } = gender;
    public string FullTitle => $"{DisplayName} ({Language}, {Gender})";
}

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    [ObservableProperty] public partial VoiceOptionItem? SelectedVoice { get; set; }
    [ObservableProperty] public partial double Speed { get; set; }
    [ObservableProperty] public partial double Pitch { get; set; }
    [ObservableProperty] public partial double Volume { get; set; }

    public List<VoiceOptionItem> VoiceOptions { get; } = [];

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        Speed = settings.Speed;
        Pitch = settings.Pitch;
        Volume = settings.Volume;

        try
        {
            foreach (var voice in SpeechSynthesizer.AllVoices)
            {
                var item = new VoiceOptionItem(
                    voice.Id,
                    voice.DisplayName,
                    voice.Language,
                    voice.Gender.ToString());
                VoiceOptions.Add(item);
            }

            SelectedVoice = VoiceOptions.FirstOrDefault(v => v.Id == settings.VoiceId)
                ?? VoiceOptions.FirstOrDefault();
        }
        catch
        {
            // fallback
        }

        PropertyChanged += OnSettingsPropertyChanged;
    }

    [RelayCommand]
    private async Task TestAudioAsync()
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            if (SelectedVoice != null)
            {
                var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == SelectedVoice.Id);
                if (voice != null) synth.Voice = voice;
            }

            synth.Options.SpeakingRate = Math.Clamp(Speed, 0.5, 6.0);
            synth.Options.AudioPitch = Math.Clamp(Pitch, 0.0, 2.0);
            synth.Options.AudioVolume = Math.Clamp(Volume, 0.0, 100.0);

            var stream = await synth.SynthesizeTextToStreamAsync("你好，欢迎使用 STranslate 离线语音合成！Hello, welcome to STranslate!");
            if (stream != null && stream.Size > 0)
            {
                using var reader = new Windows.Storage.Streams.DataReader(stream);
                var bytes = new byte[stream.Size];
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
                await _context.AudioPlayer.PlayAsync(bytes);
            }
        }
        catch (Exception ex)
        {
            _context.Snackbar.ShowWarning($"测试播放失败: {ex.Message}");
            _context.Logger.LogError(ex, "测试播放失败");
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedVoice):
                _settings.VoiceId = SelectedVoice?.Id ?? string.Empty;
                break;
            case nameof(Speed):
                _settings.Speed = Speed;
                break;
            case nameof(Pitch):
                _settings.Pitch = Pitch;
                break;
            case nameof(Volume):
                _settings.Volume = Volume;
                break;
            default:
                return;
        }
        _context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= OnSettingsPropertyChanged;
}
