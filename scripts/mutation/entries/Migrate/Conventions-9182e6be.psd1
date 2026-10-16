# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'var @default = style is not null && style.TryGetValue(convention.Key, out var styled) ? styled : convention.Default;'
    Replace = 'var @default = convention.Default;'
    Tests = 'MigrationTests.TheStyle_IsTheConventionsDefault'
}
