# BRO1404's off line belongs to the members' or the types' convention: a second init kept it for both.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'SharesOff(convention) ? convention.What : null'
    Replace = 'null'
    Tests = 'StyleBro.Tests.MigrationTests.Init_RunAgain_KeepsBothAccessModifierVerdicts'
}
