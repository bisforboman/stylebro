# EasyNetQ/EasyNetQ (trial 52, targeted; random.json entry 54)

Commit c1dd7f2e3d34ee3b06e67a7551f7d0a9d442ff27. StyleBro main 8247120, packed as 0.8.0-trial52.1.
Lens (trial 51's fixes): five test projects (the per-run solution filter), Source/Directory.Build.props and
Source/Directory.Packages.props with none at the root (where the preview puts the reference), EasyNetQ multi-targeted
netstandard2.0;net8.0;net9.0;net10.0 with net8.0 tests.
Setup: path A, step 1 in Source/Directory.Build.props (version in Source/Directory.Packages.props), `bro init --write`.

## Baseline (untouched)

Debug build green; TWAE Debug/Release green (the repository sets TreatWarningsAsErrors false); tests EasyNetQ.Tests 443,
Serialization.Tests 14, ApprovalTests 3; Hosepipe.Tests 17 with 8-12 failing untouched, varying run to run (all 12 names
seen failing untouched in three extra runs: they need RabbitMQ); IntegrationTests left out (RabbitMQ in Docker).

## Default engine, first round

preview init 0 (187 files, clean after 1 run; the reference went into Source/Directory.Build.props and its version into
Source/Directory.Packages.props: right), init --write 0, format 0 (two runs: netstandard2.0 and net8.0, net9.0/net10.0
compile the same; 187 files + settings, run 2 clean; xunit.analyzers 1.27.0 and IDE0049 honestly "Not checked"),
check 0 Clean, check --build 0 Clean, TWAE Debug/Release green, tests as untouched (Hosepipe's flaky set aside).
Conventions read right (e.g. ')' of split lists: 711 of 762 on its own line -> own_line). The host run of the first round
was cut off by a worker restart; the re-check covers it.

No findings. Trial 51's lenses held: all five test projects loaded in the net8.0 run, the preview's reference landed in
the props the projects import.
