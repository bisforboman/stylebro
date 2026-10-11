# Trial 8, tool (2026-10-11): --once isn't passed on to 'dotnet format' in --files mode
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'args = ProjectAsPath(args.Where(a => a != OnceOption).ToArray());'
    Replace = 'args = ProjectAsPath(args);'
    Tests = 'MigrationTests.Format_TakesItsDocumentedOptions'
}
