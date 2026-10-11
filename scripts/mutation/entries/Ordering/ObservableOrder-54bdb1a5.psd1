# BRO1001: order the runtime sees
@{
    File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'
    Find = 'var layout = type is StructDeclarationSyntax || type.IsKind(SyntaxKind.RecordStructDeclaration) || HasAttribute(type.AttributeLists, "StructLayout");'
    Replace = 'var layout = false;'
    Tests = 'MemberOrderingTests'
}
