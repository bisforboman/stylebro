# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (VerifyFromRecord(args, root, log) is { } fromRecord)'
    Replace = 'if (false && VerifyFromRecord(args, root, log) is { } fromRecord)'
    Tests = 'MigrationTests.Verify_AnswersFromTheRecord_AfterACleanCheckedFormat'
}
