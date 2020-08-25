using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace GUI
{
    class ImgBind
    {
        private string _size;

        public string Name { get; set; }
        public string Group { get; set; }
        public string Size 
        {
            get => _size;
            set => _size = Utils.GetFileSize(value);
        }
    }
}
