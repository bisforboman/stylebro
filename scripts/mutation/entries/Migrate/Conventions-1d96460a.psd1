# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'out var severity) && severity == "none");'
    Replace = 'out var severity) && severity != "none");'
    Tests = 'MigrationTests.TheStyle_SkipsConventionsOfRulesItTurnsOff_AndKeepsUnprefixedStatics'
}
