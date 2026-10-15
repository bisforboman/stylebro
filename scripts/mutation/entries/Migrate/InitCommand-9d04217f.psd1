# Trial 22 (spot d2): AnalysisMode All makes every style rule a warning
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '|| !StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => Regex.IsMatch(File.ReadAllText(f), @"<AnalysisMode(?:Style)?>\s*All\s*<", RegexOptions.IgnoreCase)))'
    Replace = ')'
    Tests = 'MigrationTests.Init_ReadsTheRootGlobalConfig'
}
