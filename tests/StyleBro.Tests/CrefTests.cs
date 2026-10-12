using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Tests;

public class CrefTests
{
    // Crefs have their own syntax: tuple types ('(int, string)'), 'T?' inside type arguments and most rewrites of code
    // don't exist there, and a rule that reports one can't fix it ('dotnet format' says clean while the build warns).
    private const string Code = """
        using System;
        using System.Collections.Generic;

        namespace Docs
        {
            /// <summary>
            /// See <see cref="List{T}"/>, <see cref="Dictionary{TKey, TValue}"/>, <see cref="ValueTuple{T1, T2}"/>,
            /// <see cref="ValueTuple{T1, T2}.Item1"/>, <see cref="Nullable{T}"/>, <see cref="Nullable{T}.Value"/>,
            /// <see cref="M(ValueTuple{int, string}, Nullable{int})"/>, <see cref="M(Func{ValueTuple{int, string}, int}, Func{Nullable{int}})"/>,
            /// <see cref="M(List{ValueTuple{int, ValueTuple{int, int}}}, Nullable{ValueTuple{int, int}})"/>,
            /// <see cref="M(int?, Nullable{int})"/>, <see cref="String.Empty"/>, <see cref="Int32"/> and <see cref="Object.ToString()"/>.
            /// </summary>
            /// <remarks>
            /// <seealso cref="Func{ValueTuple{int, string}, Nullable{int}}"/>
            /// </remarks>
            public static class Holder
            {
                /// <inheritdoc cref="M(ValueTuple{int, string}, Nullable{int})"/>
                public static void M((int A, string B) pair, int? value)
                {
                }

                /// <summary>Calls <see cref="M(Func{ValueTuple{int, string}, int}, Func{Nullable{int}})"/>.</summary>
                /// <param name="map">The map.</param>
                /// <param name="value">The value.</param>
                public static void M(Func<(int A, string B), int> map, Func<int?> value)
                {
                }

                /// <summary>Calls <see cref="M(List{ValueTuple{int, ValueTuple{int, int}}}, Nullable{ValueTuple{int, int}})"/>.</summary>
                /// <param name="list">The list.</param>
                /// <param name="pair">The pair.</param>
                public static void M(List<(int A, (int B, int C) D)> list, (int A, int B)? pair)
                {
                }

                /// <summary>Calls <see cref="M(int?, Nullable{int})"/>.</summary>
                /// <param name="a">The first.</param>
                /// <param name="b">The second.</param>
                public static void M(int? a, long b)
                {
                }
            }
        }
        """;

    [Fact]
    public async Task EveryAnalyzer_ReportsNothingInCrefs()
    {
        var diagnostics = await GeneratedFileSuppressorTests.RunAsync(Code, @"C:\repo\src\Docs\Holder.cs");
        var root = await Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(Code).GetRootAsync();
        var crefs = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlCrefAttributeSyntax>().Select(c => c.Span).ToList();

        Assert.NotEmpty(crefs);
        Assert.All(diagnostics, d => Assert.False(crefs.Any(c => c.Contains(d.Location.SourceSpan.Start)), d.ToString()));
    }
}
