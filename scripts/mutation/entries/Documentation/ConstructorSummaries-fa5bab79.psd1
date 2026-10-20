# BRO1606: a filler sentence naming the type in <c> is replaced whole (trial 35, Castle Core)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '.Any(m => sentence.IndexOf("</" + m.Groups[1].Value + ">", m.Index, StringComparison.Ordinal) < 0)'
    Replace = '.Any(m => false)'
    Tests = 'DocumentationTests.ConstructorSummaries_FillerWithTheNameInCode'
}
