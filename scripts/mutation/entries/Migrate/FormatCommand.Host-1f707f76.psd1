# The host is the default engine (owner, 2026-10-25)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Host.cs'
    Find = 'return HostEngine;'
    Replace = 'return DotnetFormatEngine;'
    Tests = 'FormatHostTests.TheEngineOption_IsTakenOut'
}
