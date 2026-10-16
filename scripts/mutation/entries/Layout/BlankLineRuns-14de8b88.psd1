# Trial 26: the next accessor starting with an attribute list
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '            || next.Parent is AttributeListSyntax { Parent: AccessorDeclarationSyntax })'
    Replace = '            )'
    Tests = 'BlankLineRunsTests.NextAccessorWithAttributeOrModifier_NotReported'
}
