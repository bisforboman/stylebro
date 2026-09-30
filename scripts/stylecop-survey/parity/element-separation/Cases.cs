using System;
namespace Probe
{
    using System.Collections.Generic;
    public enum E { A }
    public class Fields
    {
        private int a;
        private int b;
        private int c = 1;
        // commented field
        private int d;
        public event EventHandler Changed;
        public int P { get; set; }
        public int Q { get; set; }
        public int R
        {
            get { return a; }
            set { a = value; }
        }
        public int S
        {
            get
            {
                return b;
            }
            set
            {
                b = value;
            }
        }
        public void M() { }
        public void N() { }
        public void O()
        {
        }
        public void Q2() => M();
        /// <summary>Documented.</summary>
        public void Documented() { }
        [Obsolete]
        public void Attributed() { }
#if DEBUG
        public void Debug() { }
#endif
        public void AfterDirective() { }
    }
    public class Second { }
    public interface I
    {
        void X();
        void Y();
    }
}
