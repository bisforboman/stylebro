# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& !shadowing.Contains(n.Identifier.ValueText)))'
    Replace = '))'
    Tests = 'MigrationTests.ThisUses_AreCounted_WithoutTheOnesAParameterNeeds'
}
