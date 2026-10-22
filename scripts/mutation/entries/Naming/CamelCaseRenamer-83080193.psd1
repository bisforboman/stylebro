# BRO1303: a static field's qualifier is spelled in the reference's own compilation (Serilog's Log.cs, fix-loop measurement 2026-10)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| (model.GetSymbolInfo(name, cancellationToken).Symbol ?? symbol) is not IFieldSymbol field'
    Replace = '|| symbol is not IFieldSymbol field'
    Tests = 'LinkedFileFixAllTests.StaticFieldQualifier_IsTheSameWhicheverCopyIsFixed'
}
