# Trial 24: old Mac files are left out of every pass already
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'LoneCrs(text) is { Code: false, Literal: true }'
    Replace = 'LoneCrs(text) is { Literal: true }'
    Tests = 'MigrationTests.Format_LeavesAStringsLoneCarriageReturn'
}
