# v.0.1 validation record

- Release solution build: zero warnings and zero errors.
- Regression suite: **273 assertions passed**, including 20 independently generated 60-card sets and 24 print-option combinations.
- WPF rendering: card designer, classic/midnight cards, four-card sheet, print preview and host console rendered and inspected. Print viewer pages were loaded through WPF's dispatcher.
- Standalone Windows x64 self-test: passed native SQLite loading, 60-card generation, 15 four-card pages, archive round trip and Media Foundation WAV decoding.
- Packaged native SQLite: **3.53.3**. Packaged .NET runtime: **8.0.31**.
- Dependency audit: no known vulnerable packages reported by configured NuGet sources after updating SQLite dependencies.

The suite and standalone tests use new temporary databases. They do not modify an existing user library. No physical audio output, printer job or real second-monitor session is claimed here. See [TESTING.md](TESTING.md) for the event-machine acceptance steps.

To exercise the published executable without opening the host window:

```powershell
$report = Join-Path $PWD 'standalone-test.json'
$process = Start-Process -FilePath '.\HazzMusicBingo.exe' -ArgumentList @('--self-test', ('"' + $report + '"')) -WindowStyle Hidden -PassThru
$process.WaitForExit()
$process.ExitCode
Get-Content $report
```

Exit code 0 and a JSON `PASS` result indicate success. This mode creates temporary test data, decodes generated silence without playing sound, and does not submit print jobs. It is a packaging check, not the entire regression suite. The optional second argument is the report path; without it the report is written in the temporary test folder.

GitHub's [Actions page](https://github.com/harryoke/Hazz-Music-Bingo/actions) records the clean Windows build, regression and packaged-executable checks for each published source commit.
