# Trial 26: the sort other fixes run keeps a generated-part type's data order
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = 'MemberOrdering.FindFirstViolation(container, options, access, MemberOrdering.HasGeneratedPart(container, model, cancellationToken))'
    Replace = 'MemberOrdering.FindFirstViolation(container, options, access)'
    Tests = 'RegionsTests.RemovingRegions_KeepsTheDataOrder_OfATypeWithAGeneratedPart'
}
