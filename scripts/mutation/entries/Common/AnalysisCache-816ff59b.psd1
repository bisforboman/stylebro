# Format host: the compiler's diagnostics are kept too
@{
    File = 'src/StyleBro.FormatHost/AnalysisCache.cs'
    Find = '!cached.Compiler.ContainsKey(compilerKey)'
    Replace = 'true'
    Tests = 'FormatHostFixTests.TheCache_KeepsTheCompilersDiagnostics_ByTheirIds'
}
