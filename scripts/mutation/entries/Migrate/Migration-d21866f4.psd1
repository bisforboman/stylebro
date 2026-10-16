# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return set.Value == property ? null :'
    Replace = 'return false ? null :'
    Tests = 'MigrationTests.SetStyle_GoesBelowThePreset_Once'
}
