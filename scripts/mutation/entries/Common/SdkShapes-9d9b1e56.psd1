# SDK 11: a solution run passes no project id
@{
    File = 'src/StyleBro.FormatHost/SdkShapes.cs'
    Find = 'return projectPath.Length == 0'
    Replace = 'return projectPath.Length < 0'
    Tests = 'FormatHostTests.TheProjectArgument_FitsEachSdksDotnetFormat'
}
