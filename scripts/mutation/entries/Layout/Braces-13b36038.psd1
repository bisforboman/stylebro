# Trial 22: the body of a statement that shared its owner's line lines up with the statement.
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'BodyBraceLine(inside.Statement, text) is { } braceLine ?'
    Replace = 'BodyBraceLine(inside.Statement, text) is { } braceLine && false ?'
    Tests = 'BracesTests.SameLineStatement_BodyIndentedByTheFormatter'
}
