using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI
{
    public static class Utils
    {
        /// <summary>
        /// Return size of file in humar readable format
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <returns>Human readable filesize</returns>
        public static string GetFileSize(string path)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };

            double len = new FileInfo(path).Length;
            int index = 0;

            while (len >= 1024 && index < sizes.Length - 1)
            {
                index++;
                len /= 1024;
            }

            return String.Format("{0:0.##} {1}", len, sizes[index]);
        }
    }
}
