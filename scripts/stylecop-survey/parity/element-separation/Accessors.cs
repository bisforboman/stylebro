namespace AccessorProbe
{
    public class C
    {
        private int a;

        public int P
        {
            get => a;
            set
            {
                a = value;
            }
        }

        public int Q
        {
            get
            {
                return a;
            }
            set => a = value;
        }

        public int R
        {
            get
            {
                return a;
            }
            set { a = value; }
        }
    }
}
