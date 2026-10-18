# Issue #177: a props that stops the root import, under which nothing uses StyleBro, gets no preset
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!usesStyleBro && File.Exists(props) && !Regex.IsMatch'
    Replace = 'if (false && File.Exists(props) && !Regex.IsMatch'
    Tests = 'MigrationTests.Issue177_Init_LeavesAPropsThatStopsTheRootImport_WhenNothingUnderItUsesStyleBro'
}
