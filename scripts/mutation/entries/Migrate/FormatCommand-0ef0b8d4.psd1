# a run with load warnings gets its projects probed (DapperAOT, trial 57: a false Clean in a shallow clone)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '                warnedRuns.Add(currentRun);'
    Replace = ''
    Tests = 'FormatHostTests.AProjectWhoseDesignTimeBuildFails_IsNamed_AndTheCheckIsNotVerified'
}
