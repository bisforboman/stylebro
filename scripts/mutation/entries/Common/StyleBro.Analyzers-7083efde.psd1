# Trial 38: include: takes a snippet
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '(?:snippet|include|singleLineInclude):'
    Replace = '(?:snippet|singleLineInclude):'
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
