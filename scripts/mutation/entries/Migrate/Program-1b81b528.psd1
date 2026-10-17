# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '.Any(r => !IsUnder(r, folder))'
    Replace = '.Any(r => true)'
    Tests = 'MigrationTests.RepositoryFolder_MovesToTheRoot_ForARootSolution_AnOutsideReference_OrNoProject'
}
