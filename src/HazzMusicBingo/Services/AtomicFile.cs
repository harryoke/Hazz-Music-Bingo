using System.IO;

namespace HazzMusicBingo.Services;

public static class AtomicFile
{
    public static async Task WriteAllTextAsync(string path, string contents)
    {
        var target = Path.GetFullPath(path);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, contents);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
