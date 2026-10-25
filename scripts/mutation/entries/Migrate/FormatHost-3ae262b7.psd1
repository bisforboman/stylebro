# Only --engine host rolls forward when an engine is named
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = 'return at + 1 < args.Length && args[at + 1] == FormatCommand.HostEngine;'
    Replace = 'return true;'
    Tests = 'FormatHostTests.TheCommandsThatRunTheHost_RollForward'
}
