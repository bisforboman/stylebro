# Trial 22: a framework that compiles like another's in a project gets no run there (CommunityToolkit)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (key(project, framework) is not { } k || keys.Add(k))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_RunsAFrameworkOnlyWhereItsCodeCompilesDifferently'
}
