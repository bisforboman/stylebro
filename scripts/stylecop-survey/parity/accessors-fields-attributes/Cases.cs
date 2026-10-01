namespace Probe
{
    using System;
    using System.Collections.Generic;

    public class Accessors
    {
        private int x;

        public int SetFirst
        {
            set { this.x = value; }
            get { return this.x; }
        }

        public int SetFirstMulti
        {
            set
            {
                this.x = value;
            }

            get
            {
                return this.x;
            }
        }

        public int OneLine { set { this.x = value; } get { return this.x; } }

        public int AutoSetFirst { set; get; }

        public int InitFirst { init { this.x = value; } get { return this.x; } }

        public int this[int i]
        {
            set { this.x = i; }
            get { return i; }
        }

        public event EventHandler Ev
        {
            remove { }
            add { }
        }

        public int WithComment
        {
            // the setter
            set { this.x = value; }

            // the getter
            get { return this.x; }
        }
    }

    public class Fields
    {
        private int a, b;
        private readonly string c = "c", d = "d";
        [Obsolete]
        public static int e, f;

        /// <summary>Doc.</summary>
        private int g, h;

        private int i; private int j;

        public void M()
        {
            int k, l;
            k = l = 0;
        }

        public event EventHandler E1, E2;
    }

    [Obsolete()]
    public class Attributes
    {
        [Obsolete()]
        public void A()
        {
        }

        [Obsolete("x")]
        public void B()
        {
        }

        [Obsolete( )]
        public void C()
        {
        }

        [return: System.Diagnostics.CodeAnalysis.NotNull()]
        public string D() => "";
    }

    public class Nullables
    {
        private Nullable<int> a;
        private System.Nullable<int> b;
        private global::System.Nullable<int> c;
        private List<Nullable<int>> d;
        private Type t = typeof(Nullable<>);
        private Type u = typeof(Nullable<int>);

        /// <summary>See <see cref="Nullable{T}"/>.</summary>
        public void M(Nullable<bool> p)
        {
            var x = default(Nullable<long>);
            var s = nameof(Nullable<int>);
        }
    }
}
