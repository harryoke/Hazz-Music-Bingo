using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace HazzMusicBingo.Data;

public sealed partial class AppDatabase
{
    public async Task<WinningSettings> GetWinningSettingsAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Settings FROM GameWinningSettings WHERE GameId=$game";
        cmd.Parameters.AddWithValue("$game", gameId);
        var json = await cmd.ExecuteScalarAsync() as string;
        return json is null ? new() : JsonSerializer.Deserialize<WinningSettings>(json) ?? new();
    }

    public async Task SaveWinningSettingsAsync(long gameId, WinningSettings settings)
    {
        LiveWinnerService.Validate(settings, (await GetCardsAsync(gameId, 1000)).Select(c => c.CardNumber));
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await WriteWinningSettingsAsync(cn, null, gameId, settings);
    }

    private static async Task WriteWinningSettingsAsync(SqliteConnection cn, SqliteTransaction? tx, long gameId, WinningSettings settings)
    {
        await using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO GameWinningSettings(GameId,Settings) VALUES($game,$settings) ON CONFLICT(GameId) DO UPDATE SET Settings=excluded.Settings";
        cmd.Parameters.AddWithValue("$game", gameId);
        cmd.Parameters.AddWithValue("$settings", JsonSerializer.Serialize(settings));
        await cmd.ExecuteNonQueryAsync();
    }
}
