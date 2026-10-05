using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ObjectCreationParenthesesAnalyzer, StyleBro.CodeFixes.Readability.ObjectCreationParenthesesCodeFixProvider>;

namespace StyleBro.Tests;

public class ObjectCreationParenthesesTests
{
    [Fact]
    public Task Omit_RemovesEmptyParentheses() => Verify.VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public int X { get; set; }

            public List<int> A = new List<int>{|BRO1141:()|} { 1, 2 };
            public C B = new C {|BRO1141:( )|}
            {
                X = 1,
            };
            public List<C> D = new List<C>{|BRO1141:()|} { new C{|BRO1141:()|} { X = 2 } };
            public List<int> E = new List<int>(4) { 1 };
            public List<int> F = new() { 1 };
            public List<int> G = new List<int>();
            public C H = new C /* why */ () { X = 3 };
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            public int X { get; set; }

            public List<int> A = new List<int> { 1, 2 };
            public C B = new C
            {
                X = 1,
            };
            public List<C> D = new List<C> { new C { X = 2 } };
            public List<int> E = new List<int>(4) { 1 };
            public List<int> F = new() { 1 };
            public List<int> G = new List<int>();
            public C H = new C /* why */ () { X = 3 };
        }
        """);

    [Fact]
    public Task Include_AddsThem() => Verify.VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public int X { get; set; }

            public List<int> A = new {|BRO1141:List<int>|} { 1, 2 };
            public C B = new {|BRO1141:C|}
            {
                X = 1,
            };
            public List<int> D = new List<int>() { 1 };
            public List<int> E = new() { 1 };
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            public int X { get; set; }

            public List<int> A = new List<int>() { 1, 2 };
            public C B = new C()
            {
                X = 1,
            };
            public List<int> D = new List<int>() { 1 };
            public List<int> E = new() { 1 };
        }
        """,
        "stylebro_object_creation_parentheses = include");
}
