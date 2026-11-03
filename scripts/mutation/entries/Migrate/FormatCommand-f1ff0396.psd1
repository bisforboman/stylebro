# The record keeps the kept raw string files.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'GeneratorLines.TryRemove(Key(root), out var generatorLine) ? generatorLine : null, rawStringGaps);'
    Replace = 'GeneratorLines.TryRemove(Key(root), out var generatorLine) ? generatorLine : null);'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
