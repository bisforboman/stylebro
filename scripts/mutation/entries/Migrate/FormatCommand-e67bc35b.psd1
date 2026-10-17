# Trial 28: the restore warning, shown relative and without the project tag
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Select(m => $"{Relative(root, m.Groups["path"].Value)}: {m.Groups["id"].Value}: {m.Groups["message"].Value.TrimEnd(''.'')}.")'
    Replace = '.Select(m => m.Value)'
    Tests = 'MigrationTests.Format_NamesTheRestoreWarning_ForWorkspaceWarnings'
}
