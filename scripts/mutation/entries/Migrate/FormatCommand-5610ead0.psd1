# Trial 22: a string of either parse that spans a directive of the other (CommunityToolkit's raw strings in an inactive #if)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return Clash(none, all).Concat(Clash(all, none))'
    Replace = 'return Clash(none, all)'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
