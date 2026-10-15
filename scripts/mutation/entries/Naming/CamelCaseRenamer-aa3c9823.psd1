# Trial 23: names VB uses keep them (owner: "Leave VB alone")
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (reference.Document.Project.Language != LanguageNames.CSharp)'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests.ATypeVisualBasicUses_IsKept_UsedFromVisualBasic'
}
