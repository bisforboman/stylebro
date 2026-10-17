# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (!propsShadowed && !editorConfigStops)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.RepositoryFolder_MovesToTheRoot_ForARootSolution_AnOutsideReference_OrNoProject'
}
