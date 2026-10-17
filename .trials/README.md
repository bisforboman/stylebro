# Trials

First-run trials clone public repositories here (`.trials/trialNN/<repo>`, repros in `.trials/trialNN/spot/`, logs in
`.trials/trialNN/logs/`). Everything except this README and the stub files is gitignored.

The stubs make a clone see a clean machine: MSBuild and `.editorconfig` lookups walk up the folder tree, and without
them every clone would inherit StyleBro's own `.editorconfig`, `Directory.Build.props/.targets` and
`Directory.Packages.props` (an early real-world run failed to build that way). A clone's own files still win.

## Choosing the repositories (owner's decision, 2026-10-17)

Each trial takes four repositories: three targeted at what the previous trial's fixes changed, one random.

Every repository must:

- be public on GitHub and never used before, neither in a trial nor in the real-world CI (`scripts/realworld/repos.psd1`);
  check the earlier `.trials/trialNN` folders and grep the git history (commit messages and diffs) for the name, since
  trials before 23 lived outside this repository and only their fixes record them;
- build on the trial machine with the installed SDKs (a small local workaround is fine, say so in the report; a repo
  that needs an SDK or target framework that isn't installed is out);
- have tests that run offline (leave out ones needing Docker, a network or a database, and say which);
- have tens to a few hundred C# files;
- together with the other three give a mix of setups (StyleCop / StyleSharp / Sonar / CSharpier / none, single- or
  multi-targeted, warnings as errors, apps and libraries, XAML, generators).

Procedure:

1. Targeted: for each area the last trial's fixes touched, pick a repository that exercises it (for example a XAML MVVM
   app for name guards, a multi-targeted library with `#if` in method bodies for format, a CSharpier repository with an
   in-solution generator). Smoke-build the candidates (restore + Debug build) before choosing.
2. Random: write a fixed list of about 30 popular .NET repositories ranked by GitHub stars (C#, libraries or apps;
   leave out huge ones such as dotnet/runtime, roslyn or aspnetcore, and any already used), then pick one with
   Python's `random.Random(<trial number>).randrange(len(list))`. Log the list, the seed and the pick in
   `.trials/trialNN/logs/`. Only if the pick fails the practical criteria above, take the next entry down the list
   (wrapping around) and write down why.
3. Record each repository's commit, and why it was chosen, in the trial's logs.
