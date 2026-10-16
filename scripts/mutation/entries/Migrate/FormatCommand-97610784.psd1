# What an earlier command recorded as unchecked does not count.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Unchecked.TryRemove(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), out _);'
    Replace = ''
    Tests = 'KeptFindingsTests.Verify_SaysTheVerdictAfterTheChecksAtTheEnd'
}
