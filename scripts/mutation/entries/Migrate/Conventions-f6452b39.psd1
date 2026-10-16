# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (total == 0 || (convention.StyleOnly && style is null))'
    Replace = 'if (total == 0)'
    Tests = 'MigrationTests.TheStyle_SkipsConventionsOfRulesItTurnsOff_AndKeepsUnprefixedStatics'
}
