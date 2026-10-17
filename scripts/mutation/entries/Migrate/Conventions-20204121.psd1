# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'declared.Contains(name) || types.Contains(name) ||'
    Replace = 'declared.Contains(name) ||'
    Tests = 'MigrationTests.Conventions_CountUsingsRelativeToATypeOrTheRuntime'
}
