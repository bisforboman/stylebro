namespace Probe
{
    using System;

    public class Strings
    {
        private const string Constant = "";
        private static readonly string ReadOnly = "";
        private string field = "";

        [Obsolete("")]
        public string M(string s = "")
        {
            var a = "";
            var b = @"";
            var c = $"";
            var e = "x";
            var f = " ";
            switch (s)
            {
                case "":
                    break;
            }

            bool g = s is "";
            bool h = s == "";
            var i = s switch { "" => 1, _ => 2 };
            ReadOnlySpan<byte> j = ""u8;
            var k = nameof(M) + "";
            return a + b + c + e + f + g + h + i + j.Length + k + field + ReadOnly + Constant;
        }
    }
}
