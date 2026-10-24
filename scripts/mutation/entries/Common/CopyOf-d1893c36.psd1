# Format host: a copy lacks only the files it doesn't have
@{
    File = 'src/StyleBro.FormatHost/CopyOf.cs'
    Find = '&& !File.Exists(this.OnDisk(path))'
    Replace = ''
    Tests = 'FormatHostFixTests.ACopy_LacksTheFilesGitLeftOut_ButNotWhatTheBuildWrites'
}
