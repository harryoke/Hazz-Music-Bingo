using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using MessageBox = System.Windows.MessageBox;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;

namespace HazzMusicBingo.Views;

public partial class MainWindow
{
    private readonly GameShortcutService _shortcuts = new();
    private List<GameShortcut?> _gameButtons = Enumerable.Repeat<GameShortcut?>(null, 8).ToList();

    private void RenderGameButtons()
    {
        GameButtonsPanel.Children.Clear();
        for (var index = 0; index < 8; index++)
        {
            var number = index;
            var slot = _gameButtons[index];
            var color = slot is null ? System.Windows.Media.Color.FromRgb(45, 57, 79) : (System.Windows.Media.Color)ColorConverter.ConvertFromString(slot.Color);
            var foreground = color.R * .299 + color.G * .587 + color.B * .114 > 150 ? Brushes.Black : Brushes.White;
            var active = slot?.ResumeGameId is long id && id == _gameId;
            var button = new Button
            {
                Background = new SolidColorBrush(color), BorderBrush = active ? Brushes.Gold : Brushes.SlateGray,
                BorderThickness = new Thickness(active ? 3 : 1), Padding = new Thickness(8, 5, 8, 5),
                Content = new TextBlock { Text = $"{index + 1}  {(active ? "▶ " : "")}{slot?.Label ?? "Assign game…"}",
                    Foreground = foreground, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxHeight = 42 },
                ToolTip = slot is null ? "Click to assign a saved game and audience theme"
                    : $"{slot.Label} — click to resume. Right-click to edit, capture audience style or clear."
            };
            button.Click += async (_, _) => { if (slot is null) await EditGameButtonAsync(number); else await ActivateGameButtonAsync(number); };
            var menu = new ContextMenu();
            var edit = new MenuItem { Header = "Assign / edit game button…" };
            edit.Click += async (_, _) => await EditGameButtonAsync(number); menu.Items.Add(edit);
            if (slot is not null)
            {
                var theme = new MenuItem { Header = "Store current audience style" };
                theme.Click += async (_, _) => await EditGameButtonAsync(number, true); menu.Items.Add(theme);
                var clear = new MenuItem { Header = "Clear button" };
                clear.Click += async (_, _) =>
                {
                    try
                    {
                        var updated = _gameButtons.ToList(); updated[number] = null;
                        await _shortcuts.SaveAsync(updated); _gameButtons = updated; RenderGameButtons();
                    }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Game buttons"); }
                };
                menu.Items.Add(clear);
            }
            button.ContextMenu = menu;
            GameButtonsPanel.Children.Add(button);
        }
    }

    private async Task EditGameButtonAsync(int index, bool captureTheme = false)
    {
        try
        {
            var old = _gameButtons[index];
            var dialog = new GameShortcutWindow(old, _audienceDesign) { Owner = this };
            if (!captureTheme && dialog.ShowDialog() != true) return;
            IsEnabled = false;
            var slot = await _shortcuts.PrepareAsync(captureTheme ? old!.GameFile : dialog.GameFile,
                captureTheme ? old!.Label : dialog.ButtonLabel, captureTheme ? old!.Color : dialog.ButtonColor,
                captureTheme ? _audienceDesign : dialog.Audience);
            if (old is not null && (captureTheme || string.Equals(old.GameFile, dialog.GameFile, StringComparison.OrdinalIgnoreCase)))
            { slot.ResumeGameId = old.ResumeGameId; slot.ResumeSessionCode = old.ResumeSessionCode; }
            var updated = _gameButtons.ToList(); updated[index] = slot;
            await _shortcuts.SaveAsync(updated); _gameButtons = updated; RenderGameButtons();
            if (slot.ResumeGameId == _gameId && _gameId.HasValue)
            { _audienceDesign = slot.Audience.Clone(); _audienceDesignService.Save(_audienceDesign); _audience?.ApplyDesign(_audienceDesign); }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save game button"); }
        finally { IsEnabled = true; }
    }

    private async Task ActivateGameButtonAsync(int index)
    {
        var slot = _gameButtons[index];
        if (slot is null) return;
        try
        {
            IsEnabled = false;
            StopPlaybackUi();
            var result = await _shortcuts.ActivateAsync(_db, slot);
            _gameId = result.GameId; _lastTrack = null;
            _audienceDesign = slot.Audience.Clone();
            CurrentTitleText.Text = "Ready"; CurrentArtistText.Text = "";
            _audience?.ApplyDesign(_audienceDesign); _audience?.ShowTrack(null);
            EnableGameControls(true);
            await RefreshStatusAsync();
            // UI is already consistent with the database if saving preferences fails.
            await _shortcuts.SaveAsync(_gameButtons);
            _audienceDesignService.Save(_audienceDesign);
            if (result.MissingFiles > 0)
                MessageBox.Show(this, $"{result.MissingFiles} music file(s) need attention. Use CHECK MUSIC / LOCATE FILES.", slot.Label);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Game button"); }
        finally { IsEnabled = true; }
    }
}
