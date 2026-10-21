# Trial 38: an in-place singleLineInclude follows the included text
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '(?:[^\n]*?&lt;!--[ \t]*|[ \t]*)'
    Replace = '(?:[ \t]*&lt;!--[ \t]*|[ \t]*)'
    Tests = 'PackageTargets_ReadTheSnippetNames'
}
