# Third-party components

Hazz Music Bingo uses the following components. Their notices and licence texts are in `licenses/`. These licences apply to the named third-party components, not automatically to Hazz Music Bingo's own source.

| Component | Version / licence | Project |
| --- | --- | --- |
| NAudio and its component packages | 2.2.1, MIT | https://github.com/naudio/NAudio |
| Microsoft.Data.Sqlite / Core | 8.0.31, MIT | https://github.com/dotnet/efcore |
| SQLitePCLRaw bundle, provider and core | 2.1.13, Apache-2.0; Copyright SourceGear, LLC | https://github.com/ericsink/SQLitePCL.raw |
| Native SQLite | Bundled by SQLitePCLRaw; SQLite public-domain dedication | https://sqlite.org/copyright.html |
| .NET runtime / Windows Desktop runtime and support libraries | 8.0.31; Microsoft licence and third-party notices included | https://github.com/dotnet/runtime and https://github.com/dotnet/wpf |

The standalone Windows executable embeds runtime and library code. Keep this file and the `licenses` directory with redistributed binary packages. No audio recordings or background artwork are bundled. Song/artist names in the designer are illustrative labels.

## TagLibSharp 2.3.0

MP3 metadata is read using TagLibSharp (https://github.com/mono/taglib-sharp), LGPL-2.1. Copyright and author information are retained in licenses/TagLibSharp-AUTHORS.txt and the included corresponding source archive. The library is unmodified. Its licence is in licenses/TagLibSharp-LGPL-2.1.txt and complete upstream source for tag TaglibSharp-2.3.0.0 (b5ae84f2e84087bf160bb0471420200dd2b5d809) is in licenses/TagLibSharp-2.3.0-source.zip.

TagLibSharp.dll is distributed alongside the executable, separately from the single-file bundle, so it can be replaced with a compatible modified library. Keep the DLL alongside the EXE. Hazz's licence does not restrict rights granted by the LGPL, including modification/replacement of this library and reverse engineering for debugging such modifications.
