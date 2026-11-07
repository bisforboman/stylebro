# The record keeps the kept raw string files.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'GeneratorLines.TryRemove(Key(root), out var generatorLine) ? generatorLine : null, rawStringGaps, notes);'
    Replace = 'GeneratorLines.TryRemove(Key(root), out var generatorLine) ? generatorLine : null, null, notes);'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
