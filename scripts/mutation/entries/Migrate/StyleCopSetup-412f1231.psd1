# Trial 15: unquoted GetPathOfFileAbove arguments.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'GetPathOfFileAbove\(\s*''?([^'',]+?)''?'
    Replace = 'GetPathOfFileAbove\(\s*''([^'',]+?)'''
    Tests = 'MigrationTests.AnUnquotedGetPathOfFileAbove_IsFollowed'
}
