using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HazzMusicBingo.Views;

public partial class MainWindow : Window
{
    private readonly AppDatabase _db = new();
    private LibraryScanner? _scanner;
    private GameService? _gameService;
    private CardGenerator? _cardGenerator;
    private GameFileService? _gameFileService;

    private readonly AudioClipPlayer _audio = new();


    private readonly CardDesignSettingsService _cardDesignService = new();
    private CardDesignSettings _cardDesign = new();

    private readonly AudienceDesignSettingsService _audienceDesignService = new();
    private AudienceDesignSettings _audienceDesign = new();

    private AudienceWindow? _audience;
    private long? _gameId;
    private Track? _lastTrack;
    private CancellationTokenSource? _scanCts;
    private bool _manualStopRequested;
    private int _playbackOperationId;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        Closed += (_, _) =>
        {
            _scanCts?.Cancel();
            _audio.Dispose();
            try { _audience?.Close(); } catch { }
        };
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _db.InitializeAsync();

            _scanner = new LibraryScanner(_db);
            _gameService = new GameService(_db);
            _cardGenerator = new CardGenerator(_db);
            _gameFileService = new GameFileService(_db);

            _cardDesign = _cardDesignService.Load();
            _audienceDesign = _audienceDesignService.Load();

            _gameId = await _db.GetActiveGameIdAsync();

            await RefreshStatusAsync();
            FooterText.Text = $"Database: {_db.DatabasePath}";

            EnableGameControls(_gameId.HasValue);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.ToString(),
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshStatusAsync(bool checkMusic = true)
    {
        var game = _gameId;
        var total = await _db.GetTrackCountAsync();
        LibraryCountText.Text = $"Songs indexed: {total:N0}";
        if (game.HasValue)
        {
            var played = await _db.GetPlayedCountAsync(game.Value);
            var code = await _db.GetSessionCodeAsync(game.Value);
            if (_gameId != game) return;
            PlayedCountText.Text = $"Songs played: {played} / 60";
            GameStatusText.Text = $"{played} / 60 PLAYED • {code}";
            if (checkMusic)
            {
                var pool = await _db.GetGamePoolAsync(game.Value);
                var issues = await Task.Run(() => MusicHealthService.Check(pool));
                if (_gameId == game)
                    MusicHealthText.Text = issues.Count == 0 ? "Current game: all files available" : $"Current game: {issues.Count} songs need attention — use CHECK MUSIC.";
            }
        }
        else if (!_gameId.HasValue)
        {
            MusicHealthText.Text = "";
            PlayedCountText.Text = "Songs played: 0 / 60";
            GameStatusText.Text = "No active game";
        }
    }
    private void EnableGameControls(bool enabled)
    {
        WinnerButton.IsEnabled = enabled;
        PlayNextButton.IsEnabled = enabled;
        StopButton.IsEnabled = enabled;
        PlayedSongsButton.IsEnabled = enabled;
        PrintCardsButton.IsEnabled = enabled;
        NewGameButton.IsEnabled = enabled;
        ResetGameButton.IsEnabled = enabled;
        SaveGameButton.IsEnabled = enabled;

        RepeatButton.IsEnabled =
            enabled && _lastTrack is not null;
    }

