# BRO1310 (Hungarian prefixes)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '&& !HungarianNames.IsInNativeMethods(context.Node)'
    Replace = ''
    Tests = 'HungarianNamingTests'
}
