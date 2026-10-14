# Format speed (2026-10-14): a project at the root includes everything
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'outer == "**/*.cs" || (outer.EndsWith'
    Replace = 'outer == "**/*.cs!" || (outer.EndsWith'
    Tests = 'MigrationTests.Format_ARunWhoseFilesTheWhitespaceExclusionsCover_HasNoWhitespacePass'
}
