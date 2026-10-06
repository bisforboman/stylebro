namespace Messy.Headers
{
    public class Lamp // a simple lamp
    {
        private bool _on;

        public void Toggle()
        {
            // flips the switch
            _on = !_on;
        }
    }
}
