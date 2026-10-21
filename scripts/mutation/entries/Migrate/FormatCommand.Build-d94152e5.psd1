# Trial 40: the HEAD copy's build keeps format's rules as warnings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'Environment.SetEnvironmentVariable(HookVariable, hook);'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.BuildCheck_TheHeadCopyKeepsFormatsRulesAsWarnings'
}
