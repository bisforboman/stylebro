# Trial 18: after the swap, the version the last --write recorded means migrated, not leftovers (Fonts)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var installed = recordedVersion is not null ||'
    Replace = 'var installed = recorded is not null ||'
    Tests = 'MigrationTests.Migrate_NamesAStyleCopReferenceInASubmodule_AndRecognisesTheSwap'
}
