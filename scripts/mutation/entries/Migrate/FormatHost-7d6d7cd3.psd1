# Format host: a prerelease runtime folder counts by its major version
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = 'Regex.Replace(Path.GetFileName(d), "-.*$", string.Empty)'
    Replace = 'Path.GetFileName(d)'
    Tests = 'FormatHostTests.TheNewestRuntime_IsTheHighestMajorNextToThisOne'
}
