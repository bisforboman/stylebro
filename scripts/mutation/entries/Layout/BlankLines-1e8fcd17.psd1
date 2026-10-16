# Trial 26: assembly attributes are no element's
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& list.Target?.Identifier.Kind() is not (SyntaxKind.AssemblyKeyword or SyntaxKind.ModuleKeyword);'
    Replace = ';'
    Tests = 'BlankLineAfterTests.CommentBelowAnAttribute_NoDiagnostic_UnlessAnAssemblyAttribute'
}
