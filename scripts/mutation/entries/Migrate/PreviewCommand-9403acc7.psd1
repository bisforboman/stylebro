# Trial of 0.5.0-alpha.1 (2026-10-10): settings only where StyleCop runs, sort keys, the package next to StyleCop's,
# no whitespace pass with IDE0055 off, old Mac line endings, verify's own errors, kept findings marked as kept
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'foreach (var file in removes)'
    Replace = 'foreach (var file in Array.Empty<string>())'
    Tests = 'MigrationTests.ThePackage_'
}
