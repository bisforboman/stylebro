# an old 'stylebro-whitespace' block is replaced, not kept next to the new one
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'text = Migration.Apply(target.Begin == WhitespaceBegin ? RemoveWhitespaceBlock(text, legacyOnly: true) : text,'
    Replace = 'text = Migration.Apply(text,'
    Tests = 'MigrationTests.Init_ReplacesALegacyWhitespaceBlock'
}
