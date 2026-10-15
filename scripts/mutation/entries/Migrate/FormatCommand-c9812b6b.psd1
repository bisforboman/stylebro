# Trial 22: only string lines another parse reads as a directive
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'strings.Intersect(directives)'
    Replace = 'strings'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
