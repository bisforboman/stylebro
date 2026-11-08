# Backlog 083: the check from the record says format's notes.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '(record.Notes ?? []).ForEach(log);'
    Replace = ';'
    Tests = 'MigrationTests.Verify_FromTheRecord_SaysFormatsNotesAboutTheRepository_NotThoseAboutTheRun'
}
