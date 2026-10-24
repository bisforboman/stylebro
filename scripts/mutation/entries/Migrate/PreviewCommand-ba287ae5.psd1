# Trial 51: the preview's copy keeps files committed with CRLF
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'MakeRepository(copy, ignoredTracked, restoreOutput, source);'
    Replace = 'MakeRepository(copy, ignoredTracked, restoreOutput);'
    Tests = 'MigrationTests.Preview_TheCopy_KeepsFilesCommittedWithCrLf'
}
