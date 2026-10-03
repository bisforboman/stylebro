using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.BaseCallsAnalyzer, StyleBro.CodeFixes.Readability.BaseCallsCodeFixProvider>;

namespace StyleBro.Tests;

public class BaseCallsTests
{
    [Fact]
    public Task NonVirtualMembers_And_SealedTypes_AreFixed() => VerifyFixAsync(
        """
        public class Base
        {
            public int Count;

            public int this[int i] => i;

            public int Size { get; set; }

            public void Reset()
            {
            }

            public virtual string Describe() => "base";
        }

        public class Derived : Base
        {
            public int Total() => {|BRO1131:base|}.Count + {|BRO1131:base|}[1] + {|BRO1131:base|}.Size;

            public void Clear() => {|BRO1131:base|}.Reset();
        }

        public sealed class Leaf : Base
        {
            public string Text() => {|BRO1131:base|}.Describe();
        }

        public struct Value
        {
            public string Text() => {|BRO1131:base|}.ToString();
        }
        """,
        """
        public class Base
        {
            public int Count;

            public int this[int i] => i;

            public int Size { get; set; }

            public void Reset()
            {
            }

            public virtual string Describe() => "base";
        }

        public class Derived : Base
        {
            public int Total() => this.Count + this[1] + this.Size;

            public void Clear() => this.Reset();
        }

        public sealed class Leaf : Base
        {
            public string Text() => this.Describe();
        }

        public struct Value
        {
            public string Text() => this.ToString();
        }
        """);

    [Fact]
    public Task QualificationFalse_DropsTheQualifier_WhereThePlainNameMeansTheSame() => VerifyFixAsync(
        """
        public class Base
        {
            public int Count;

            public int this[int i] => i;

            public void Reset()
            {
            }
        }

        public class Derived : Base
        {
            public int Total(int Count) => {|BRO1131:base|}.Count + {|BRO1131:base|}[1];

            public void Clear() => {|BRO1131:base|}.Reset();

            public int Local()
            {
                var Count = 2;
                return {|BRO1131:base|}.Count + Count;
            }
        }
        """,
        """
        public class Base
        {
            public int Count;

            public int this[int i] => i;

            public void Reset()
            {
            }
        }

        public class Derived : Base
        {
            public int Total(int Count) => this.Count + this[1];

            public void Clear() => Reset();

            public int Local()
            {
                var Count = 2;
                return this.Count + Count;
            }
        }
        """,
        editorConfig: "dotnet_style_qualification_for_method = false\ndotnet_style_qualification_for_field = false:silent");

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public class Base
        {
            public int Count;

            public virtual string Describe() => "base";

            public virtual void Reset()
            {
            }

            public void Hidden()
            {
            }
        }

        public class Derived : Base
        {
            // A local implementation: 'base' is needed.
            public override string Describe() => base.Describe() + "!";

            // Virtual and the type isn't sealed: a derived override would run instead.
            public void Clear() => base.Reset();

            // Hidden by a member of this type.
            public new int Count => base.Count;

            public new void Hidden() => base.Hidden();

            public Derived()
                : base()
            {
            }
        }
        """);
}
