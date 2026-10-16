# A BRO1519 the team set is left alone
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& tag == "off" && !IsSet(afterBrace)'
    Replace = '&& tag == "off"'
    Tests = 'MigrationTests.Conventions_CompactMembers_TurnBro1519OffWithBro1505'
}
