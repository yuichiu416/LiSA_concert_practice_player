using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Rkry8FinalProject
{
    class Background
    {
        Image bgImage;
        public Background(Image backgroundImage)
        {
            bgImage = backgroundImage;
        }

        public void Load(String backgroundFilePath)
        {
            BitmapImage theBitmap = new BitmapImage();
            theBitmap.BeginInit();
            theBitmap.UriSource = new Uri(backgroundFilePath, UriKind.Absolute);
            theBitmap.EndInit();
            bgImage.Source = theBitmap;
        }
    }
}
