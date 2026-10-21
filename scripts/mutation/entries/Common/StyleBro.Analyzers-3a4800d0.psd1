# Trial 38: an in-place include's key ends before '. path:'
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '+?)\.?(?='
    Replace = '+)\.?(?='
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
