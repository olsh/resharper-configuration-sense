using Microsoft.Extensions.Configuration;

namespace ConsoleApp
{
    public static class Program
    {
        public static void Main(IConfiguration configuration)
        {
            var section = configuration.GetSection("MissingSection");
        }
    }
}
