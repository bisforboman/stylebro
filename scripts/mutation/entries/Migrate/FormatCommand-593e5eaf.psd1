# Trial 37: bro's MSBuild queries don't restore for a Directory.Build.rsp
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'foreach (var argument in WithoutRestore(arguments))'
    Replace = 'foreach (var argument in arguments)'
    Tests = 'Capture_AnMSBuildQuery'
}
