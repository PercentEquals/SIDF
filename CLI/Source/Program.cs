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
            string path;

            if (args.Length >= 1)
            {
                path = args[0];
            }
            else
            {
                Console.Write("Path: ");
                path = Console.ReadLine();
                Console.WriteLine();
            }

            comparer.SetPath(path);

            Console.WriteLine("Preparing images: ");

            for (int i = 0; i < comparer.Files.Count(); i++)
            {
                comparer.IteratePreparation(i);
                ProgressBar(i + 1, comparer.Files.Count());

            }

            Console.WriteLine("\nComparing images: ");

            int index = 1;
            foreach(var hash in comparer.Hashes)
            {
                comparer.IterateComparison(hash);
                ProgressBar(index, comparer.Files.Count());
                index++;
            }

            Console.WriteLine("\n");

            foreach (var item in comparer.Result)
            {
                Console.WriteLine($"File \"{ item.Key }\" is similar to:");
                Console.ForegroundColor = ConsoleColor.Cyan;

                foreach (var copy in item.Value)
                {
                    Console.WriteLine($"> { copy }");
                }

                Console.ResetColor();
                Console.WriteLine();
            }

            if (args.Length == 0) Console.ReadLine();

            comparer.Clear();
        }

        public static void ProgressBar(int i, int n)
        {
            Console.Write($"\r{ i }/{ n } - ({ Convert.ToInt32(Convert.ToDecimal(i) / n * 100) }%)");
        }
    }
}
