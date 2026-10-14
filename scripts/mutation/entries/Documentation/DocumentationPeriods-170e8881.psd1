# BRO1603: no period after text ending in a comma or semicolon (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'is ''.'' or ''?'' or ''!'' or '':'' or '','' or '';'')'
    Replace = 'is ''.'' or ''?'' or ''!'' or '':'')'
    Tests = 'DocumentationTests.TextEndingInACommaOrSemicolon_GetsNoPeriod'
}
