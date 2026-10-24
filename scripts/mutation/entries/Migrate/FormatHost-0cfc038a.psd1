# Trial 50: bro reads the host load failure
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = 'failure = line.Substring(LoadFailedMarker.Length);'
    Replace = 'output(line);'
    Tests = 'FormatHostTests.AWorkspaceThatDoesntLoad_IsDeclinedWithTheReason'
}
