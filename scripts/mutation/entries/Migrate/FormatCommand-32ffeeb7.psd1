# Backlog 083: a note format says twice is recorded once.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (IsRecordedNote(line) && !notes.Contains(line))'
    Replace = 'if (IsRecordedNote(line))'
    Tests = 'MigrationTests.Verify_FromTheRecord_SaysFormatsNotesAboutTheRepository_NotThoseAboutTheRun'
}
