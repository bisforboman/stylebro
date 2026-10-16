# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(rest.TrimStart().StartsWith("</", StringComparison.Ordinal) ? indent : indent + unit)'
    Replace = '(indent + unit)'
    Tests = 'MigrationTests.SetPreset_WritesWellFormedXml'
}
