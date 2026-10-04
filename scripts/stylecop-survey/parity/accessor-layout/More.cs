namespace Probe
{
    public class More
    {
        private int x;
        private int y;

        public int TwoStatements
        {
            get { return this.x; }
            set
            {
                this.x = value;
                this.y = value;
            }
        }

        public int AttributeAbove
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return this.x; }
            set { this.x = value; }
        }

        public int AttributeAboveMixed
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return this.x; }
            set
            {
                this.x = value;
                this.y = value;
            }
        }

        public int WithComment
        {
            get { return this.x; }
            set
            {
                // keep it
                this.x = value;
            }
        }

        public int Nested
        {
            get { return this.x; }
            set
            {
                if (value > 0)
                {
                    this.x = value;
                }
            }
        }
    }
}
