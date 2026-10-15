# Trial 22: only directives the formatter moves, not the conditional ones
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'CSharpExtensions.Kind(t) is not ('
    Replace = 'CSharpExtensions.Kind(t) is ('
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
