# Trial 37: snippets only with an mdsnippets.json
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = 'GetPathOfFileAbove(''mdsnippets.json'', ''$(MSBuildProjectDirectory)'')'
    Replace = 'GetPathOfFileAbove(''x.json'', ''$(MSBuildProjectDirectory)'')'
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
