# Format host: what the build writes under obj doesn't make a copy lack a file
@{
    File = 'src/StyleBro.FormatHost/CopyOf.cs'
    Find = '!(built is not null && path.StartsWith(built, StringComparison.Ordinal))'
    Replace = 'true'
    Tests = 'FormatHostFixTests.ACopy_LacksTheFilesGitLeftOut_ButNotWhatTheBuildWrites'
}
