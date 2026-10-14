# Format speed (2026-10-14): an exclusion inside a run's folder still needs the whitespace pass on its own
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Covers(e, i) || Covers(i, e))) ? Passes(whitespaceExclusions) : Passes(null);'
    Replace = 'Covers(e, i))) ? Passes(whitespaceExclusions) : Passes(null);'
    Tests = 'MigrationTests.Format_ARunWhoseFilesTheWhitespaceExclusionsCover_HasNoWhitespacePass'
}
