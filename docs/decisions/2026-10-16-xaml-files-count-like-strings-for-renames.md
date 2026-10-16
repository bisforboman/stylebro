# XAML files count like strings for renames (2026-10-16)

## Question

Trial 26: BRO1309 renamed `public string title` to `Title` in an internal view model while `MainWindow.xaml` bound
`{Binding title}` (and `DisplayMemberPath="label"`). The build and every check passed, but binding paths are
case-sensitive and resolved at run time: the window showed nothing. The renamer reads C# strings and snapshot files,
not XAML. Should names used in XAML keep a rename back?

## Choices

1. **Read XAML as strings**: the renamer reads `.xaml` (WPF, WinUI, MAUI) and `.axaml` (Avalonia) files under each
   project's repository, and an identifier in an attribute value (binding paths and their segments, `Path=`,
   `DisplayMemberPath`, `SelectedValuePath`, `x:Name`, `x:Static`, event handlers, `x:Class`) keeps a member or type
   with that name, with the kept reason `NameInXamlFile`.
2. Parse bindings exactly (markup extensions, data types): more precise, much more code, and still blind to bindings
   built in code.
3. Leave it: the user finds the empty binding at run time.

## Decision

**Choice 1** (owner's choice 2026-10-16: "Read XAML as strings"). Namespace declarations (`xmlns:*`) and element and
attribute names don't count; private fields don't either (XAML can't reach them, and a word in a `Text="..."` value
would keep common names). Files under `bin`, `obj`, `.git`, `.vs` and `node_modules` and files over 1 MB are skipped,
like snapshot files; the same disk scan reads both, so a `stylebro-migrate format` run that loads only some projects
still sees every XAML file in the repository. A property kept this way keeps its type's other properties too, like a
property kept by a string (a view model's casing stays consistent).
