# A protected line is told apart from lines with the same text by the lines around it.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (now[at] == old[line] && Same(at, from, to))'
    Replace = 'if (now[at] == old[line])'
    Tests = 'MigrationTests.IfBodies_AreFoundAgainByTheLinesAroundThem'
}
