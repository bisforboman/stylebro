# baseline --diff runs no format
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = '(expanded.FirstOrDefault() != "baseline" && PreviewCommand.Wants(expanded))'
    Replace = 'PreviewCommand.Wants(expanded)'
    Tests = 'FormatHostTests.TheCommandsThatRunTheHost_RollForward'
}
