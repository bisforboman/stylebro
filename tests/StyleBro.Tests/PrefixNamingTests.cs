using StyleBro.Analyzers.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.PrefixNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class PrefixNamingTests
{
    [Theory]
    [InlineData("Shape", "IShape")]
    [InlineData("iShape", "IShape")]
    [InlineData("_Shape", "IShape")]
    [InlineData("shape", "IShape")]
    [InlineData("IShape", null)]
    [InlineData("Item", null)]
    [InlineData("_", null)]
    public void InterfaceName(string name, string? expected) => Assert.Equal(expected, PrefixNames.GetInterfaceName(name));

    [Theory]
    [InlineData("Item", "TItem")]
    [InlineData("t", "T")]
    [InlineData("tKey", "TKey")]
    [InlineData("_x", "TX")]
    [InlineData("key", "TKey")]
    [InlineData("T1", null)]
    [InlineData("Type", null)]
    public void TypeParameterName(string name, string? expected) => Assert.Equal(expected, PrefixNames.GetTypeParameterName(name));

    [Fact]
    public Task Interfaces_AreRenamedEverywhere() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        /// <summary>Implemented by <see cref="Circle"/>; see also <see cref="Shape.Area"/>.</summary>
        public interface {|BRO1304:Shape|}
        {
            double Area();
        }

        public partial interface {|BRO1304:Split|}
        {
        }

        public partial interface {|BRO1304:Split|}
        {
        }

        public class Circle : Shape, Split
        {
            public interface {|BRO1304:Nested|}
            {
            }

            public double Area() => 1;

            public static List<Shape> All(Shape first) => new List<Shape> { first, (Shape)new Circle() };
        }
        """,
        """
        using System.Collections.Generic;

        /// <summary>Implemented by <see cref="Circle"/>; see also <see cref="IShape.Area"/>.</summary>
        public interface IShape
        {
            double Area();
        }

        public partial interface ISplit
        {
        }

        public partial interface ISplit
        {
        }

        public class Circle : IShape, ISplit
        {
            public interface INested
            {
            }

            public double Area() => 1;

            public static List<IShape> All(IShape first) => new List<IShape> { first, (IShape)new Circle() };
        }
        """);

    [Fact]
    public Task TypeParameters_AreRenamedEverywhere() => VerifyFixAsync(
        """
        using System;

        /// <typeparam name="Item">The item.</typeparam>
        public class Box<{|BRO1305:Item|}, TGood, {|BRO1305:t|}>
        {
            public Item Value;

            public TOut Map<{|BRO1305:Result|}, TOut>(Result r, Func<Result, TOut> f) => f(r);

            public void M<{|BRO1305:Value|}>(Value v)
            {
                void Local<{|BRO1305:Inner|}>(Inner i)
                {
                }
            }
        }

        public delegate TResult Handler<{|BRO1305:Arg|}, TResult>(Arg a);
        """,
        """
        using System;

        /// <typeparam name="TItem">The item.</typeparam>
        public class Box<TItem, TGood, T>
        {
            public TItem Value;

            public TOut Map<TResult, TOut>(TResult r, Func<TResult, TOut> f) => f(r);

            public void M<TValue>(TValue v)
            {
                void Local<TInner>(TInner i)
                {
                }
            }
        }

        public delegate TResult Handler<TArg, TResult>(TArg a);
        """);

    [Fact]
    public Task MethodTypeParameters_RenameOverridesAndImplementations() => VerifyFixAsync(
        """
        public interface IConverter
        {
            T Convert<{|BRO1305:Source|}, T>(Source s);
        }

        public abstract class Base
        {
            public abstract void Run<{|BRO1305:Input|}>(Input i);
        }

        public class Impl : Base, IConverter
        {
            public T Convert<Source, T>(Source s) => default;

            public override void Run<Input>(Input i)
            {
            }
        }
        """,
        """
        public interface IConverter
        {
            T Convert<TSource, T>(TSource s);
        }

        public abstract class Base
        {
            public abstract void Run<TInput>(TInput i);
        }

        public class Impl : Base, IConverter
        {
            public T Convert<TSource, T>(TSource s) => default;

            public override void Run<TInput>(TInput i)
            {
            }
        }
        """);

    [Fact]
    public Task Conflicts_AreSkipped() => VerifyNoDiagnosticsAsync("""
        public interface IShape
        {
        }

        public interface Shape
        {
        }

        public class TItem
        {
        }

        public class Box<Item>
        {
        }

        public class Pair<Key, TKey>
        {
        }

        public class Outer<Value>
        {
            public class TValue
            {
            }
        }

        public class Named
        {
            public object Get() => System.Type.GetType("Reflected");
        }

        namespace A
        {
            public interface IThing
            {
            }
        }

        namespace B
        {
            using System.Collections;

            public interface Thing
            {
            }

            public interface Enumerable
            {
            }
        }

        public class Holder
        {
            public int IWidget;

            public interface Widget
            {
            }
        }
        """);

    [Fact]
    public Task PartialMethods_AreSkipped() => VerifyNoDiagnosticsAsync("""
        public partial class P
        {
            partial void Q<Item>();

            partial void Q<Item>()
            {
            }
        }
        """);

    // Bogus: 'V' -> 'TV' renamed the declaration and the active branch, the '#if STANDARD' branch kept 'V' (CS0246 in
    // the target frameworks that compile it). Disabled code isn't bound, so no rename can follow it.
    [Fact]
    public Task NamesInDisabledCode_AreSkipped() => VerifyNoDiagnosticsAsync(
        ("/0/A.cs", """
            using System;
            using System.Reflection;

            internal static class Setters
            {
                private static Action<T, object> Create<T, V>(MethodInfo setter)
                {
                    var typed =
            #if STANDARD
                        (Action<T, V>)setter.CreateDelegate(typeof(Action<T, V>))
            #else
                        (Action<T, V>)Delegate.CreateDelegate(typeof(Action<T, V>), setter)
            #endif
                    ;
                    return (t, value) => typed(t, (V)value);
                }
            }

            internal class Box<item>
            {
                internal object Get()
                {
            #if NEVER
                    return default(item);
            #endif
                    return null;
                }
            }

            internal interface Shape
            {
            }
            """),
        ("/0/B.cs", """
            internal class Uses
            {
            #if NEVER
                Shape shape;
            #endif
            }
            """));

    [Fact]
    public Task DisabledCodeWithoutTheName_DoesNotBlock() => VerifyFixAsync(
        """
        internal class Box<{|BRO1305:item|}>
        {
            internal item Get()
            {
        #if NEVER
                return null;
        #endif
                return default(item);
            }
        }
        """,
        """
        internal class Box<TItem>
        {
            internal TItem Get()
            {
        #if NEVER
                return null;
        #endif
                return default(TItem);
            }
        }
        """);

    [Fact]
    public Task ConflictsInOtherFiles_KeepTheName() => VerifyNotFixedAsync(
        [
            """
            public interface {|BRO1304:Enumerable|}
            {
            }
            """,
            """
            namespace Other
            {
                using System.Collections;

                class C : Enumerable
                {
                }
            }
            """,
        ]);

    [Fact]
    public Task TypeNamesInStrings_KeepTheName() => VerifyNotFixedAsync(
        [
            """
            namespace App
            {
                public interface {|BRO1304:Reflected|}
                {
                }
            }
            """,
            """
            class Loader
            {
                object Load() => System.Type.GetType("App.Reflected, App");
            }
            """,
        ]);

    [Fact]
    public Task TypeParametersOfATypeWithAGeneratedPart_KeepTheirNames() => VerifyNoDiagnosticsAsync(
        ("/0/Grid.razor.cs", """
            public partial class Grid<item>
            {
            }
            """),
        ("/0/Grid.razor.g.cs", """
            public partial class Grid<item>
            {
                public item Value { get; set; }
            }
            """));

    [Fact]
    public Task PublicApi_IsLeftAloneByDefault() => VerifyFixAsync(
        """
        public interface Shape
        {
        }

        public class Box<Item>
        {
            internal void Put<{|BRO1305:Value|}>(Value value)
            {
            }
        }

        internal interface {|BRO1304:Area|}
        {
        }
        """,
        """
        public interface Shape
        {
        }

        public class Box<Item>
        {
            internal void Put<TValue>(TValue value)
            {
            }
        }

        internal interface IArea
        {
        }
        """,
        "stylebro_rename_public_api = false");
}
