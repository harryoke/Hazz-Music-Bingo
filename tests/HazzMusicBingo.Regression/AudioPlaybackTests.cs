using HazzMusicBingo.Services;
using NAudio.Wave;
using System.IO;

internal static class AudioPlaybackTests
{
    public static async Task<int> Run(string folder)
    {
        var checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            checks++;
        }
        var path = Path.Combine(folder, "handover.wav");
        using (var writer = new WaveFileWriter(path, new WaveFormat(44100, 16, 1)))
            for (var i = 0; i < 44100 * 5; i++) writer.WriteSample(0.5f);

        var outputs = new List<RecordingOutput>();
        using var player = new AudioClipPlayer(() =>
        {
            var output = new RecordingOutput();
            outputs.Add(output);
            return output;
        });
        var repeat = player.PlayAsync(path, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        Check(player.IsPlaying && outputs[0].ReadLevel() > 0.4f, "Repeat has audible samples");
        var started = 0;
        var next = player.PlayAsync(path, TimeSpan.Zero, TimeSpan.FromSeconds(30),
            onStarted: () => { started++; return Task.CompletedTask; });
        await repeat.WaitAsync(TimeSpan.FromSeconds(5));
        Check(outputs[0].Disposed, "Interrupted repeat releases its output");
        Check(player.IsPlaying && outputs[1].ReadLevel() > 0.4f,
            "Next song stays audible after repeat cleanup");
        Check(started == 1, "Next song reports its start exactly once");
        player.Stop();
        await next.WaitAsync(TimeSpan.FromSeconds(5));
        Check(!player.IsPlaying && outputs[1].Disposed, "Stop releases next song");

        var fade = player.PlayAsync(path, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        var fading = outputs[2];
        var initial = fading.ReadLevel();
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!fading.Disposed && fading.ReadLevel() >= initial * 0.8f && DateTime.UtcNow < timeout)
            await Task.Delay(5);
        Check(!fading.Disposed && fading.ReadLevel() < initial * 0.8f,
            "Fade reduces sample amplitude before output disposal");
        // Interrupt during the fade, not just during the steady part of a repeat.
        var afterFade = player.PlayAsync(path, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        await fade.WaitAsync(TimeSpan.FromSeconds(5));
        Check(player.IsPlaying && outputs[3].ReadLevel() > 0.4f,
            "Interrupting a fading repeat restores full sample amplitude");
        player.Stop();
        await afterFade.WaitAsync(TimeSpan.FromSeconds(5));
        await player.PlayAsync(path, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
        Check(!player.IsPlaying && outputs[4].Disposed, "Natural completion releases output");
        Check(outputs.All(o => o.VolumeWrites == 0),
            "Start, fade, interruption and cleanup never change shared device volume");
        var seekPath = Path.Combine(folder, "seek.wav");
        using (var writer = new WaveFileWriter(seekPath, new WaveFormat(44100, 16, 1)))
            for (var i = 0; i < 44100 * 61; i++) writer.WriteSample(i < 44100 * 30 ? 0.25f : i < 44100 * 60 ? 0.5f : 0.75f);
        foreach (var offset in new[] { 0, 30, 60, 90 })
        {
            TimeSpan? resolved = null;
            var notifications = 0;
            var playback = player.PlayAsync(seekPath, TimeSpan.FromSeconds(offset), TimeSpan.FromSeconds(30),
                onStarted: () => { notifications++; return Task.CompletedTask; }, onPositionResolved: value => resolved = value);
            var expected = offset == 30 ? 0.5f : offset == 60 ? 0.75f : 0.25f;
            Check(Math.Abs(outputs[^1].ReadLevel() - expected) < 0.01f, $"Decoded sample at start offset {offset}");
            Check(resolved == TimeSpan.FromSeconds(offset == 90 ? 0 : offset) && notifications == 1, $"Resolved offset and one start callback: {offset}");
            player.Stop(); await playback.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Check(PlaybackSettingsService.ResolveStart(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)) == TimeSpan.Zero, "Offset at exact end falls back to start");
        Check(!PlaybackSettingsService.IsValid(-30) && !PlaybackSettingsService.IsValid(45) && !PlaybackSettingsService.IsValid(86430), "Invalid offsets rejected");
        Check(PlaybackSettingsService.IsValid(0) && PlaybackSettingsService.IsValid(90) && PlaybackSettingsService.IsValid(86400), "Thirty-second steps accepted");
        var preferences = new PlaybackSettingsService(); preferences.Save(90);
        Check(new PlaybackSettingsService().Load() == 90, "Playback offset survives restart");
        preferences.Save(0);
        return checks;
    }

    private sealed class RecordingOutput : IWavePlayer
    {
        private IWaveProvider? _source;
        public int VolumeWrites { get; private set; }
        public bool Disposed { get; private set; }
        public float Volume { get => 1; set => VolumeWrites++; }
        public PlaybackState PlaybackState { get; private set; }
        public WaveFormat OutputWaveFormat => _source!.WaveFormat;
        public event EventHandler<StoppedEventArgs>? PlaybackStopped;
        public void Init(IWaveProvider waveProvider) => _source = waveProvider;
        public void Play() => PlaybackState = PlaybackState.Playing;
        public void Pause() => PlaybackState = PlaybackState.Paused;
        public void Stop()
        {
            PlaybackState = PlaybackState.Stopped;
            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
        }
        public void Dispose() { Stop(); Disposed = true; }
        public float ReadLevel()
        {
            var bytes = new byte[4];
            return _source!.Read(bytes, 0, 4) == 4 ? Math.Abs(BitConverter.ToSingle(bytes)) : 0;
        }
    }
}
