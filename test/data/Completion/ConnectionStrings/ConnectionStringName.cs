using System.Configuration;

namespace ConsoleApp
{
    public static class Program
    {
        public static void Main()
        {
            var value = ConfigurationManager.ConnectionStrings["{caret}"];
        }
    }
}
