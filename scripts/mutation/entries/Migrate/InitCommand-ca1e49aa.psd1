# BRO1404's off line belongs to the members' or the types' convention: a second init kept it for both.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'SharesOff(convention) || convention.Key == Conventions.StaticPrefixKey ? convention.What : null'
    Replace = 'convention.Key == Conventions.StaticPrefixKey ? convention.What : null'
    Tests = 'StyleBro.Tests.MigrationTests.Init_RunAgain_KeepsBothAccessModifierVerdicts'
}
