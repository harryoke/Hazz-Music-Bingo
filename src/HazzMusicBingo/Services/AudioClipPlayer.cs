using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace HazzMusicBingo.Services;

public sealed class AudioClipPlayer : IDisposable
{
    private static readonly TimeSpan DefaultFadeDuration =
        TimeSpan.FromSeconds(2);

    private readonly object _sync = new();
    private PlaybackSession? _current;
    private readonly Func<IWavePlayer> _createOutput;

    public AudioClipPlayer() : this(() => new WaveOutEvent()) { }

    internal AudioClipPlayer(Func<IWavePlayer> createOutput)
    {
        _createOutput = createOutput;
    }

    public bool IsPlaying
    {
        get
        {
            lock (_sync)
            {
                try
                {
                    return _current?.Output?.PlaybackState ==
                           PlaybackState.Playing;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public async Task PlayAsync(
        string filePath,
        TimeSpan start,
        TimeSpan duration,
        CancellationToken cancellationToken = default,
        Func<Task>? onStarted = null)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));
        cancellationToken.ThrowIfCancellationRequested();
        var session =
            new PlaybackSession(
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken));

        try
        {
            // Build this playback session locally first.
            // It does not touch the currently playing session until it is ready.
            session.Reader =
                new MediaFoundationReader(filePath);

            if (start > TimeSpan.Zero &&
                start < session.Reader.TotalTime)
            {
                session.Reader.CurrentTime = start;
            }

            // Device volume can be shared between waveOut handles. Keep gain
            // in this clip's samples so retiring a repeat cannot mute its successor.
            session.Gain = new VolumeSampleProvider(session.Reader.ToSampleProvider());
            session.Output = _createOutput();
            session.Output.Init(session.Gain.ToWaveProvider());

            PlaybackSession? previous;

            lock (_sync)
            {
                previous = _current;
                _current = session;
            }

            // Cancel/stop the previous clip, but do NOT dispose its resources
            // here. Its own PlayAsync finally block owns those resources.
            CancelAndStop(previous);

            var token = session.Cts.Token;
            token.ThrowIfCancellationRequested();

            var available =
                session.Reader.TotalTime -
                session.Reader.CurrentTime;

            var actualDuration =
                available < duration
                    ? available
                    : duration;

            if (actualDuration <= TimeSpan.Zero)
                throw new InvalidOperationException("The audio file has no playable duration.");

            session.Output.Play();
            if (onStarted is not null)
                await onStarted();

            var fadeDuration =
                DefaultFadeDuration;

            // Very short clips get a shorter proportional fade.
            if (actualDuration <
                TimeSpan.FromTicks(
                    DefaultFadeDuration.Ticks * 2))
            {
                fadeDuration =
                    TimeSpan.FromTicks(
                        Math.Max(
                            TimeSpan.FromMilliseconds(250).Ticks,
                            actualDuration.Ticks / 2));
            }

            if (fadeDuration > actualDuration)
                fadeDuration = actualDuration;

            var steadyDuration =
                actualDuration - fadeDuration;

            if (steadyDuration > TimeSpan.Zero)
            {
                await Task.Delay(
                    steadyDuration,
                    token);
            }

            await FadeOutAsync(
                session.Gain,
                fadeDuration,
                token);
        }
        catch (OperationCanceledException)
        {
            // STOP or a newer playback request intentionally cancelled this clip.
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_current, session))
                    _current = null;
            }

            DisposeSession(session);
        }
    }

    private static async Task FadeOutAsync(
        VolumeSampleProvider gain,
        TimeSpan fadeDuration,
        CancellationToken token)
    {
        if (fadeDuration <= TimeSpan.Zero)
            return;

        const int steps = 40;

        var delayMs =
            Math.Max(
                5,
                fadeDuration.TotalMilliseconds / steps);

        for (var step = 0;
             step <= steps;
             step++)
        {
            token.ThrowIfCancellationRequested();

            var volume =
                1.0f - (step / (float)steps);

            try
            {
                gain.Volume =
                    Math.Clamp(
                        volume,
                        0.0f,
                        1.0f);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (step < steps)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(delayMs),
                    token);
            }
        }
    }

    public void Stop()
    {
        PlaybackSession? session;

        lock (_sync)
        {
            session = _current;
            _current = null;
        }

        // The running PlayAsync call remains responsible for disposal.
        CancelAndStop(session);
    }

    private static void CancelAndStop(
        PlaybackSession? session)
    {
        if (session is null)
            return;

        try
        {
            session.Cts.Cancel();
        }
        catch
        {
        }

        try
        {
            if (session.Output is not null)
            {
                if (session.Gain is not null) session.Gain.Volume = 0.0f;
                session.Output.Stop();
            }
        }
        catch
        {
        }
    }

    private static void DisposeSession(
        PlaybackSession session)
    {
        try
        {
            if (session.Output is not null)
            {
                try
                {
                    if (session.Gain is not null) session.Gain.Volume = 0.0f;
                    session.Output.Stop();
                }
                catch
                {
                }

                session.Output.Dispose();
                session.Output = null;
            }
        }
        finally
        {
            session.Reader?.Dispose();
            session.Reader = null;

            session.Cts.Dispose();
        }
    }

    public void Dispose() => Stop();

    private sealed class PlaybackSession
    {
        public PlaybackSession(
            CancellationTokenSource cts)
        {
            Cts = cts;
        }

        public CancellationTokenSource Cts { get; }
        public MediaFoundationReader? Reader { get; set; }
        public IWavePlayer? Output { get; set; }
        public VolumeSampleProvider? Gain { get; set; }
    }
}
