# Format host: a copy that lacks a file of the repository's workspace gets a load of its own
@{
    File = 'src/StyleBro.FormatHost/Session.cs'
    Find = 'if (loaded.Missing)'
    Replace = 'if (loaded.Missing && false)'
    Tests = 'FormatHostTests.AHostRunOnACopy_FixesTheCopy_InTheLoadedWorkspace'
}
