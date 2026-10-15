# Kept one-line empty blocks get csharp_preserve_single_line_statements = true
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (formatsWhitespace && kept.Concat(detected).Contains($"{StyleBro.Analyzers.Layout.SingleLineBlocks.AllowEmptyKey} = true")'
    Replace = 'if (!formatsWhitespace && kept.Concat(detected).Contains($"{StyleBro.Analyzers.Layout.SingleLineBlocks.AllowEmptyKey} = true")'
    Tests = 'MigrationTests.Init_EmptyBlocksKept_KeepsThemFromIde0055'
}
