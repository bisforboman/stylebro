# Only bodies the driver skips: a hand-written partial method is reported once.
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = ' || !IsGeneratedByAttribute(symbol))'
    Replace = ')'
    Tests = 'StyleBro.Tests.GeneratedFileSuppressorTests.PartialMethodBody_IsAnalyzedWhenItsDeclarationIsGenerated'
}
