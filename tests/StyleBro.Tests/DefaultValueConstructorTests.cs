using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.DefaultValueConstructorAnalyzer, StyleBro.CodeFixes.Readability.DefaultValueConstructorCodeFixProvider>;

namespace StyleBro.Tests;

public class DefaultValueConstructorTests
{
    [Fact]
    public Task ValueTypes_BecomeDefault() => VerifyFixAsync(
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new int()|};
                var b = {|BRO1104:new DateTime()|};
                var c = {|BRO1104:new Plain()|};
                var d = {|BRO1104:new int?()|};
                Plain e = {|BRO1104:new()|};
                var f = {|BRO1104:new Plain()|}.X;
            }
        }
        """,
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = default(int);
                var b = default(DateTime);
                var c = default(Plain);
                var d = default(int?);
                Plain e = default(Plain);
                var f = default(Plain).X;
            }
        }
        """);

    [Fact]
    public Task WellKnownTypesAndEnums_UseTheirEmptyMember() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new CancellationToken()|};
                var b = {|BRO1104:new Guid()|};
                var c = {|BRO1104:new IntPtr()|};
                var d = {|BRO1104:new Color()|};
                var e = {|BRO1104:new Flags()|};
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = CancellationToken.None;
                var b = Guid.Empty;
                var c = IntPtr.Zero;
                var d = Color.Red;
                var e = default(Flags);
            }
        }
        """);

    [Fact]
    public Task ParameterDefaults_StayConstant() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = {|BRO1104:new Guid()|}, CancellationToken ct = {|BRO1104:new CancellationToken()|}, Color c = {|BRO1104:new Color()|})
            {
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = default(Guid), CancellationToken ct = default(CancellationToken), Color c = Color.Red)
            {
            }
        }
        """);

    [Fact]
    public Task CodeThatDefaultWouldSkip_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        struct WithConstructor
        {
            public int X;
            public WithConstructor() { X = 1; }
        }

        struct Plain { public int X; }

        class C
        {
            void M<T>() where T : struct
            {
                var a = new WithConstructor();
                var b = new T();
                new Plain();
            }
        }
        """);

    [Fact]
    public Task ArgumentsInitializersAndClasses_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = new Plain { X = 1 };
                var b = new Plain() { };
                var c = new System.DateTime(2000, 1, 1);
                var d = new C();
            }
        }
        """);
}
