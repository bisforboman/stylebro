# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'
    Find = '|| method.Parent is not TypeDeclarationSyntax or InterfaceDeclarationSyntax'
    Replace = '|| method.Parent is not TypeDeclarationSyntax'
    Tests = 'InternalTypeMethodTests'
}
