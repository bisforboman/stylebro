namespace Probe
{
    public class Underscores
    {
        private readonly int _signature_size;
        private int _a_b;
        private int sig_size;
        private static int _static_value;
        private int __two_under;

        public Underscores(int size)
        {
            _signature_size = size;
        }

        public int Use() => _signature_size + _a_b + sig_size + _static_value + __two_under;
    }
}
