using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIDFLibrary;

namespace CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            ImageComparer comparer = new ImageComparer();

            Console.Write("Path: ");
            string path = Console.ReadLine();
            Console.WriteLine();

            comparer.Prepare(path);
            var result = comparer.Compare();

            foreach (var item in result)
            {
                Console.WriteLine($"\"{ item.Key }\" is similar to:");
                Console.ForegroundColor = ConsoleColor.Cyan;

                foreach (var copy in item.Value)
                {
                    Console.WriteLine($"> \"{ copy }\"");
                }

                Console.ResetColor();
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}
