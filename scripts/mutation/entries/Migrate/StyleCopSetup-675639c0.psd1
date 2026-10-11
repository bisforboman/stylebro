# Trial of 0.5.0-alpha.1 (2026-10-10): settings only where StyleCop runs, sort keys, the package next to StyleCop's,
# no whitespace pass with IDE0055 off, old Mac line endings, verify's own errors, kept findings marked as kept
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'else if (covered.Count == folders.Count)'
    Replace = 'else if (false)'
    Tests = 'MigrationTests.Settings_'
}
