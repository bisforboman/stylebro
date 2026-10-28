# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var text = StyleCopSetup.WithoutComments(File.ReadAllText(props));'
    Replace = 'var text = File.ReadAllText(props);'
    Tests = 'MigrationTests'
}
