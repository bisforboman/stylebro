# Trial 22 (spot d2): AnalysisMode All makes every style rule a warning
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '|| !StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => Regex.IsMatch(Migration.ReadOrNull(f) ?? string.Empty, @"<AnalysisMode(?:Style)?>\s*All\s*<", RegexOptions.IgnoreCase)))'
    Replace = ')'
    Tests = 'MigrationTests.Init_ReadsTheRootGlobalConfig'
}
