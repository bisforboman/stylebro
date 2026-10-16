# Node actions run in hand-written bodies of partial methods a generator declared with [GeneratedCode].
@{
    File = 'src/StyleBro.Analyzers/ForeignFiles.cs'
    Find = '            inner.RegisterCompilationStartAction(start => RegisterGeneratedPartialBodies(start, action, syntaxKinds));'
    Replace = ''
    Tests = 'StyleBro.Tests.GeneratedFileSuppressorTests.PartialMethodBody_IsAnalyzedWhenItsDeclarationIsGenerated'
}
