# Trial 55: a build step names a generated file by its whole name
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'build.Split(''\n'').Any(l => named.IsMatch(l) &&'
    Replace = 'build.Split(''\n'').Any(l => l.Contains(name, StringComparison.OrdinalIgnoreCase) &&'
    Tests = 'MigrationTests.Format_AGeneratedFilesName_InsideALongerName'
}
