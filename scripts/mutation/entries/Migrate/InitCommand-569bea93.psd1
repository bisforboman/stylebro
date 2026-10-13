# init: a static field naming rule nothing enforces (IDE1006 off) leaves the casing to the code.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'else if (!NamingRulesOff(own) && '
    Replace = 'else if ('
    Tests = 'MigrationTests.Init_MixedPrivateStaticFieldCasing'
}
