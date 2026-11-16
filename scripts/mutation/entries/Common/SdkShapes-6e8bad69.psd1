# SDK 11's DetermineFormattableFilesAsync takes a ProjectId, SDK 10's a path
@{
    File = 'src/StyleBro.FormatHost/SdkShapes.cs'
    Find = 'if (parameter.FullName != typeof(ProjectId).FullName)'
    Replace = 'if (parameter.FullName == typeof(ProjectId).FullName)'
    Tests = 'FormatHostTests.TheProjectArgument_FitsEachSdksDotnetFormat'
}
