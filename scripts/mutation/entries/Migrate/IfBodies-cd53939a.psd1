# Trial 27: a kept body is found again whatever spacing the run fixed on it
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'var nowText = now.Select(l => WithoutWhitespace(l.Text)).ToArray();'
    Replace = 'var nowText = now.Select(l => l.Text.Trim()).ToArray();'
    Tests = 'MigrationTests.IfBodies_KeepOnlyTheIndentation_AndTheirOtherSpacingIsFixed'
}
