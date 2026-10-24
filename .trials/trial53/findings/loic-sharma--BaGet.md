# loic-sharma/BaGet (trial 53, targeted)

Commit 5a8502ca4e3b21acd369d4a98c2529dac2d81cfe. StyleBro main 241bca8 (after #300), packed as 0.8.0-trial53.1.
Setup: path A, step 1 in `src/Directory.Build.props` and `tests/Directory.Build.props` (none at the root; the two samples
projects have no props), then `bro init --write`. Local workaround: the trial feed in its nuget.config (`<clear/>`),
`DOTNET_ROLL_FORWARD=Major` (netcoreapp3.1).

## Baseline (untouched)

Debug build green; TWAE Debug/Release green. Tests (Debug and Release): Core 117/117, Protocol 46/46, Web 20/20,
BaGet.Tests 19/66 (47 FormatException "The format of value '' is invalid"), Protocol.Samples.Tests 6/8 (nuget.org).

## The lens (trial 52's committed .nupkg fix)

`tests/BaGet.Tests/TestData/` holds TestData.1.2.3.nupkg/.snupkg next to TestData.Designer.cs and TestData.resx and no
project: `IsExtractedPackage` still calls it a package folder and init/format skip it. No effect here: the only C# file
there is a `.Designer.cs`, generated code that init doesn't count and format doesn't touch (init counted 248 files =
250 outside Migrations/ minus the 2 designer files).

## Default engine

preview 0 (71 files, clean after 1 run), init 0, format 0 (70 files, clean on run 2), check 0 Clean (the cached
verdict of the clean format), check --build 0 Clean (Debug, Release); TWAE Debug/Release green; tests as the baseline.
Init put `<StyleBroPreset>` into src/ and tests/Directory.Build.props and a new root Directory.Build.props (only the two
samples projects get it; they have no StyleBro, and format says so).

## Finding 1: a minority of BOM files against `charset = utf-8` is converted without a word

`.editorconfig` `[*] charset = utf-8`; 16 of the 248 C# files init counts start with a BOM (committed that way, nothing
enforced it). Init turns IDE0055 on, and `bro format` removed the 16 BOMs (preview: "CHARSET 16 files"), whole-file
changes init and format don't announce. The same situation as trial 52's error-or for end_of_line (owner: "Warn only",
`Conventions.ConvertedMinorityNote`), which covers only end_of_line: `ContradictedFileSetting` handles charset and
insert_final_newline only when the files OUTVOTE the key. Asked the owner.
