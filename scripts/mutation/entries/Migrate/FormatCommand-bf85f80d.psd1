# verify says its verdict after the checks at the end.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Action<string> runLog = hidden is null && !verify ? log : line =>'
    Replace = 'Action<string> runLog = hidden is null ? log : line =>'
    Tests = 'KeptFindingsTests.Verify_SaysTheVerdictAfterTheChecksAtTheEnd'
}
