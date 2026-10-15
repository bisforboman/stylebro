# Trials

First-run trials clone public repositories here (`.trials/trialNN/<repo>`, repros in `.trials/trialNN/spot/`, logs in
`.trials/trialNN/logs/`). Everything except this README and the stub files is gitignored.

The stubs make a clone see a clean machine: MSBuild and `.editorconfig` lookups walk up the folder tree, and without
them every clone would inherit StyleBro's own `.editorconfig`, `Directory.Build.props/.targets` and
`Directory.Packages.props` (an early real-world run failed to build that way). A clone's own files still win.
