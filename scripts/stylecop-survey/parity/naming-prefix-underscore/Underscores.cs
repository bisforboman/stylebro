namespace Probe
{
    using System;

    public class Underscores
    {
        private int m_member;
        private static int s_static;
        [ThreadStatic]
        private static int t_thread;
        private int x_other;
        private int M_upper;
        private int m_;
        private int with_underscore;
        private int _leading;
        private int trailing_;
        private int two__underscores;
        private const int MAX_VALUE = 1;
        private static readonly int Default_Value = 1;
        public int Public_Field;
        protected int protected_field;
        private int m_with_more;
        private int m_Upper;

        public event EventHandler on_changed;

        public int Some_Property { get; set; }

        public void Some_Method(int m_param, int with_under)
        {
            int m_local = 1;
            int local_var = 2;
        }

        public int Use() => m_member + s_static + t_thread + x_other + M_upper + m_ + with_underscore + _leading
            + trailing_ + two__underscores + MAX_VALUE + Default_Value + Public_Field + protected_field + m_with_more
            + m_Upper;
    }
}
