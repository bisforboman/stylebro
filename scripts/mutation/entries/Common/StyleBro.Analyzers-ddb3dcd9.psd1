# Trial 38: the keyword in any case
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '''(?im)^'
    Replace = '''(?m)^'
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
