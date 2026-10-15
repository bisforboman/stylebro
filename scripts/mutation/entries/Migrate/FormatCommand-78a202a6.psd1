# Trial 22: a failed code fix makes a format run not clean (akka)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '                    failures.Add(failure);'
    Replace = '                    _ = failure;'
    Tests = 'MigrationTests.Format_IsNotClean_WhenACodeFixFailed'
}
