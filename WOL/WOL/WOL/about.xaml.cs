using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace WOL
{
    public partial class about : PhoneApplicationPage
    {
        public about()
        {
            InitializeComponent();
        }

        private void TextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask browser = new WebBrowserTask();
            browser.Uri = new Uri("http://www.mendelonline.be");
            browser.Show();

        }
    }
}