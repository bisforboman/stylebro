# init: a private field naming rule nothing enforces (IDE1006 off) leaves the style to the code.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '        if (NamingRulesOff(settings))'
    Replace = '        if (false)'
    Tests = 'MigrationTests.Init_LeavesThePrivateFieldStyle'
}
