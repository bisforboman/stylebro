namespace Probe
{
    using System;

    public class Props
    {
        private int x;

        public int Mixed
        {
            get { return this.x; }
            set
            {
                this.x = value;
            }
        }

        public int MixedReverse
        {
            get
            {
                return this.x;
            }
            set { this.x = value; }
        }

        public int AllSingle
        {
            get { return this.x; }
            set { this.x = value; }
        }

        public int AllMulti
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = value;
            }
        }

        public int ExpressionAndBlock
        {
            get => this.x;
            set
            {
                this.x = value;
            }
        }

        public int AutoGetBlockSet
        {
            get;
            set
            {
                this.x = value;
            }
        }

        public int OneLine { get { return this.x; } set { this.x = value; } }

        public int this[int i]
        {
            get { return i; }
            set
            {
                this.x = value;
            }
        }

        public event EventHandler Ev
        {
            add { }
            remove
            {
                this.x = 0;
            }
        }

        public int MultiLineButCompact
        {
            get { return this.x;
            }
            set { this.x = value; }
        }
    }
}