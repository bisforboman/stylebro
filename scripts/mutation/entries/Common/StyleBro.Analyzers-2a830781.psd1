# Trial 38: .mdx pages hold snippets too
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = ';$(_StyleBroSnippetsRoot)**/*.mdx'
    Replace = ''
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
