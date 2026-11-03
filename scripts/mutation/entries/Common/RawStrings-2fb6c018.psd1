# An interpolation hole is one placeholder: reformatting its expression is not a change.
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'text.Append(content is InterpolatedStringTextSyntax part ? part.TextToken.Text : "{}");'
    Replace = 'text.Append(content.ToString());'
    Tests = 'RawStringsTests'
}
