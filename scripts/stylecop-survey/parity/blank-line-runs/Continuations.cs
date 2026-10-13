namespace Probe
{
    public class Continuations
    {
        public int Value { get; set; }

        public object M(object o, Continuations c)
        {
            var a = o
                is Continuations
                {
                    Value: 1,
                }
                ? 1
                : 2;
            var b = new Continuations
            {
                Value = 1,
            }
            ?? c;
            var d = new
            {
                Value = 2,
            }
            with
            {
                Value = 3,
            };
            var e = c.Value switch
            {
                1 => 1,
                _ => 2,
            }
            + a;
            var f = new[]
            {
                1,
            }
            as object;
            var g = o switch
            {
                Continuations
                {
                    Value: 1,
                }
                when b != null => 1,
                _ => 0,
            };
            return new Continuations
            {
                Value = 1,
            }
            ?.Value;
        }
    }
}
