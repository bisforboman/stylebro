# Trial 37: bin's markdown isn't the docs
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '$(_StyleBroSnippetsRoot)**/bin/**;'
    Replace = ''
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
