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
        #region Fields and Properties

        private string[] _extensions;
        private List<string> _used;

        public Dictionary<string, List<bool>> Hashes { get; private set; } = new Dictionary<string, List<bool>>();
        public Dictionary<string, List<string>> Result { get; private set; } = new Dictionary<string, List<string>>();
        public List<string> Files { get; set; } = new List<string>();

        #endregion

        #region Constructors

        /// <summary>
        /// Default contructor
        /// </summary>
        public ImageComparer() 
        {
            _extensions = new string[]
            {
                ".bmp", ".exif", ".jpeg", ".jpg", ".png", ".tiff"
            };

            _used = new List<string>();

            Clear();
        }

        #endregion

        #region PrivateMethods

        /// <summary>
        /// Returns whether file has acceptable extenstion
        /// </summary>
        /// <param name="file">Full filepath with filename</param>
        /// <returns></returns>
        private bool CheckExtension(string file)
        {
            return _extensions.Contains(Path.GetExtension(file.ToLower()));
        }

        #endregion

        #region PublicMethods

        /// <summary>
        /// Enumarates through all files within directory and its subfolders
        /// </summary>
        /// <param name="path">Path to directory</param>
        public void SetPath(string path)
        {
            if (path == "") return;

            Files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Where(file => CheckExtension(file)).ToList();
        }

        /// <summary>
        /// Executes 'IteratePreparation' method for every file
        /// </summary>
        public void Prepare()
        {
            for(int i = 0; i < Files.Count(); i++)
            {
                IteratePreparation(i);
            }
        }

        /// <summary>
        /// Executes 'IterateComparison' method for every file
        /// </summary>
        public void Compare()
        {
            foreach (var hash in Hashes)
            {
                IterateComparison(hash);
            }
        }

        #endregion

        #region IterateMethods

        /// <summary>
        /// Iterates through files and generates hashes
        /// </summary>
        /// <param name="i">Index of Files property</param>
        public void IteratePreparation(int i)
        {
            try
            {
                Hashes.Add(Files[i], GetHash(Files[i]));
            }
            catch (System.ArgumentException)
            {
                return;
            };

            // Garbage collector
            if (i % 30 == 0)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Iterates through hashes and compares them with passed hash
        /// </summary>
        /// <param name="hash">Hash to campare with every other one</param>
        public void IterateComparison(in KeyValuePair<string, List<bool>> hash)
        {
            if (_used.Contains(hash.Key)) return;

            foreach (var comp in Hashes)
            {
                if (hash.Key == comp.Key) continue;

                if (hash.Value.Zip(comp.Value, (i, j) => i == j).Count(eq => eq) >= 253)
                {
                    if (Result.ContainsKey(hash.Key)) Result[hash.Key].Add(comp.Key);
                    else Result.Add(hash.Key, new List<string>() { comp.Key });

                    _used.Add(comp.Key);
                }
            }
        }

        #endregion

        #region Hashing Methods

        /// <summary>
        /// Returns median brightness of image
        /// </summary>
        /// <param name="bitmap">Bitmap of image</param>
        /// <returns></returns>
        public static float GetMedianBrightness(in Bitmap bitmap)
        {
            List<float> brightness = new List<float>();

            for (int j = 0; j < bitmap.Height; j++)
            {
                for (int i = 0; i < bitmap.Width; i++)
                {
                    if (bitmap.GetPixel(i, j).A <= 0.1) continue; 
                    brightness.Add(bitmap.GetPixel(i, j).GetBrightness());
                }
            }

            brightness.Sort();

            return brightness[brightness.Count / 2];
        }

        /// <summary>
        /// Creates hashes of images by scaling them to 16x16 and then reducing those pixels to black and white
        /// </summary>
        /// <param name="bitmap">Bitmap of image</param>
        /// <returns>Hash of file in form of 16x16 List of booleans</returns>
        public static List<bool> GetHash(string path)
        {
            List<bool> result = new List<bool>();

            using (Bitmap temp = new Bitmap(path))
            {
                using (Bitmap bitmap = new Bitmap(temp, new Size(16, 16)))
                {
                    float brightness = GetMedianBrightness(bitmap);

                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        for (int i = 0; i < bitmap.Width; i++)
                        {
                            result.Add(bitmap.GetPixel(i, j).GetBrightness() < brightness);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region ClearMethod

        /// <summary>
        /// Clears dictionaries and lists
        /// </summary>
        public void Clear()
        {
            Hashes.Clear();
            Result.Clear();
            Files.Clear();

            _used.Clear();
        }

        #endregion
    }
}
