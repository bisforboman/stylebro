using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.RedundantBaseTypeAnalyzer, StyleBro.CodeFixes.Maintainability.RedundantBaseTypeCodeFixProvider>;

namespace StyleBro.Tests;

public class RedundantBaseTypeTests
{
    [Fact]
    public Task IntEnumsAndObjectClasses_LoseTheBase() => Verify.VerifyFixAsync(
        """
        using System;
        using I = System.Int32;

        public enum A : {|BRO1408:int|}
        {
            X,
        }

        public enum B : {|BRO1408:Int32|} { Y }

        public enum D : {|BRO1408:System.Int32|} { Z }

        public enum E : {|BRO1408:I|} { W }

        public class F : {|BRO1408:object|}
        {
        }

        public class G<T>
            : {|BRO1408:Object|}
            where T : class
        {
        }

        public class H : {|BRO1408:System.Object|}, IDisposable
        {
            public void Dispose()
            {
            }
        }
        """,
        """
        using System;
        using I = System.Int32;

        public enum A
        {
            X,
        }

        public enum B { Y }

        public enum D { Z }

        public enum E { W }

        public class F
        {
        }

        public class G<T>
            where T : class
        {
        }

        public class H : IDisposable
        {
            public void Dispose()
            {
            }
        }
        """);

    [Fact]
    public Task OtherBases_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;

        public enum A : long { X }

        public enum B : byte { Y }

        public class Object
        {
        }

        public class F : Object
        {
        }

        public class G : Exception
        {
        }

        public class H : /* base */ object
        {
        }

        public class J : object /* first */, IDisposable
        {
            public void Dispose()
            {
            }
        }
        """);
}
