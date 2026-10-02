using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Documentation.FileHeaderAnalyzer, StyleBro.CodeFixes.Documentation.FileHeaderCodeFixProvider>;

namespace StyleBro.Tests;

public class FileHeaderTests
{
    private const string Contoso = "stylebro_file_header_company = Contoso\n";

    [Fact]
    public Task CorrectHeader_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task WithoutCompany_NothingIsReported() => Verify.VerifyNoDiagnosticsAsync("""
        namespace P;
        """);

    [Fact]
    public Task MissingHeader_IsAdded() => Verify.VerifyFixAsync("""
        {|BRO1615:|}using System;

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        using System;

        namespace P;
        """, Contoso);

    [Fact]
    public Task MissingHeader_BlankLinesAtTheTopAreReplaced() => Verify.VerifyFixAsync("""


        #nullable enable
        {|BRO1615:|}namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        #nullable enable
        namespace P;
        """, Contoso);

    [Fact]
    public Task DocumentationCommentFirst_HeaderGoesAbove() => Verify.VerifyFixAsync("""
        /// <summary>A type.</summary>
        {|BRO1615:|}public class C { }
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        /// <summary>A type.</summary>
        public class C { }
        """, Contoso);

    [Fact]
    public Task PlainComment_IsKeptBelowTheNewHeader() => Verify.VerifyFixAsync("""
        {|BRO1615:|}// Some notes about this file.

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        // Some notes about this file.

        namespace P;
        """, Contoso);

    [Fact]
    public Task PlainHeaderWithTheCopyrightText_BecomesTheXmlHeader() => Verify.VerifyFixAsync("""
        {|BRO1615:|}// Copyright (c) Contoso. All rights reserved.

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task BrokenXmlHeader_IsLeftToAPerson() => Verify.VerifyNoDiagnosticsAsync("""
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.

        namespace P;
        """, Contoso);

    [Fact]
    public Task HeaderWithoutCopyrightTag_GetsOneAtTheTop() => Verify.VerifyFixAsync("""
        {|BRO1615:|}// <summary>Helpers.</summary>

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>
        // <summary>Helpers.</summary>

        namespace P;
        """, Contoso);

    [Fact]
    public Task WrongFileCompanyAndText_TagIsRewrittenAndTheRestKept() => Verify.VerifyFixAsync("""
        //-----------------------------------------------------------------------
        // {|BRO1615:|}<copyright file="Other.cs" company="Fabrikam">
        //     Copyright (c) Fabrikam. All rights reserved.
        // </copyright>
        // <summary>Helpers.</summary>
        //-----------------------------------------------------------------------

        namespace P;
        """, """
        //-----------------------------------------------------------------------
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>
        // <summary>Helpers.</summary>
        //-----------------------------------------------------------------------

        namespace P;
        """, Contoso);

    [Theory]
    [InlineData("<copyright company=\"Contoso\">")]
    [InlineData("<copyright file=\"test0.cs\" company=\"Contoso\">")]
    [InlineData("<copyright file=\"Test0.cs\">")]
    [InlineData("<copyright file=\"Test0.cs\" company=\" \">")]
    public Task EachAttributeProblem_IsReported(string openingTag) => Verify.VerifyFixAsync($$"""
        // {|BRO1615:|}{{openingTag}}
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task EmptyCopyrightText_IsFilledIn() => Verify.VerifyFixAsync("""
        // {|BRO1615:|}<copyright file="Test0.cs" company="Contoso">
        // </copyright>

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task SelfClosingTag_IsReplaced() => Verify.VerifyFixAsync("""
        // {|BRO1615:|}<copyright file="Test0.cs" company="Contoso" />

        namespace P;
        """, """
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task TextIsComparedLikeStyleCop_LineByLineWithoutSurroundingWhitespace() => Verify.VerifyNoDiagnosticsAsync("""
        //<copyright file="Test0.cs" company="Contoso">
        //      Copyright (c) Contoso. All rights reserved.
        //</copyright>
        namespace P;
        """, Contoso);

    [Fact]
    public Task OneLineTag_IsFine() => Verify.VerifyNoDiagnosticsAsync("""
        // <copyright file="Test0.cs" company="Contoso">Copyright (c) Contoso. All rights reserved.</copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task CustomCopyrightText_WithLineBreaksAndVariables() => Verify.VerifyFixAsync("""
        {|BRO1615:|}namespace P;
        """, """
        // <copyright file="Test0.cs" company="A &amp; B">
        // Copyright A &amp; B.
        //
        // Test0.cs is licensed under the MIT license.
        // </copyright>

        namespace P;
        """, "stylebro_file_header_company = A & B\nstylebro_file_header_copyright = Copyright {companyName}.\\n\\n{fileName} is licensed under the MIT license.\n");

    [Fact]
    public Task Decoration_SurroundsANewHeader() => Verify.VerifyFixAsync("""
        {|BRO1615:|}namespace P;
        """, """
        // -----------------------------------------------------------------------
        // <copyright file="Test0.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>
        // -----------------------------------------------------------------------

        namespace P;
        """, Contoso + "stylebro_file_header_decoration = -----------------------------------------------------------------------\n");

    [Fact]
    public Task TagSharingALineWithOtherText_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        // <summary>Helpers.</summary><copyright file="Other.cs" company="Contoso">
        // Copyright (c) Contoso. All rights reserved.
        // </copyright>

        namespace P;
        """, Contoso);

    [Fact]
    public Task BlockCommentHeader_IsNotChecked() => Verify.VerifyNoDiagnosticsAsync("""
        /* <copyright file="Other.cs" company="Contoso">
           Copyright (c) Contoso. All rights reserved.
           </copyright> */

        namespace P;
        """, Contoso);

    [Fact]
    public Task GeneratedFile_IsNotChecked() => Verify.VerifyNoDiagnosticsAsync("""
        // <auto-generated/>
        namespace P;
        """, Contoso);

    [Fact]
    public Task WhitespaceOnlyFile_IsNotChecked() => Verify.VerifyNoDiagnosticsAsync("\n\n", Contoso);

    [Fact]
    public Task CrLfFile_KeepsItsLineEndings() => Verify.VerifyFixAsync(
        "{|BRO1615:|}namespace P;\r\n\r\npublic class C { }\r\n",
        "// <copyright file=\"Test0.cs\" company=\"Contoso\">\r\n// Copyright (c) Contoso. All rights reserved.\r\n// </copyright>\r\n\r\nnamespace P;\r\n\r\npublic class C { }\r\n",
        Contoso);

    [Fact]
    public Task FixAll_FixesEveryFile() => Verify.VerifyFixAsync(
        new[] { "{|BRO1615:|}namespace P;\n", "// {|BRO1615:|}<copyright file=\"Test0.cs\" company=\"Contoso\">\n// Copyright (c) Contoso. All rights reserved.\n// </copyright>\nnamespace P;\n" },
        new[]
        {
            "// <copyright file=\"Test0.cs\" company=\"Contoso\">\n// Copyright (c) Contoso. All rights reserved.\n// </copyright>\n\nnamespace P;\n",
            "// <copyright file=\"Test1.cs\" company=\"Contoso\">\n// Copyright (c) Contoso. All rights reserved.\n// </copyright>\nnamespace P;\n",
        },
        Contoso);
}
