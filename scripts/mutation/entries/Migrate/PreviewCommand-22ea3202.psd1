# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (File.Exists(file) && GlobalStyleCop.IsMatch('
    Replace = 'if (false && GlobalStyleCop.IsMatch('
    Tests = 'MigrationTests.Preview_AGlobalPackageReference'
}
