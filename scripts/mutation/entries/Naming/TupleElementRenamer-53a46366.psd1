# Trial 21: a tuple element kept with another says so
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'blocker is not null && blocker != oldName ? KeptReason.KeptWithOtherElement : '
    Replace = ''
    Tests = 'KeptFindingsTests.ATupleElementKeptWithAnother_SaysSo'
}
