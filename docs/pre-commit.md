# Pre-commit hook

A pre-commit hook runs StyleBro on the C# files you're about to commit, so a style problem never reaches CI. It uses
`stylebro-migrate format --files` (see [Scripts and AI agents](agents.md#format-files)), which loads only the projects
those files belong to.

There are two kinds of hook:

- **Check only** (recommended for a hand-written hook): `--verify-no-changes` changes nothing. When formatting would
  change a staged file, the commit stops and says to run `stylebro-migrate format`.
- **Fix and stage**: format the staged files and `git add` them again. Husky.Net and lefthook do this safely, also for
  files you staged only partly. A hand-written hook can't do that easily (see
  [Partly staged files](#partly-staged-files)).

The examples call `stylebro-migrate` as a global tool (`dotnet tool install -g StyleBro.Migrate --prerelease`). With
the tool in the repository's tool manifest (`dotnet tool install StyleBro.Migrate --prerelease`), write
`dotnet stylebro-migrate` instead.

Every recipe here was tested on a repository with a multi-targeted library (stylebro-migrate 0.7.0-alpha.1,
Husky.Net 0.9.1, lefthook 2.2.2, Git Bash on Windows).

## A plain git hook

Save it as `.git/hooks/pre-commit`. It's POSIX sh, so it also runs in Git Bash on Windows (git runs hooks with it). On
Linux and macOS, make it executable too: `chmod +x .git/hooks/pre-commit`.

### Check only (recommended)

```sh
#!/bin/sh
# StyleBro: fail the commit when formatting would change a staged C# file.
git diff --cached --name-only --diff-filter=ACMR -z -- '*.cs' |
  xargs -0 -r stylebro-migrate format --verify-no-changes --files || {
  echo "StyleBro: formatting would change the files above." >&2
  echo "Run 'stylebro-migrate format --files <file>...' (or 'stylebro-migrate format'), review, git add, commit again." >&2
  exit 1
}
```

A commit stopped by it looks like this:

```
C:\src\shop\src\Orders\My Order.cs(5,36): warning BRO1509: Put 'Empty' on separate lines
Not clean: formatting would change 1 file. Run 'stylebro-migrate format'.
StyleBro: formatting would change the files above.
Run 'stylebro-migrate format --files <file>...' (or 'stylebro-migrate format'), review, git add, commit again.
```

It never writes a file, so edits you didn't stage stay unstaged. It checks the files as they are in your working
folder, though, not the staged version: an unstaged edit that needs formatting stops the commit too, and a staged
problem that an unstaged edit already fixed slips through (CI still catches that).

### Fix and stage

```sh
#!/bin/sh
# StyleBro: format the staged C# files, then stage the result.
staged() { git diff --cached --name-only --diff-filter=ACMR -z -- '*.cs'; }

# 'git add' would also stage edits you left unstaged: stop instead.
partial=$(staged | xargs -0 -r git --literal-pathspecs diff --name-only --)
if [ -n "$partial" ]; then
  echo "StyleBro: these files also have unstaged changes:" >&2
  echo "$partial" >&2
  echo "Stage the whole files (git add), or set the other edits aside, then commit again." >&2
  exit 1
fi

staged | xargs -0 -r stylebro-migrate format --files || exit 1
staged | xargs -0 -r git --literal-pathspecs add --
```

The commit then contains the formatted files. A file you staged only partly stops the commit instead: re-adding it
would stage your other edits too.

## Husky.Net

[Husky.Net](https://alirezanet.github.io/Husky.Net/) is a .NET tool. Set it up once:

```
dotnet new tool-manifest
dotnet tool install Husky
dotnet tool install StyleBro.Migrate --prerelease
dotnet husky install
dotnet husky add pre-commit -c "dotnet husky run --group pre-commit"
```

Then add the task to `.husky/task-runner.json`:

```json
{
  "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
  "tasks": [
    {
      "name": "stylebro",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["stylebro-migrate", "format", "--files", "${staged}"],
      "include": ["**/*.cs"]
    }
  ]
}
```

`${staged}` stages the files again after the task. For a partly staged file, Husky.Net writes the staged version to a
temporary copy next to it (`1a2b3_Order.cs`), formats both, and stages the formatted copy's content: the commit gets
the formatted staged version, and your other edits stay unstaged (the working file is formatted as well). While the
task runs, the project compiles both copies of that file (the same types twice), so a fix that needs the semantic
model, like a rename, may not apply to it; the check in CI shows anything left.

For a check only, add `"--verify-no-changes"` to `args`.

## lefthook

[lefthook](https://lefthook.dev/) is a single binary (from its GitHub releases, npm, Homebrew, ...). `lefthook.yml`:

```yaml
pre-commit:
  commands:
    stylebro:
      glob: "*.cs"
      run: stylebro-migrate format --files {staged_files}
      stage_fixed: true
```

Then `lefthook install`. `stage_fixed: true` runs `git add` on the files after the command. For partly staged files,
lefthook sets the unstaged edits aside before the command and puts them back afterwards, so the commit gets the
formatted staged version and your other edits stay unstaged. For a check only, add `--verify-no-changes` and drop
`stage_fixed`.

## Partly staged files

`git add -p` stages part of a file. Re-adding the file after formatting would stage the rest too, and the commit would
contain edits you meant to leave out.

| Hook | Partly staged file |
|---|---|
| Plain hook, check only | Nothing is written; the commit stops if the working file needs formatting. |
| Plain hook, fix and stage | Stops and names the file. |
| Husky.Net (`${staged}`) | Formats the staged version in a temporary copy; other edits stay unstaged. |
| lefthook (`stage_fixed`) | Sets other edits aside while formatting; they stay unstaged. |

Auto-fixing is fine with Husky.Net and lefthook, and with the plain hook when you always stage whole files.

## Speed

Every commit with C# files loads the projects they belong to with MSBuild. Measured on a small library (three target
frameworks) and its test project:

| Commit | Time |
|---|---|
| No C# files staged | instant (nothing runs) |
| Check only, clean | 7 to 12 s |
| Check only, a file needs formatting | about 25 s (a second run on a copy tells [kept findings](getting-started.md#findings-kept-on-purpose) apart) |
| Fix and stage | 20 to 35 s (formats until a run changes nothing) |

A project with several target frameworks is formatted once per framework, so each framework adds a load. Files of
several projects cost one run per project. Large solutions take longer: see the measurements in
[Scripts and AI agents](agents.md#format-files). If that's too slow for every commit, use the check-only hook and
leave the rest to [CI](ci.md).

Findings StyleBro keeps on purpose (a rename that would break reflection, ...) don't fail `--verify-no-changes` and
aren't changed by a fix run, so they never stop a commit. They stay build warnings.

## File names with spaces

The plain hooks pass file names with `git diff -z` and `xargs -0`, so spaces and other unusual characters survive
(`git --literal-pathspecs` keeps `[` and `*` in a name from being read as patterns). Don't loop over
`$(git diff --name-only)`: the shell splits names at spaces. Husky.Net passes each file as its own argument, and lefthook
quotes the names it puts into `{staged_files}`. All four were tested with `src/My Helper.cs`.
