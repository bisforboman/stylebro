# Check coverage (2026-10-20): a file not in HEAD is deleted from the untouched copy
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'PreviewCommand.Git(repository, "cat-file", "-e", $"HEAD:{p}").Code == 0'
    Replace = 'true'
    Tests = 'FormatCoverageTests.TheUntouchedCopy'
}
