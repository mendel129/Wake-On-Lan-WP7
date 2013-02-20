using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;

namespace WOL
{
    public class iconset
    {
        string imagename;

        public string Imagename
        {
            get { return imagename; }
            set { imagename = value; }
        }
    }

    public class imagelist
    {
        BitmapImage imagename;

        public BitmapImage getImage
        {
            get { return imagename; }
            set { imagename = value; }
        }
    }

    public class computerset
    {
        string mac;
        string name;
        string image;

        public string Mac
        {
            get { return mac; }
            set { mac = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Image
        {
            get { return image; }
            set { image = value; }
        }
    }


    public class othercomputerset
    {
        string mac;
        string name;
        BitmapImage image;

        public string Mac
        {
            get { return mac; }
            set { mac = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }
    }
}
