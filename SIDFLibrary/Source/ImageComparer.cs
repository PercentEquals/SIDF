using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SIDFLibrary
{
    public class ImageComparer
    {
        private string[] extensions;
        private Dictionary<string, List<bool>> hashes;

        public ImageComparer() 
        {
            extensions = new string[]
            {
                ".bmp", ".exif", ".jpeg", ".jpg", ".png", ".tiff"
            };

            hashes = new Dictionary<string, List<bool>>();
        }

        private bool CheckExtension(string file)
        {
            return extensions.Contains(Path.GetExtension(file));
        }

        public void Prepare(string path)
        {
            if (path == "") return;

            string[] files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Where(file => CheckExtension(file)).ToArray();

            for(int i = 0; i < files.Count(); i++)
            {
                try
                {
                    hashes.Add(files[i], GetHash(new Bitmap(files[i])));
                }
                catch (System.ArgumentException) {
                    Console.WriteLine("Found corrupted file"); // DEBUG LINE
                    continue;
                };

                // Garbage collector
                if (i % 15 == 0)
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                }
            }
        }

        public Dictionary<string, List<string>> Compare()
        {
            var result = new Dictionary<string, List<string>>();
            var used = new List<string>();
            bool isDuplicate = false;

            foreach (var hash in hashes)
            {
                if (used.Contains(hash.Key)) continue;

                foreach (var comp in hashes)
                {
                    if (hash.Key == comp.Key) continue;

                    isDuplicate = hash.Value.Zip(comp.Value, (i, j) => i == j).Count(eq => eq) >= 398 ||
                                  hash.Value.Zip(comp.Value, (i, j) => j == i).Count(eq => eq) >= 398;

                    if (isDuplicate)
                    {
                        if (result.ContainsKey(hash.Key)) result[hash.Key].Add(comp.Key);
                        else result.Add(hash.Key, new List<string>() { comp.Key });

                        used.Add(comp.Key);
                    }
                }
            }

            return result;
        }

        public static List<bool> GetHash(Bitmap bitmap)
        {
            List<bool> result = new List<bool>();

            bitmap = new Bitmap(bitmap, new Size(20, 20));

            for (int j = 0; j < bitmap.Height; j++)
            {
                for (int i = 0; i < bitmap.Width; i++)
                {
                    result.Add(bitmap.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }

            bitmap.Dispose();

            return result;
        }
    }
}
