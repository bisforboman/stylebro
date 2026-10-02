# Cases for Test-SdkCoverage.ps1: per StyleCop rule, a small piece of code that violates that rule and compiles.
# Each case becomes <Id>.cs in its own namespace. $T is a tab; trailing spaces are written explicitly.
$T = "`t"
$cases = [ordered]@{
    # ---- Spacing -------------------------------------------------------------------------------
    SA1000 = 'class C { void M() { if(true) { } } }'
    SA1001 = 'class C { void M(int a, int b) { M(a ,b); } }'
    SA1002 = 'class C { void M() { int x = 1 ; _ = x; } }'
    SA1003 = 'class C { int M(int a, int b) { return a+b; } }'
    SA1005 = "class C`n{`n    //comment`n    void M() { }`n}"
    SA1007 = 'class C { public static C operator+(C a, C b) => a; }'
    SA1008 = 'class C { void M(int a) { M( a); } }'
    SA1009 = 'class C { void M(int a) { M(a ); } }'
    SA1010 = 'class C { int M(int[] a) { return a [0]; } }'
    SA1011 = 'class C { int M(int[] a) { return a[0 ]; } }'
    SA1012 = 'class C { int[] M() { return new[] {1 }; } }'
    SA1013 = 'class C { int[] M() { return new[] { 1}; } }'
    SA1014 = 'class C { System.Collections.Generic.List <int> M() => null; }'
    SA1015 = 'class C { System.Collections.Generic.List<int > M() => null; }'
    SA1016 = 'class C { [ System.Obsolete] void M() { } }'
    SA1017 = 'class C { [System.Obsolete ] void M() { } }'
    SA1018 = 'class C { int ? M() => null; }'
    SA1019 = 'class C { string M(string s) { return s . Trim(); } }'
    SA1020 = 'class C { void M(int i) { i ++; } }'
    SA1021 = 'class C { int M() { return - 1; } }'
    SA1022 = 'class C { int M() { return + 1; } }'
    SA1024 = 'class C { int M(bool b) { return b ? 1:2; } }'
    SA1025 = 'class C { void M() { int  x = 1; _ = x; } }'
    SA1026 = 'class C { int[] M() { return new [] { 1 }; } }'
    SA1027 = "class C`n{`n${T}void M() { }`n}"
    SA1028 = "class C`n{    `n    void M() { }`n}"
    SA1004 = "class C`n{`n    ///<summary>Does it.</summary>`n    public void M() { }`n}"
    SA1006 = "class C`n{`n# if DEBUG`n    void M() { }`n#endif`n}"
    SA1023 = 'unsafe class C { int M(int * p) { return * p; } }'

    # ---- Readability ---------------------------------------------------------------------------
    SA1101 = 'class C { int x; int M() { return x; } }'
    SA1112 = "class C`n{`n    void M()`n    {`n        M(`n        );`n    }`n}"
    SA1106 = 'class C { void M() { ; } }'
    SA1107 = "class C`n{`n    void M()`n    {`n        int a = 1; int b = 2;`n        _ = a + b;`n    }`n}"
    SA1116 = "class C`n{`n    void M(int a,`n        int b)`n    {`n    }`n}"
    SA1117 = "class C`n{`n    void M(int a, int b,`n        int c)`n    {`n    }`n}"
    SA1119 = 'class C { int M() { return (1); } }'
    SA1121 = 'class C { System.Int32 M() => 0; }'
    SA1122 = 'class C { string M() => ""; }'
    SA1128 = "class C`n{`n    C(int a) : this() { }`n`n    C() { }`n}"
    SA1129 = 'class C { int M() => new int(); }'
    SA1131 = 'class C { bool M(int x) => 1 == x; }'
    SA1133 = 'class C { [System.Obsolete, System.Diagnostics.DebuggerStepThrough] void M() { } }'
    SA1134 = 'class C { [System.Obsolete] void M() { } }'
    SA1137 = "class C`n{`n    void M()`n    {`n        int a = 1;`n       int b = 2;`n        _ = a + b;`n    }`n}"

    # ---- Ordering ------------------------------------------------------------------------------
    SA1200 = "using System;`n`nnamespace Cases.SA1200`n{`n    class C { Type t; }`n}"
    SA1208 = "namespace Cases.SA1208`n{`n    using Cases.SA1208.Inner;`n    using System;`n`n    class C { Type t; D d; }`n`n    namespace Inner { class D { } }`n}"
    SA1210 = "namespace Cases.SA1210`n{`n    using System.Text;`n    using System.Collections;`n`n    class C { StringBuilder s; ArrayList a; }`n}"
    SA1211 = "namespace Cases.SA1211`n{`n    using Z = System.Text;`n    using A = System.Collections;`n`n    class C { Z.StringBuilder s; A.ArrayList a; }`n}"

    # ---- Naming --------------------------------------------------------------------------------
    SA1300 = 'class C { void m() { } }'
    SA1302 = 'interface Thing { }'
    SA1303 = 'class C { const int max = 1; }'
    SA1306 = 'class C { private int Value; }'
    SA1309 = 'class C { private int _value; }'
    SA1311 = 'class C { private static readonly int value = 1; }'
    SA1312 = 'class C { void M() { int Value = 1; _ = Value; } }'
    SA1313 = 'class C { void M(int Value) { } }'
    SA1314 = 'class C<Item> { }'

    # ---- Maintainability -----------------------------------------------------------------------
    SA1206 = 'class C { static public void M() { } }'
    SA1207 = 'class C { internal protected void M() { } }'
    SA1209 = "namespace Cases.SA1209`n{`n    using A = System.Text;`n    using System;`n`n    class C { A.StringBuilder s; Type t; }`n}"
    SA1216 = "namespace Cases.SA1216`n{`n    using static System.Math;`n    using System;`n`n    class C { Type t; double d = Abs(1); }`n}"
    SA1217 = "namespace Cases.SA1217`n{`n    using static System.Math;`n    using static System.Console;`n`n    class C { double d = Abs(1); void M() => WriteLine(); }`n}"
    SA1205 = "public partial class C { }`npartial class C { }`npartial class D { }"
    SA1400 = 'class C { void M() { } }'
    SA1407 = 'class C { int M(int a, int b, int c) => a + b * c; }'
    SA1408 = 'class C { bool M(bool a, bool b, bool c) => a || b && c; }'
    SA1412 = 'class C { }'
    SA1413 = "class C`n{`n    int[] M() => new[]`n    {`n        1,`n        2`n    };`n}"

    # ---- Layout --------------------------------------------------------------------------------
    SA1500 = "class C`n{`n    void M() {`n    }`n}"
    SA1501 = "class C`n{`n    void M(bool b)`n    {`n        if (b) { return; }`n    }`n}"
    SA1502 = 'class C { }'
    SA1503 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n            return;`n    }`n}"
    SA1505 = "class C`n{`n`n    void M() { }`n}"
    SA1507 = "class C`n{`n    int a;`n`n`n    int b;`n}"
    SA1508 = "class C`n{`n    void M() { }`n`n}"
    SA1509 = "class C`n{`n    void M()`n`n    {`n    }`n}"
    SA1510 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n        {`n        }`n`n        else`n        {`n        }`n    }`n}"
    SA1512 = "class C`n{`n    // comment`n`n    void M() { }`n}"
    SA1513 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n        {`n        }`n        M(b);`n    }`n}"
    SA1515 = "class C`n{`n    void M()`n    {`n        int a = 1;`n        // comment`n        _ = a;`n    }`n}"
    SA1516 = "class C`n{`n    void A() { }`n    void B() { }`n}"
    SA1517 = "`n`nclass C { }"
    SA1518 = "class C { }`n`n`n"
    SA1519 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n            M(`n                b);`n    }`n}"
    SA1520 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n        {`n            M(b);`n        }`n        else`n            M(!b);`n    }`n}"

    # ---- Documentation -------------------------------------------------------------------------
    SA1633 = 'class C { }'

    # ---- Probes: does StyleCop 1.2 report these at all? ('no-repro' = no diagnostic on an obvious violation) -------
    SA1109 = "class C`n{`n    void M(bool b)`n    {`n        if (b)`n        #region R`n        {`n        }`n        #endregion`n    }`n}"
    SA1126 = 'class C { int x; int M() { return x; } }'
    SA1301 = 'class C { public int Value { get; set; } }'
    SA1409 = 'class C { void M() { try { } finally { } } }'
    SA1630 = "/// <summary>Words.</summary>`npublic class C`n{`n    /// <summary>Nowhitespacehere.</summary>`n    public void M() { }`n}"
    SA1631 = "/// <summary>Words.</summary>`npublic class C`n{`n    /// <summary>@@@ ### $$$ %%% ^^^.</summary>`n    public void M() { }`n}"
    SA1632 = "/// <summary>Words.</summary>`npublic class C`n{`n    /// <summary>A.</summary>`n    public void M() { }`n}"
    SA1650 = "/// <summary>Words.</summary>`npublic class C`n{`n    /// <summary>Teh wrold is runing.</summary>`n    public void M() { }`n}"
}
