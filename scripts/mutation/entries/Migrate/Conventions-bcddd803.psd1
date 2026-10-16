# BRO1519 goes off with BRO1505 for compact members
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'convention.Key == SeparationKey && tag == "off"'
    Replace = 'convention.Key == SeparationKey && tag == "never"'
    Tests = 'MigrationTests.Conventions_CompactMembers_TurnBro1519OffWithBro1505'
}
