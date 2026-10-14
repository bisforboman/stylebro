# Format speed (2026-10-14): no whitespace pass only when the exclusions cover all of the run
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (include.All(i => whitespaceExclusions.Any(e => Covers(e, i))))'
    Replace = 'if (include.Any(i => whitespaceExclusions.Any(e => Covers(e, i))))'
    Tests = 'MigrationTests.Format_ARunWhoseFilesTheWhitespaceExclusionsCover_HasNoWhitespacePass'
}
