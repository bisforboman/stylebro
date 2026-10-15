# Trial 22: a fix that failed in verify's copy is no "no fix changes" (akka)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '                        copyFailures.Add(failure);'
    Replace = '                        _ = failure;'
    Tests = 'MigrationTests.Verify_IsNotClean_WhenTheCopysCodeFixFailed'
}
