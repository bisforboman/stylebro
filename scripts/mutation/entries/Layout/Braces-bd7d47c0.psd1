# A child on its owner's line goes one level deeper under CSharpier.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'LeadingWhitespace(text, ownerLine).Length + unit :'
    Replace = 'LeadingWhitespace(text, ownerLine).Length :'
    Tests = 'BracesTests.WithCSharpier'
}
