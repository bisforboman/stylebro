# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var installed = recordedVersion is not null || recordedStyleSharp is not null ||'
    Replace = 'var installed = recordedVersion is not null ||'
    Tests = 'MigrationTests.StyleSharp_IsReadLikeStyleCop_'
}
