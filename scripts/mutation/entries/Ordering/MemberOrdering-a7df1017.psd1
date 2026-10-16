# BRO1001: the sort adds BRO1505's blank line only when BRO1505 is on (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'var createsSeparation = isOn?.Invoke(DiagnosticIds.ElementsSeparatedByBlankLine) != false'
    Replace = 'var createsSeparation = true'
    Tests = 'MemberOrderingTests.OneLineType_IsExpandedBeforeTheSort_WhenBro1509IsOn'
}
