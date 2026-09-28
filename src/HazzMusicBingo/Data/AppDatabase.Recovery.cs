using HazzMusicBingo.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace HazzMusicBingo.Data;

public sealed partial class AppDatabase
{
    private static async Task EnsureSessionCodesAsync(SqliteConnection cn)
    {
        var found = false;
        await using (var query = cn.CreateCommand())
        {
            query.CommandText = "PRAGMA table_info(Games);";
            await using var reader = await query.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (reader.GetString(1) == "SessionCode") found = true;
        }
        await using var command = cn.CreateCommand();
        if (!found)
        {
            command.CommandText = "ALTER TABLE Games ADD COLUMN SessionCode TEXT;";
            await command.ExecuteNonQueryAsync();
        }
        command.CommandText = "UPDATE Games SET SessionCode=upper(hex(randomblob(6))) WHERE SessionCode IS NULL;";
        await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetSessionCodeAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT SessionCode FROM Games WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", gameId);
        return (string?)await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Game not found.");
    }

    public async Task<List<Track>> GetAllTracksAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id, FilePath, Title, Artist, DurationSeconds FROM Tracks ORDER BY Title COLLATE NOCASE;";
        await using var reader = await cmd.ExecuteReaderAsync();
        var tracks = new List<Track>();
        while (await reader.ReadAsync()) tracks.Add(ReadTrack(reader));
        return tracks;
    }

    public async Task RelinkTrackAsync(long trackId, string path, double duration)
    {
        if (!File.Exists(path) || !double.IsFinite(duration) || duration <= 0)
            throw new ArgumentException("Choose an existing, playable audio file.");
        path = Path.GetFullPath(path);
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Tracks WHERE FilePath=$path COLLATE NOCASE AND Id<>$id;";
        cmd.Parameters.AddWithValue("$path", path);
        cmd.Parameters.AddWithValue("$id", trackId);
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
            throw new InvalidOperationException("That file is already assigned to another library song. Choose the original song's file to avoid duplicate card entries.");
        cmd.CommandText = "UPDATE Tracks SET FilePath=$path, DurationSeconds=$duration, LastWriteUtcTicks=$ticks WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$duration", duration);
        cmd.Parameters.AddWithValue("$ticks", File.GetLastWriteTimeUtc(path).Ticks);
        if (await cmd.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Track not found.");
    }

    public async Task<List<GameSummary>> GetGameHistoryAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT g.Id, g.SessionCode, g.CreatedUtc, g.Status,
              (SELECT count(*) FROM GameTracks t WHERE t.GameId=g.Id AND t.IsPlayed=1),
              (SELECT count(*) FROM Cards c WHERE c.GameId=g.Id)
            FROM Games g ORDER BY g.Id DESC;
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        var games = new List<GameSummary>();
        while (await reader.ReadAsync()) games.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt32(5)));
        return games;
    }

    public async Task ReopenGameAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        await using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT count(*) FROM GameTracks WHERE GameId=$id;";
        cmd.Parameters.AddWithValue("$id", gameId);
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 60)
            throw new InvalidOperationException("This game does not contain a complete 60-song pool and cannot be recovered.");
        cmd.CommandText = "UPDATE Games SET Status='Closed' WHERE Status='Active'; UPDATE Games SET Status='Active' WHERE Id=$id;";
        await cmd.ExecuteNonQueryAsync();
        tx.Commit();
    }

    public async Task BackupAsync(string path)
    {
        if (File.Exists(path)) throw new IOException("Choose a new filename for the database backup.");
        await using var source = new SqliteConnection(ConnectionString);
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(path) }.ToString());
        await source.OpenAsync();
        await destination.OpenAsync();
        source.BackupDatabase(destination);
    }
}
