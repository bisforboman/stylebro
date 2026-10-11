# a hand-written Migrations folder counts; only EF Core's files are skipped, like the generated_code sections (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '            || IsEfMigration(text)'
    Replace = ''
    Tests = 'MigrationTests'
}
