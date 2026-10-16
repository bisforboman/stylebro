# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& total >= MinimumAgreeing && Winner(count[0], count[1]) == 0'
    Replace = '&& total >= MinimumAgreeing && Winner(count[0], count[1]) == 1'
    Tests = 'MigrationTests.TheStyle_SkipsConventionsOfRulesItTurnsOff_AndKeepsUnprefixedStatics'
}
