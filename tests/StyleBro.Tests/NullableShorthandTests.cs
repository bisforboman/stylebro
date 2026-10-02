using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.NullableShorthandAnalyzer, StyleBro.CodeFixes.Readability.NullableShorthandCodeFixProvider>;

namespace StyleBro.Tests;

public class NullableShorthandTests
{
    [Fact]
    public Task Nullable_IsShortened() => VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            private {|BRO1115:Nullable<int>|} a;
            private {|BRO1115:System.Nullable<int>|} b;
            private {|BRO1115:global::System.Nullable<int>|} c;
            private List<{|BRO1115:Nullable<int>|}> d;
            private Type t = typeof({|BRO1115:Nullable<int>|});
            private {|BRO1115:Nullable<KeyValuePair<int, {|BRO1115:Nullable<long>|}>>|} e;

            public {|BRO1115:Nullable<bool>|} M({|BRO1115:Nullable<bool>|} p) => default({|BRO1115:Nullable<bool>|});
        }
        """,
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            private int? a;
            private int? b;
            private int? c;
            private List<int?> d;
            private Type t = typeof(int?);
            private KeyValuePair<int, long?>? e;

            public bool? M(bool? p) => default(bool?);
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;
        using NI = System.Nullable<int>;

        /// <summary>See <see cref="Nullable{T}"/>.</summary>
        public class C
        {
            private Type t = typeof(Nullable<>);
            private string s = nameof(Nullable<int>);
            private bool e = Nullable<int>.Equals(1, 1);

            public bool M(object o) => o is Nullable<int>;

            public object A(object o) => o as Nullable<int>;
        }

        public class Nullable<T>
        {
        }

        namespace Inner
        {
            public class D
            {
                private Nullable<int> own;
            }

            public class Nullable<T>
            {
            }
        }
        """);
}
