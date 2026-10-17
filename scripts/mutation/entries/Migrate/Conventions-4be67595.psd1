# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| RuntimeNamespaces.Contains(name))'
    Replace = ')'
    Tests = 'MigrationTests.Conventions_CountUsingsRelativeToATypeOrTheRuntime'
}
