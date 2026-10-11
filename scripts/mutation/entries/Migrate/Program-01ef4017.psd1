# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'PreviewCommand.ReferencesStyleBro(root) ? FormatHint : AddPackageHint'
    Replace = 'FormatHint'
    Tests = 'MigrationTests.NextStep_NamesStyleBroFormat'
}
