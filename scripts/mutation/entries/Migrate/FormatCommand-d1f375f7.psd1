# Trial 24 (refit): files linked for some frameworks only
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'linked.Union(compile.Where(f => !Path.GetFullPath(f)'
    Replace = 'linked.Union(compile.Where(f => Path.GetFullPath(f)'
    Tests = 'MigrationTests.Format_FormatsFilesLinkedForSomeFrameworksOnly'
}
