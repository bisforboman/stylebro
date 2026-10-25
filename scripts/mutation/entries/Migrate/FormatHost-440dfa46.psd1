# format and check roll forward: they run the host by default
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = '(expanded.FirstOrDefault() == "format" ||'
    Replace = '(false ||'
    Tests = 'FormatHostTests.TheCommandsThatRunTheHost_RollForward'
}
