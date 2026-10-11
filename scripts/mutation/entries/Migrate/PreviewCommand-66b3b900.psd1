# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'rest.Remove("--no-restore");'
    Replace = ''
    Tests = 'MigrationTests.Preview_AFailedFormat'
}
