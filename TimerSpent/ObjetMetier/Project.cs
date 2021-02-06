
namespace TimerSpent.ObjetMetier
{
    public class Project
    {
        public int Number { set; get; }

        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Number} - {Description}";
        }
    }
}
