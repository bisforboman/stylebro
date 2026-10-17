# BRO1001: the sort adds the blank lines also with BRO1505 off (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'var createsSeparation = NeedsSeparation('
    Replace = 'var createsSeparation = isOn?.Invoke(DiagnosticIds.ElementsSeparatedByBlankLine) != false && NeedsSeparation('
    Tests = 'MemberOrderingTests.FieldsFirst_GivesMovedMembersTheirBlankLines_WhenBro1505IsOff'
}
