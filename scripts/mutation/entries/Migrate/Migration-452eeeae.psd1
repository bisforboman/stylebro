# Trial of 0.5.0-alpha.1 (2026-10-10): settings only where StyleCop runs, sort keys, the package next to StyleCop's,
# no whitespace pass with IDE0055 off, old Mac line endings, verify's own errors, kept findings marked as kept
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.Scopes.Any(scope => setup.For(scope) is var inScope'
    Replace = 'if (false && setup.Scopes.Any(scope => setup.For(scope) is var inScope'
    Tests = 'MigrationTests.ScopesThatDontSortUsings'
}