    private async void ScanFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_scanner is null)
            return;

        using var dialog = new Forms.FolderBrowserDialog
        {
            Description =
                "Select the root folder containing your music",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
            return;

        var scanButton = (System.Windows.Controls.Button)sender;
        scanButton.IsEnabled = false;
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressBar.Maximum = Math.Max(1, p.Total);
            ScanProgressBar.Value = p.Processed;
            ScanStatusText.Text =
                $"Scanning {p.Processed:N0} / {p.Total:N0}\n" +
                p.CurrentFile;
        });

        try
        {
            ScanStatusText.Text = "Finding music files...";

            var scanFolder = dialog.SelectedPath;
            var scanToken = _scanCts.Token;
            var count = await Task.Run(() => _scanner.ScanAsync(
                scanFolder,
                progress,
                scanToken), scanToken);

            ScanStatusText.Text =
                $"Scan complete. {count:N0} music files processed.";

            await RefreshStatusAsync();
        }
        catch (OperationCanceledException)
        {
            ScanStatusText.Text = "Scan cancelled.";
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                this,
                "Windows denied access to part of that folder.\n\n" +
                ex.Message,
                "Scan stopped",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Scan error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            scanButton.IsEnabled = true;
        }
    }

    private async void GenerateGame_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_gameService is null)
            return;

        if (_gameId.HasValue)
        {
            var result = MessageBox.Show(
                this,
                "Generating a new game will close the current game. Continue?",
                "Generate new game",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        try
        {
            IsEnabled = false;
            StopPlaybackUi();
            _lastTrack = null;

            _gameId =
                await _gameService.GenerateGameAsync(60);

            CurrentTitleText.Text = "Ready";
            CurrentArtistText.Text = "";
            EnableGameControls(true);
            GameStatusText.Text =
                "0 / 60 PLAYED";

            _audience?.ShowTrack(null);

            await RefreshStatusAsync();

            MessageBox.Show(
                this,
                "A new 60-song game has been generated and locked.\n\n" +
                "All playback and printed cards will use only these 60 songs.",
                "Game ready",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Could not generate game",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally { IsEnabled = true; }
    }

    private async void PlayNext_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_gameId.HasValue || _gameService is null)
            return;

        var operationId =
            ++_playbackOperationId;

        try
        {
            PlayNextButton.IsEnabled = false;
            RepeatButton.IsEnabled = false;
            _manualStopRequested = false;

            var track =
                await _gameService.GetNextTrackAsync(
                    _gameId.Value);

            if (operationId != _playbackOperationId)
                return;

            if (track is null)
            {
                MessageBox.Show(
                    this,
                    "All 60 songs in this game have been played.",
                    "Game complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var playingGameId = _gameId.Value;
            var seconds = GetClipSeconds();

            PlaybackText.Text =
                $"Playing {seconds}-second clip · smooth fade-out";

            await _audio.PlayAsync(
                track.FilePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(seconds),
                onStarted: async () =>
                {
                    await _gameService.MarkPlayedAsync(playingGameId, track);
                    if (operationId != _playbackOperationId)
                        return;
                    _lastTrack = track;
                    ShowCurrentTrack(track);
                    await RefreshStatusAsync(checkMusic: false);
                });

            // An older Repeat/Play task must never overwrite the status
            // belonging to a newer playback operation.
            if (operationId == _playbackOperationId)
            {
                PlaybackText.Text =
                    _manualStopRequested
                        ? "Stopped"
                        : "Clip finished";
            }
        }
        catch (Exception ex)
        {
            if (operationId == _playbackOperationId)
            {
                MessageBox.Show(
                    this,
                    "This track could not be played.\n\n" +
                    ex.Message,
                    "Playback error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            if (operationId == _playbackOperationId)
            {
                PlayNextButton.IsEnabled =
                    _gameId.HasValue;

                RepeatButton.IsEnabled =
                    _gameId.HasValue &&
                    _lastTrack is not null;
            }
        }
    }

    private async void Repeat_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_lastTrack is null)
            return;

        var operationId =
            ++_playbackOperationId;

        try
        {
            // Repeat itself cannot be pressed twice at once.
            RepeatButton.IsEnabled = false;

            // Keep NEXT available so the host can interrupt a repeat
            // and immediately move to the next bingo song.
            PlayNextButton.IsEnabled =
                _gameId.HasValue;

            _manualStopRequested = false;

            var repeatedTrack = _lastTrack;

            ShowCurrentTrack(repeatedTrack);

            var seconds = GetClipSeconds();

            PlaybackText.Text =
                $"Repeating {seconds}-second clip · smooth fade-out";

            await _audio.PlayAsync(
                repeatedTrack.FilePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(seconds));

            if (operationId == _playbackOperationId)
            {
                PlaybackText.Text =
                    _manualStopRequested
                        ? "Stopped"
                        : "Clip finished";
            }
        }
        catch (Exception ex)
        {
            if (operationId == _playbackOperationId)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Playback error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            if (operationId == _playbackOperationId)
            {
                PlayNextButton.IsEnabled =
                    _gameId.HasValue;

                RepeatButton.IsEnabled =
                    _gameId.HasValue &&
                    _lastTrack is not null;
            }
        }
    }

    private void StopMusic_Click(
        object sender,
        RoutedEventArgs e)
    {
        ++_playbackOperationId;

        _manualStopRequested = true;
        _audio.Stop();

        PlaybackText.Text = "Stopped";

        PlayNextButton.IsEnabled =
            _gameId.HasValue;

        RepeatButton.IsEnabled =
            _gameId.HasValue &&
            _lastTrack is not null;
    }

    private void ShowCurrentTrack(Track track)
    {
        CurrentTitleText.Text = track.Title;
        CurrentArtistText.Text = track.Artist;

        if (_audience is { IsLoaded: true })
        {
            _audience.ShowTrack(track);
        }
        else if (MonitorHelper.HasSecondaryMonitor)
        {
            EnsureAudience();
            _audience!.ShowTrack(track);
        }
    }

    private async void PlayedSongs_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_gameId.HasValue || _gameService is null)
            return;

        try
        {
            var tracks =
                await _gameService.GetPlayedAlphabeticalAsync(
                    _gameId.Value);

            // Always send the list to the audience output.
            // With display 2 connected this is full-screen there;
            // otherwise it is the normal floating preview window.
            EnsureAudience();
            _audience!.ShowPlayedSongs(tracks, 0);

            var window =
                new PlayedSongsWindow(
                    tracks,
                    pageIndex =>
                    {
                        if (_audience is { IsLoaded: true })
                            _audience.ShowPlayedSongs(tracks, pageIndex);
                    })
                {
                    Owner = this
                };

            window.ShowDialog();

            if (_audience is { IsLoaded: true })
                _audience.ShowTrack(_lastTrack);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "Played Songs could not be displayed.\n\n" + ex.Message,
                "Played Songs error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowAudience_Click(
        object sender,
        RoutedEventArgs e)
    {
        EnsureAudience();
        _audience!.Activate();
    }

    private void EnsureAudience()
    {
        if (_audience is { IsLoaded: true })
            return;

        _audience =
            new AudienceWindow(_audienceDesign);

        _audience.Closed +=
            (_, _) => _audience = null;

        _audience.Show();
    }

    private void CardDesign_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window =
            new CardDesignWindow(_cardDesign)
            {
                Owner = this
            };

        if (window.ShowDialog() == true)
        {
            _cardDesign = window.Settings.Clone();
            _cardDesignService.Save(_cardDesign);
        }
    }

    private void AudienceDesign_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window =
            new AudienceDesignWindow(_audienceDesign)
            {
                Owner = this
            };

        if (window.ShowDialog() == true)
        {
            _audienceDesign =
                window.Settings.Clone();

            _audienceDesignService.Save(
                _audienceDesign);

            if (_audience is { IsLoaded: true })
                _audience.ApplyDesign(_audienceDesign);
        }
    }

    private async void PrintCards_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_gameId.HasValue || _cardGenerator is null)
            return;

        try
        {
            PrintCardsButton.IsEnabled = false;

            await _cardGenerator
                .EnsureStrictCardsExistAsync(
                    _gameId.Value);

            var cards =
                await _db.GetCardsAsync(
                    _gameId.Value,
                    60);

            var code = await _db.GetSessionCodeAsync(_gameId.Value);
            new PrintCardsWindow(cards, _cardDesign, code) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Print error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            PrintCardsButton.IsEnabled =
                _gameId.HasValue;
        }
    }

    private async void SaveGame_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_gameId.HasValue || _gameFileService is null)
            return;

        var dialog = new WpfSaveFileDialog
        {
            Title = "Save Music Bingo Game",
            Filter = "Hazz Music Bingo Game|*.hmbgame",
            DefaultExt = ".hmbgame",
            AddExtension = true,
            FileName =
                $"Music Bingo Game {DateTime.Now:yyyy-MM-dd HH-mm}"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            await _gameFileService.SaveAsync(
                _gameId.Value,
                dialog.FileName);

            MessageBox.Show(
                this,
                "Game saved.\n\n" +
                "The file contains the exact 60-song pool, " +
                "played progress and any generated cards. " +
                "Playback order is deliberately randomized every time the game is loaded.",
                "Game saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Could not save game",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void LoadGame_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_gameFileService is null)
            return;

        if (_gameId.HasValue)
        {
            var answer = MessageBox.Show(
                this,
                "Loading a saved game will close the current active game. Continue?",
                "Load saved game",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;
        }

        var dialog = new WpfOpenFileDialog
        {
            Title = "Load Music Bingo Game",
            Filter = "Hazz Music Bingo Game|*.hmbgame|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            IsEnabled = false;
            StopPlaybackUi();

            var result =
                await _gameFileService.LoadAsync(
                    dialog.FileName);

            _gameId = result.GameId;
            _lastTrack = null;

            CurrentTitleText.Text = "Ready";
            CurrentArtistText.Text = "";
            EnableGameControls(true);
            var loadedPlayed =
                await _db.GetPlayedCountAsync(_gameId.Value);

            GameStatusText.Text =
                $"{loadedPlayed} / 60 PLAYED";

            _audience?.ShowTrack(null);

            await RefreshStatusAsync();

            var message =
                $"Saved game loaded.\n\n" +
                $"Restored cards: {result.RestoredCards}";

            if (result.MissingFiles > 0)
            {
                message +=
                    $"\n\nWarning: {result.MissingFiles} audio file(s) " +
                    "are not currently present at their saved paths.";
            }

            MessageBox.Show(
                this,
                message,
                "Game loaded",
                MessageBoxButton.OK,
                result.MissingFiles > 0
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Could not load game",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally { IsEnabled = true; }
    }

    private async void ResetGame_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_gameId.HasValue || _gameService is null)
            return;

        var played =
            await _db.GetPlayedCountAsync(_gameId.Value);

        var answer = MessageBox.Show(
            this,
            "Reset this active game back to 0 played songs?\n\n" +
            $"Currently played: {played} / 60\n\n" +
            "The same 60-song pool and existing bingo cards will be kept. " +
            "All songs will be marked unplayed and the playback order will " +
            "be freshly randomized.",
            "Reset game to 0",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            IsEnabled = false;
            StopPlaybackUi();

            await _gameService.ResetGameProgressAsync(
                _gameId.Value);

            _lastTrack = null;

            CurrentTitleText.Text = "Ready";
            CurrentArtistText.Text = "";
            PlaybackText.Text = "Ready";

            RepeatButton.IsEnabled = false;
            PlayNextButton.IsEnabled = true;

            _audience?.ShowTrack(null);

            await RefreshStatusAsync();


            MessageBox.Show(
                this,
                "Game reset to 0 played songs.\n\n" +
                "The same 60 songs and bingo cards have been kept. " +
                "A new random playback order has been created.",
                "Game reset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Could not reset game",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally { IsEnabled = true; }
    }

    private async void NewGame_Click(object sender, RoutedEventArgs e)
    {
        if (!_gameId.HasValue) return;
        try
        {
            var played = await _db.GetPlayedCountAsync(_gameId.Value);
            if (MessageBox.Show(this, $"Close this game ({played}/60 played)?\n\nYou can reopen it from GAME HISTORY. Save a .hmbgame file for a portable copy.",
                "Close game", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            IsEnabled = false;
            StopPlaybackUi();
            await _db.CloseActiveGameAsync();
            _gameId = null; _lastTrack = null;
            CurrentTitleText.Text = "Ready"; CurrentArtistText.Text = "";
            EnableGameControls(false);
            _audience?.ShowTrack(null);
            await RefreshStatusAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not close game"); }
        finally { IsEnabled = true; }
    }
    private async void CheckMusic_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StopPlaybackUi();
            new MusicHealthWindow(_db, _gameId) { Owner = this }.ShowDialog();
            await RefreshStatusAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Music check"); }
    }

    private async void Winner_Click(object sender, RoutedEventArgs e)
    {
        if (!_gameId.HasValue) return;
        try
        {
            var code = await _db.GetSessionCodeAsync(_gameId.Value);
            new WinnerWindow(_db, _gameId.Value, code) { Owner = this }.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Winner check"); }
    }

    private async void GameHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new GameHistoryWindow(_db) { Owner = this };
            if (window.ShowDialog() != true || !window.SelectedGameId.HasValue) return;
            IsEnabled = false;
            StopPlaybackUi();
            await _db.ReopenGameAsync(window.SelectedGameId.Value);
            _gameId = window.SelectedGameId;
            _lastTrack = null;
            CurrentTitleText.Text = "Ready";
            CurrentArtistText.Text = "";
            _audience?.ShowTrack(null);
            EnableGameControls(true);
            await RefreshStatusAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not recover game"); }
        finally { IsEnabled = true; }
    }
    private void StopPlaybackUi()
    {
        ++_playbackOperationId;

        _manualStopRequested = true;
        _audio.Stop();
        PlaybackText.Text = "Ready";
        PlayNextButton.IsEnabled = _gameId.HasValue;
        RepeatButton.IsEnabled = _gameId.HasValue && _lastTrack is not null;
    }

    private int GetClipSeconds()
    {
        if (ClipSecondsCombo.SelectedItem is ComboBoxItem item
            && int.TryParse(
                item.Tag?.ToString(),
                out var seconds))
        {
            return seconds;
        }

        return 25;
    }
}
