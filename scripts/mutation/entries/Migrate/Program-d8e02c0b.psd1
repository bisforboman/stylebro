# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (projects.Count == 0)'
    Replace = 'if (projects.Count < 0)'
    Tests = 'MigrationTests.RepositoryFolder_MovesToTheRoot_ForARootSolution_AnOutsideReference_OrNoProject'
}
