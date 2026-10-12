using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Issue #129: init counts implicit 'private' (BRO1404) and combined declarations (BRO1142, BRO1114).</summary>
public sealed partial class MigrationTests
{
    private const string AccessKey = "dotnet_style_require_accessibility_modifiers";

    [Fact]
    public void Conventions_CountPrivateByDefaultMembers_WithAndWithoutPrivate()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A : I
            {
                int a;
                private int b;
                public int c;
                private protected int d;
                void M() { }
                private void N() { }
                A() { }
                static A() { }
                partial void P();
                void I.X() { }
                int I.Y => 1;
                event System.Action E;
                class Nested { }
                struct S { int s; }
                record R { private int r; }
            }

            class B { }

            interface I
            {
                void X();
                int Y { get; }
                private void Z() { }
            }
            """),
            counts);

        // Without: a, M, A(), E, Nested, S, s, R; with: b, N, r. Not: c, d, static A(), P, I.X, I.Y, B (internal), interface members.
        Assert.Equal(new[] { 3, 8 }, counts[AccessKey]);
    }

    [Fact]
    public void Conventions_CountCombinedDeclarations_AgainstOnesThatCouldHaveBeenCombined()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A
            {
                int _rate, _channels;
                int _one;
                int _two;
                string _name;
                [Obsolete] int _three;
                int _four;
                static int _five;
                int _six;

                void M(int[] h)
                {
                    int n = h.Length, i = 0;
                    int x = 1;
                    int y = 2;
                    var p = 1;
                    var q = 2;
                    long z = 3;
                    const int C = 1;
                    int w = 4;
                    using System.IO.Stream s1 = null;
                    System.IO.Stream s2 = null;
                }
            }
            """),
            counts);

        // Fields: '_rate, _channels' combined; '_two' after '_one' split. ('_three'/'_four' have an attribute, '_six' other modifiers.)
        Assert.Equal(new[] { 1, 1 }, counts["dotnet_diagnostic.BRO1114.severity"]);

        // Locals: 'n, i' combined; 'y' after 'x' split. ('var' can't be combined, 'const' and 'using' differ.)
        Assert.Equal(new[] { 1, 1 }, counts["dotnet_diagnostic.BRO1142.severity"]);
    }

    [Theory]
    [InlineData(AccessKey, 2, 40, AccessKey + " = omit_if_default")]
    [InlineData(AccessKey, 40, 2, null)]
    [InlineData(AccessKey, 20, 20, "dotnet_diagnostic.BRO1404.severity = none")]
    [InlineData("dotnet_diagnostic.BRO1142.severity", 2, 180, "dotnet_diagnostic.BRO1142.severity = none")]
    [InlineData("dotnet_diagnostic.BRO1142.severity", 20, 20, "dotnet_diagnostic.BRO1142.severity = none")]
    [InlineData("dotnet_diagnostic.BRO1142.severity", 40, 2, null)]
    [InlineData("dotnet_diagnostic.BRO1114.severity", 0, 5, "dotnet_diagnostic.BRO1114.severity = none")]
    [InlineData("dotnet_diagnostic.BRO1114.severity", 0, 2, null)]   // too few
    public void Conventions_ImplicitPrivateAndCombinedDeclarations_AreFollowed(string key, int first, int second, string? expected)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [key] = new[] { first, second } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#') && !l.StartsWith("dotnet_sort", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }

    [Fact]
    public void Init_ImplicitPrivateAndCombinedDeclarations_AreFollowed_UnlessTheEditorConfigSaysOtherwise()
    {
        Write(".editorconfig", "root = true\n");
        Write("Reader.cs", """
            class Reader
            {
                int _rate, _channels;
                int _bits, _frames;
                readonly byte[] _buffer = new byte[4];

                void Read() { }

                int Next(int[] h)
                {
                    int n = h.Length, s = 0;
                    for (int i = 0; i < n; i++) { s += h[i]; }
                    int a = 1, b = 2;
                    int c = 3, d = 4;
                    return s + a + b + c + d;
                }
            }
            """);

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("\n" + AccessKey + " = omit_if_default\n", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.BRO1142.severity = none\n", output, StringComparison.Ordinal);
        Assert.Contains("  too few  adjacent fields of one type: 2 places -> default stays (warning)", output, StringComparison.Ordinal);

        Write(".editorconfig", "root = true\n[*.cs]\n" + AccessKey + " = always:warning\n");
        output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.DoesNotContain("\n" + AccessKey + " = omit_if_default\n", output, StringComparison.Ordinal);
        Assert.Contains("  set      private-by-default members:", output, StringComparison.Ordinal);
    }
}
