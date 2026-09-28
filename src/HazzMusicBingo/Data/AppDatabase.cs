using System.IO;
using Microsoft.Data.Sqlite;
using HazzMusicBingo.Models;

namespace HazzMusicBingo.Data;

public sealed class AppDatabase
{
    public string DatabasePath { get; }
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DatabasePath, ForeignKeys = true }.ToString();

    public AppDatabase(string? databasePath = null)
    {
        var folder = databasePath is not null ? Path.GetDirectoryName(Path.GetFullPath(databasePath))! : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HazzMusicBingo");

        Directory.CreateDirectory(folder);
        DatabasePath = databasePath is null ? Path.Combine(folder, "hazzmusicbingo.db") : Path.GetFullPath(databasePath);
    }

    public async Task InitializeAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();

        var sql = """
        PRAGMA journal_mode=WAL;

        CREATE TABLE IF NOT EXISTS Tracks(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FilePath TEXT NOT NULL UNIQUE,
            Title TEXT NOT NULL,
            Artist TEXT NOT NULL DEFAULT '',
            DurationSeconds REAL NOT NULL DEFAULT 0,
            LastWriteUtcTicks INTEGER NOT NULL DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS IX_Tracks_Title ON Tracks(Title);
        CREATE INDEX IF NOT EXISTS IX_Tracks_Artist ON Tracks(Artist);

        CREATE TABLE IF NOT EXISTS Games(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedUtc TEXT NOT NULL,
            Status TEXT NOT NULL,
            PoolSize INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GameTracks(
            GameId INTEGER NOT NULL,
            TrackId INTEGER NOT NULL,
            PlayOrder INTEGER NOT NULL,
            IsPlayed INTEGER NOT NULL DEFAULT 0,
            PlayedUtc TEXT NULL,
            PRIMARY KEY(GameId, TrackId),
            FOREIGN KEY(GameId) REFERENCES Games(Id),
            FOREIGN KEY(TrackId) REFERENCES Tracks(Id)
        );

        CREATE INDEX IF NOT EXISTS IX_GameTracks_Game_PlayOrder
        ON GameTracks(GameId, PlayOrder);

        CREATE TABLE IF NOT EXISTS Cards(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            GameId INTEGER NOT NULL,
            CardNumber INTEGER NOT NULL,
            UNIQUE(GameId, CardNumber),
            FOREIGN KEY(GameId) REFERENCES Games(Id)
        );

        CREATE TABLE IF NOT EXISTS CardSquares(
            CardId INTEGER NOT NULL,
            Position INTEGER NOT NULL,
            TrackId INTEGER NOT NULL,
            PRIMARY KEY(CardId, Position),
            FOREIGN KEY(CardId) REFERENCES Cards(Id),
            FOREIGN KEY(TrackId) REFERENCES Tracks(Id)
        );
        """;

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertTrackAsync(
        string filePath,
        string title,
        string artist,
        double durationSeconds,
        long lastWriteUtcTicks)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        INSERT INTO Tracks(FilePath, Title, Artist, DurationSeconds, LastWriteUtcTicks)
        VALUES($path, $title, $artist, $duration, $ticks)
        ON CONFLICT(FilePath) DO UPDATE SET
            Title = excluded.Title,
            Artist = excluded.Artist,
            DurationSeconds = excluded.DurationSeconds,
            LastWriteUtcTicks = excluded.LastWriteUtcTicks;
        """;
        cmd.Parameters.AddWithValue("$path", filePath);
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$artist", artist);
        cmd.Parameters.AddWithValue("$duration", durationSeconds);
        cmd.Parameters.AddWithValue("$ticks", lastWriteUtcTicks);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetTrackCountAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Tracks;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<List<Track>> GetRandomTracksAsync(int count)
    {
        var list = new List<Track>();
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT Id, FilePath, Title, Artist, DurationSeconds
        FROM Tracks
        ORDER BY RANDOM()
        LIMIT $count;
        """;
        cmd.Parameters.AddWithValue("$count", count);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(ReadTrack(reader));

        return list;
    }

    public async Task<long> CreateGameAsync(IReadOnlyList<Track> tracks)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        await using (var deactivate = cn.CreateCommand())
        {
            deactivate.Transaction = (SqliteTransaction)tx;
            deactivate.CommandText = "UPDATE Games SET Status='Closed' WHERE Status='Active';";
            await deactivate.ExecuteNonQueryAsync();
        }

        long gameId;
        await using (var cmd = cn.CreateCommand())
        {
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
            INSERT INTO Games(CreatedUtc, Status, PoolSize)
            VALUES($created, 'Active', $pool);
            SELECT last_insert_rowid();
            """;
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$pool", tracks.Count);
            gameId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        var shuffled = tracks.OrderBy(_ => Random.Shared.Next()).ToList();
        for (var i = 0; i < shuffled.Count; i++)
        {
            await using var cmd = cn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
            INSERT INTO GameTracks(GameId, TrackId, PlayOrder)
            VALUES($game, $track, $order);
            """;
            cmd.Parameters.AddWithValue("$game", gameId);
            cmd.Parameters.AddWithValue("$track", shuffled[i].Id);
            cmd.Parameters.AddWithValue("$order", i);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return gameId;
    }

    public async Task<long?> GetActiveGameIdAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Games WHERE Status='Active' ORDER BY Id DESC LIMIT 1;";
        var result = await cmd.ExecuteScalarAsync();
        return result is null || result is DBNull ? null : Convert.ToInt64(result);
    }

    public async Task CloseActiveGameAsync()
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE Games SET Status='Closed' WHERE Status='Active';";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Track>> GetGamePoolAsync(long gameId)
    {
        var list = new List<Track>();
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT t.Id, t.FilePath, t.Title, t.Artist, t.DurationSeconds
        FROM GameTracks gt
        JOIN Tracks t ON t.Id = gt.TrackId
        WHERE gt.GameId = $game
        ORDER BY gt.PlayOrder;
        """;
        cmd.Parameters.AddWithValue("$game", gameId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(ReadTrack(reader));

        return list;
    }

    public async Task<Track?> GetNextUnplayedTrackAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT t.Id, t.FilePath, t.Title, t.Artist, t.DurationSeconds
        FROM GameTracks gt
        JOIN Tracks t ON t.Id = gt.TrackId
        WHERE gt.GameId = $game AND gt.IsPlayed = 0
        ORDER BY gt.PlayOrder
        LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$game", gameId);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadTrack(reader) : null;
    }

    public async Task MarkTrackPlayedAsync(long gameId, long trackId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        UPDATE GameTracks
        SET IsPlayed = 1, PlayedUtc = $utc
        WHERE GameId = $game AND TrackId = $track;
        """;
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$game", gameId);
        cmd.Parameters.AddWithValue("$track", trackId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Track>> GetPlayedTracksAlphabeticalAsync(long gameId)
    {
        var list = new List<Track>();
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT t.Id, t.FilePath, t.Title, t.Artist, t.DurationSeconds
        FROM GameTracks gt
        JOIN Tracks t ON t.Id = gt.TrackId
        WHERE gt.GameId = $game AND gt.IsPlayed = 1
        ORDER BY t.Title COLLATE NOCASE, t.Artist COLLATE NOCASE;
        """;
        cmd.Parameters.AddWithValue("$game", gameId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(ReadTrack(reader));

        return list;
    }

    public async Task ResetGameProgressAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        var trackIds = new List<long>();

        await using (var read = cn.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)tx;
            read.CommandText = """
            SELECT TrackId
            FROM GameTracks
            WHERE GameId = $game;
            """;
            read.Parameters.AddWithValue("$game", gameId);

            await using var reader = await read.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                trackIds.Add(reader.GetInt64(0));
        }

        if (trackIds.Count == 0)
            throw new InvalidOperationException(
                "The active game does not contain any songs.");

        // Fresh random playback order every time the game is reset.
        var shuffled =
            trackIds.OrderBy(_ => Random.Shared.Next()).ToList();

        for (var i = 0; i < shuffled.Count; i++)
        {
            await using var cmd = cn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
            UPDATE GameTracks
            SET PlayOrder = $order,
                IsPlayed = 0,
                PlayedUtc = NULL
            WHERE GameId = $game
              AND TrackId = $track;
            """;
            cmd.Parameters.AddWithValue("$order", i);
            cmd.Parameters.AddWithValue("$game", gameId);
            cmd.Parameters.AddWithValue("$track", shuffled[i]);

            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<int> GetPlayedCountAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM GameTracks WHERE GameId=$game AND IsPlayed=1;";
        cmd.Parameters.AddWithValue("$game", gameId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetCardCountAsync(long gameId)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Cards WHERE GameId=$game;";
        cmd.Parameters.AddWithValue("$game", gameId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task SaveCardsAsync(long gameId, IReadOnlyList<BingoCard> cards)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        await InsertCardsAsync(cn, (SqliteTransaction)tx, gameId, cards);

        await tx.CommitAsync();
    }

    private static async Task InsertCardsAsync(SqliteConnection cn, SqliteTransaction tx, long gameId, IReadOnlyList<BingoCard> cards)
    {
        foreach (var card in cards)
        {
            long cardId;
            await using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = (SqliteTransaction)tx;
                cmd.CommandText = """
                INSERT INTO Cards(GameId, CardNumber)
                VALUES($game, $number);
                SELECT last_insert_rowid();
                """;
                cmd.Parameters.AddWithValue("$game", gameId);
                cmd.Parameters.AddWithValue("$number", card.CardNumber);
                cardId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            }

            for (var pos = 0; pos < card.Squares.Count; pos++)
            {
                await using var cmd = cn.CreateCommand();
                cmd.Transaction = (SqliteTransaction)tx;
                cmd.CommandText = """
                INSERT INTO CardSquares(CardId, Position, TrackId)
                VALUES($card, $position, $track);
                """;
                cmd.Parameters.AddWithValue("$card", cardId);
                cmd.Parameters.AddWithValue("$position", pos);
                cmd.Parameters.AddWithValue("$track", card.Squares[pos].Id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

    }

    public async Task<List<BingoCard>> GetCardsAsync(long gameId, int count)
    {
        var cards = new List<BingoCard>();

        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();

        await using var cardCmd = cn.CreateCommand();
        cardCmd.CommandText = """
        SELECT Id, CardNumber
        FROM Cards
        WHERE GameId=$game
        ORDER BY CardNumber
        LIMIT $count;
        """;
        cardCmd.Parameters.AddWithValue("$game", gameId);
        cardCmd.Parameters.AddWithValue("$count", count);

        var cardRows = new List<(long Id, int Number)>();
        await using (var reader = await cardCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                cardRows.Add((reader.GetInt64(0), reader.GetInt32(1)));
        }

        foreach (var row in cardRows)
        {
            var card = new BingoCard
            {
                Id = row.Id,
                GameId = gameId,
                CardNumber = row.Number
            };

            await using var squareCmd = cn.CreateCommand();
            squareCmd.CommandText = """
            SELECT t.Id, t.FilePath, t.Title, t.Artist, t.DurationSeconds
            FROM CardSquares cs
            JOIN Tracks t ON t.Id = cs.TrackId
            WHERE cs.CardId=$card
            ORDER BY cs.Position;
            """;
            squareCmd.Parameters.AddWithValue("$card", row.Id);

            await using var reader = await squareCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                card.Squares.Add(ReadTrack(reader));

            cards.Add(card);
        }

        return cards;
    }


    public async Task<List<GameTrackState>> GetGameTrackStatesAsync(long gameId)
    {
        var list = new List<GameTrackState>();

        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT t.Id, t.FilePath, t.Title, t.Artist, t.DurationSeconds,
               gt.PlayOrder, gt.IsPlayed, gt.PlayedUtc
        FROM GameTracks gt
        JOIN Tracks t ON t.Id = gt.TrackId
        WHERE gt.GameId = $game
        ORDER BY gt.PlayOrder;
        """;
        cmd.Parameters.AddWithValue("$game", gameId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new GameTrackState
            {
                Track = new Track
                {
                    Id = reader.GetInt64(0),
                    FilePath = reader.GetString(1),
                    Title = reader.GetString(2),
                    Artist = reader.GetString(3),
                    DurationSeconds = reader.GetDouble(4)
                },
                PlayOrder = reader.GetInt32(5),
                IsPlayed = reader.GetInt32(6) != 0,
                PlayedUtc = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return list;
    }

    public async Task<Track?> GetTrackByFilePathAsync(string filePath)
    {
        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
        SELECT Id, FilePath, Title, Artist, DurationSeconds
        FROM Tracks
        WHERE FilePath = $path
        LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$path", filePath);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadTrack(reader) : null;
    }

    public async Task<long> CreateGameWithStateAsync(
        IReadOnlyList<GameTrackState> states, IReadOnlyList<BingoCard>? cards = null)
    {
        if (states.Count == 0)
            throw new InvalidOperationException("The saved game contains no tracks.");

        await using var cn = new SqliteConnection(ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        await using (var deactivate = cn.CreateCommand())
        {
            deactivate.Transaction = (SqliteTransaction)tx;
            deactivate.CommandText =
                "UPDATE Games SET Status='Closed' WHERE Status='Active';";
            await deactivate.ExecuteNonQueryAsync();
        }

        long gameId;
        await using (var cmd = cn.CreateCommand())
        {
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
            INSERT INTO Games(CreatedUtc, Status, PoolSize)
            VALUES($created, 'Active', $pool);
            SELECT last_insert_rowid();
            """;
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$pool", states.Count);
            gameId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        foreach (var state in states.OrderBy(s => s.PlayOrder))
        {
            await using var cmd = cn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
            INSERT INTO GameTracks(
                GameId, TrackId, PlayOrder, IsPlayed, PlayedUtc)
            VALUES(
                $game, $track, $order, $played, $playedUtc);
            """;
            cmd.Parameters.AddWithValue("$game", gameId);
            cmd.Parameters.AddWithValue("$track", state.Track.Id);
            cmd.Parameters.AddWithValue("$order", state.PlayOrder);
            cmd.Parameters.AddWithValue("$played", state.IsPlayed ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "$playedUtc",
                (object?)state.PlayedUtc ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        if (cards is not null)
            await InsertCardsAsync(cn, (SqliteTransaction)tx, gameId, cards);

        await tx.CommitAsync();
        return gameId;
    }

    private static Track ReadTrack(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        FilePath = reader.GetString(1),
        Title = reader.GetString(2),
        Artist = reader.GetString(3),
        DurationSeconds = reader.GetDouble(4)
    };
}
