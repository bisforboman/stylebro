# Trial 27: only a kept body's indentation findings don't count
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = ' && number < old.Count && int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture) <= Indentation(old[number].Text).Length + 1'
    Replace = ''
    Tests = 'MigrationTests.IfBodies_KeepOnlyTheIndentation_AndTheirOtherSpacingIsFixed'
}
