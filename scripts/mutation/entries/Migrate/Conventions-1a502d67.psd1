# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (style is not null && convention.Key != StaticPrefixKey && TurnsOff(style, convention))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.TheStyle_SkipsConventionsOfRulesItTurnsOff_AndKeepsUnprefixedStatics'
}
