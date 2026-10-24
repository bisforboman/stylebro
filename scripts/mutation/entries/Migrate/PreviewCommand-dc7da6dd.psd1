# Trial 51: CommittedWithCrLf reads the index's CRLF files
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'is ["i/crlf" or "i/mixed", ..])'
    Replace = 'is ["i/none", ..])'
    Tests = 'MigrationTests.Preview_TheCopy_KeepsFilesCommittedWithCrLf'
}
