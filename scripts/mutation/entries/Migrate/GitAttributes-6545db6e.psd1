# Backlog 085: a comment naming the baseline is not an attribute line
@{
    File = 'src/StyleBro.Migrate/GitAttributes.cs'
    Find = 'l.Length > 0 && l[0] != ''#'''
    Replace = 'l.Length > 0'
    Tests = 'GitAttributesTests.OtherLines_DontNameTheBaseline'
}
