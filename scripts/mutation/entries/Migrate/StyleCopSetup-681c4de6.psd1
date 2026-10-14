# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '"SST1633" => styleSharp.Options.TryGetValue("file_header_template", out var template) && template.Length > 0 ? styleCop : [],'
    Replace = '"SST1633" => styleCop,'
    Tests = 'MigrationTests.StyleSharp_IsReadLikeStyleCop_'
}
