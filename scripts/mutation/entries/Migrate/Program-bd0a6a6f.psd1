# Trial 18: a StyleCop reference in a submodule is named, with the swap advice (Fonts)
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (setup.ReferencedOutside.Count > 0 && setup.RemovedIn.Count == 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Migrate_NamesAStyleCopReferenceInASubmodule_AndRecognisesTheSwap'
}
