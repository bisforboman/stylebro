# BRO1001: GetFields() kept in a local and indexed later
@{
    File = 'src/StyleBro.Analyzers/Ordering/ReflectionByPosition.cs'
    Find = 'return block.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.ValueText == local && IsIndexed(n));'
    Replace = 'return false;'
    Tests = 'MemberOrderingTests.FieldsReadByPositionElsewhere'
}
