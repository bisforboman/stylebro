# Trial 37: strings count only with Shouldly
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (!UsesShouldly(project, solution))'
    Replace = 'if (false)'
    Tests = 'WithShouldly_ANameInAStringOfTheFile_IsKept'
}
