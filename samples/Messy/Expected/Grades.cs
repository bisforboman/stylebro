namespace Messy.Grades
{
    public class Grades
    {
        public bool IsPassing(int score) => score is >= 50;

        public bool IsTopOrBottom(int score) => score is (>= 90 and <= 100) or < 10 or (> 60 and < 70);

        public string Describe(object value) => value switch
        {
            int points => points.ToString(),
            _ => "none",
        };
    }
}
