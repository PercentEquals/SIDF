using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Policy;

namespace SIDFLibrary
{
    public class ImageComparer
    {
        private string[] extensions;
        private List<string> used;

        public Dictionary<string, List<bool>> Hashes { get; private set; } = new Dictionary<string, List<bool>>();
        public Dictionary<string, List<string>> Result { get; private set; } = new Dictionary<string, List<string>>();
        public List<string> Files { get; set; } = new List<string>();

        public ImageComparer() 
        {
            extensions = new string[]
            {
                ".bmp", ".exif", ".jpeg", ".jpg", ".png", ".tiff"
            };

            used = new List<string>();

            Clear();
        }

        private bool CheckExtension(string file)
        {
            return extensions.Contains(Path.GetExtension(file.ToLower()));
        }

        public void SetPath(string path)
        {
            if (path == "") return;

            Files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Where(file => CheckExtension(file)).ToList();
        }

        public void Prepare()
        {
            for(int i = 0; i < Files.Count(); i++)
            {
                IteratePreparation(i);
            }
        }

        public void IteratePreparation(int i)
        {
            try
            {
                Hashes.Add(Files[i], GetHash(new Bitmap(Files[i])));
            }
            catch (System.ArgumentException)
            {
                return;
            };

            // Garbage collector
            if (i % 15 == 0)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
        }

        public void Compare()
        {
            foreach (var hash in Hashes)
            {
                IterateComparison(hash);
            }
        }

        public void IterateComparison(in KeyValuePair<string, List<bool>> hash)
        {
            if (used.Contains(hash.Key)) return;

            foreach (var comp in Hashes)
            {
                if (hash.Key == comp.Key) continue;

                if (hash.Value.Zip(comp.Value, (i, j) => i == j).Count(eq => eq) >= 398)
                {
                    if (Result.ContainsKey(hash.Key)) Result[hash.Key].Add(comp.Key);
                    else Result.Add(hash.Key, new List<string>() { comp.Key });

                    used.Add(comp.Key);
                }
            }
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

        public void Clear()
        {
            Hashes.Clear();
            Result.Clear();
            Files.Clear();

            used.Clear();
        }
    }
}
