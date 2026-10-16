# Kept one-line empty blocks get csharp_preserve_single_line_statements = true
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (formatsWhitespace && blocksComment is not null'
    Replace = 'if (!formatsWhitespace && blocksComment is not null'
    Tests = 'MigrationTests.Init_EmptyBlocksKept_KeepsThemFromIde0055'
}
