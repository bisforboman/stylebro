# With a change right next to a protected line, the lines on its other side find it.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (Unique(-side, 0) is >= 0 and var before)'
    Replace = 'if (Unique(-side, 0) is >= 0 and var before && before < 0)'
    Tests = 'MigrationTests.IfBodies_AreFoundAgainByTheLinesAroundThem'
}
