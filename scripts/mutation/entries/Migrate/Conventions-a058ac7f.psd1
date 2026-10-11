# Trial of 0.4.0-alpha.1 (2026-10-10): rules off without a majority, few places, nested Directory.Build.props, vendored code
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'else if (total < MinimumAgreeing)'
    Replace = 'else if (false)'
    Tests = 'MigrationTests'
}
