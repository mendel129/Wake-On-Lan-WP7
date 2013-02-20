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
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace WOL
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        bool quickexit=true;

        public SettingsPage()
        {
            InitializeComponent();
            

        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            quickexit = Convert.ToBoolean(checkBox1.IsChecked.ToString());
            NavigationService.Navigate(new Uri("/MainPage.xaml?quickexit="+quickexit.ToString(), UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs ee)
        {
            try
            {
                quickexit = Convert.ToBoolean(this.NavigationContext.QueryString["quickexit"]);
            }
            catch (Exception e) { }

            checkBox1.IsChecked = quickexit;
        }

    }
}