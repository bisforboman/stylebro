# Trial 50: the host says why its workspace did not load
@{
    File = 'src/StyleBro.FormatHost/Session.cs'
    Find = 'output(Entry.LoadFailedMarker + failure);'
    Replace = 'output(failure);'
    Tests = 'FormatHostTests.AWorkspaceThatDoesntLoad_IsDeclinedWithTheReason'
}
