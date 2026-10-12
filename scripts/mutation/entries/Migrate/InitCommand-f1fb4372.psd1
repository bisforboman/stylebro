# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Where(f => StyleCopSetup.EnumerateFiles(Path.Combine(root, f), ownRoot: true).Any(IsCSharp))'
    Replace = '.Where(f => f.Length > 0 || IsCSharp(f))'
    Tests = 'MigrationTests.Init_FolderWithItsOwnRootEditorConfig_'
}
