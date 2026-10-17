# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '.Any(p => IsUnder(p, folder))'
    Replace = '.Any(p => true)'
    Tests = 'MigrationTests.RepositoryFolder_MovesToTheRoot_ForARootSolution_AnOutsideReference_OrNoProject'
}
